using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace Harmony;

/// <summary>
/// Manages progress display for file conversion operations using Spectre.Console.
/// Shows progress bar with fixed alignment: counter | bar | % | spinner | title
/// </summary>
internal class ProgressContextManager : IDisposable
{
    private readonly CancellationToken _cancellationToken;
    private readonly int _totalFiles;
    private int _currentFileIndex;
    private string _currentBookTitle = "Starting...";
    private ProgressContext? _progressContext;
    private ProgressTask? _progressTask;
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
        
        // Truncate if too long
        if (title.Length > 50)
            title = title.Substring(0, 47) + "...";
        
        return title;
    }

    /// <summary>
    /// Gets the full status line with everything aligned.
    /// Format: [[X/Y]]  Book Title (padded to fixed width so bar starts at same column)
    /// </summary>
    private string GetStatusText()
    {
        var counter = $"[[{_currentFileIndex}/{_totalFiles}]]";
        var title = _currentBookTitle;
        
        // Combine with a fixed width padding so the progress bar always starts at the same position
        // Counter is ~10 chars, title is ~50 chars, total ~60 chars
        return $"{counter,-10} {title,-50}";
    }

    /// <summary>
    /// Runs the progress display with the specified async action.
    /// </summary>
    /// <param name="action">The async action to execute within the progress context.</param>
    public async Task RunAsync(Func<ProgressContextManager, Task> action)
    {
        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn(),
            })
            .StartAsync(async ctx =>
            {
                _progressContext = ctx;
                
                // Create task with full status (counter + title on left, then bar/spinner)
                _progressTask = ctx.AddTask(GetStatusText(), maxValue: _totalFiles);

                // Execute the user action
                await action(this).ConfigureAwait(false);

                // Mark complete
                if (_progressTask != null)
                {
                    _progressTask.Value = _totalFiles;
                    _currentBookTitle = "Complete";
                    _progressTask.Description($"[[{_totalFiles}/{_totalFiles}]] Complete");
                }
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
        
        // Update description with padded text so bar stays aligned
        if (_progressTask != null)
        {
            _progressTask.Description(GetStatusText());
        }
    }

    /// <summary>
    /// Marks the current file as complete and increments progress.
    /// </summary>
    public void CompleteFile()
    {
        _cancellationToken.ThrowIfCancellationRequested();
        _progressTask?.Increment(1);
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
