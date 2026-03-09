using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace Harmony;

/// <summary>
/// Manages progress display for file conversion operations.
/// Shows a two-line display: progress bar on line 1, book title on line 2.
/// </summary>
internal class ProgressContextManager : IDisposable
{
    private readonly CancellationToken _cancellationToken;
    private readonly int _totalFiles;
    private int _currentFileIndex;
    private string _currentBookTitle = "Starting...";
    private bool _isDisposed;

    /// <summary>
    /// Gets the current book title for display.
    /// </summary>
    public string CurrentBookTitle => _currentBookTitle;

    /// <summary>
    /// Creates a new ProgressContextManager with the specified total file count.
    /// </summary>
    /// <param name="totalFiles">Total number of files to process.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public ProgressContextManager(int totalFiles, CancellationToken cancellationToken = default)
    {
        _totalFiles = totalFiles;
        _cancellationToken = cancellationToken;
        _currentFileIndex = 0;
    }

    /// <summary>
    /// Gets the current file index.
    /// </summary>
    public int CurrentFileIndex => _currentFileIndex;

    /// <summary>
    /// Gets the total number of files.
    /// </summary>
    public int TotalFiles => _totalFiles;

    /// <summary>
    /// Checks if the operation has been cancelled.
    /// </summary>
    public bool IsCancelled => _cancellationToken.IsCancellationRequested;

    /// <summary>
    /// Throws OperationCanceledException if cancellation has been requested.
    /// </summary>
    public void ThrowIfCancellationRequested() => _cancellationToken.ThrowIfCancellationRequested();

    /// <summary>
    /// Formats a filename into a readable book title.
    /// Converts underscores to spaces and removes the file extension and bitrate suffix.
    /// </summary>
    private static string FormatBookTitle(string fileName)
    {
        // Remove file extension
        var title = Path.GetFileNameWithoutExtension(fileName);
        
        // Remove the -AAX_XX_XXX or -AAXC_XX_XXX bitrate suffix pattern
        title = Regex.Replace(title, @"-AAX_?C?_\d+_\d+$", string.Empty);
        
        // Replace underscores with spaces
        title = title.Replace('_', ' ');
        
        return title;
    }

    /// <summary>
    /// Gets the progress bar as a string representation.
    /// </summary>
    private string GetProgressBar(int width = 40)
    {
        var percentage = (double)_currentFileIndex / _totalFiles;
        var filled = (int)(percentage * width);
        var bar = new string('━', filled) + new string('─', width - filled);
        return bar;
    }

    /// <summary>
    /// Gets the full status display.
    /// Format: [[X/Y]] ProgressBar Percentage Spinner
    /// Book title on second line.
    /// </summary>
    private string GetStatusDisplay()
    {
        var percentage = (int)((double)_currentFileIndex / _totalFiles * 100);
        var counter = $"[[{_currentFileIndex}/{_totalFiles}]]";
        var bar = GetProgressBar(40);
        
        // Line 1: Counter + Bar + Percentage
        return $"{counter} {bar} {percentage}%";
    }

    /// <summary>
    /// Runs the progress display with the specified async action.
    /// Uses AnsiConsole.Status with custom status text.
    /// </summary>
    /// <param name="action">The async action to execute within the progress context.</param>
    public async Task RunAsync(Func<ProgressContextManager, Task> action)
    {
        // We'll use a simple approach: update the status text with both lines
        // The status spinner will be on the right side of the status text
        
        await AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Default)
            .StartAsync("Starting...", async ctx =>
            {
                ctx.Status(GetStatusDisplay());

                // Execute the user action in background
                var workTask = action(this);

                // Keep updating display until complete
                while (!workTask.IsCompleted)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    ctx.Status(GetStatusDisplay());
                    await Task.Delay(100).ConfigureAwait(false);
                }

                await workTask.ConfigureAwait(false);
                
                // Final update
                ctx.Status(GetStatusDisplay() + "\nComplete");
            }).ConfigureAwait(false);
    }

    /// <summary>
    /// Starts tracking a new file.
    /// </summary>
    /// <param name="fileName">Name of the file being processed.</param>
    public void StartNewFile(string fileName)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        _currentFileIndex++;
        _currentBookTitle = FormatBookTitle(fileName);
    }

    /// <summary>
    /// Marks the current file as complete and increments progress.
    /// </summary>
    public void CompleteFile()
    {
        _cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Disposes the progress manager.
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
        }
    }
}
