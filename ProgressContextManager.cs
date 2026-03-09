using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace Harmony;

/// <summary>
/// Manages progress display for file conversion operations using Spectre.Console.
/// Shows a two-line display: overall progress bar and current book title.
/// </summary>
internal class ProgressContextManager : IDisposable
{
    private readonly CancellationToken _cancellationToken;
    private readonly int _totalFiles;
    private int _currentFileIndex;
    private string _currentBookTitle = "Starting...";
    private bool _isDisposed;
    private bool _isComplete;

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
    /// Creates the status text for the current progress.
    /// </summary>
    private string CreateStatusText()
    {
        var percentage = (double)_currentFileIndex / _totalFiles * 100;
        var barLength = 40;
        var filledLength = (int)(percentage / 100 * barLength);
        var bar = new string('━', filledLength) + new string('─', barLength - filledLength);
        
        var status = $"[green]Overall Progress ({_currentFileIndex} of {_totalFiles})[/] [white]{bar}[/] [yellow]{percentage:F0}%[/]";
        
        if (!_isComplete && _currentFileIndex > 0)
        {
            status += $"\n[grey]  {_currentBookTitle.EscapeMarkup()}[/]";
        }
        else if (_isComplete)
        {
            status += "\n[green]  Complete[/]";
        }
        
        return status;
    }

    /// <summary>
    /// Runs the progress display with the specified async action.
    /// Shows a two-line display: overall progress bar and current book title.
    /// </summary>
    /// <param name="action">The async action to execute within the progress context.</param>
    public async Task RunAsync(Func<ProgressContextManager, Task> action)
    {
        await AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(Spinner.Known.Default)
            .StartAsync("Starting...", async ctx =>
            {
                ctx.Status(CreateStatusText());

                // Execute the user action in background
                var workTask = action(this);

                // Keep updating until complete
                while (!workTask.IsCompleted)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    ctx.Status(CreateStatusText());
                    await Task.Delay(100).ConfigureAwait(false);
                }

                // Mark complete
                _isComplete = true;
                ctx.Status(CreateStatusText());

                await workTask.ConfigureAwait(false);
            }).ConfigureAwait(false);
    }

    /// <summary>
    /// Starts tracking a new file.
    /// </summary>
    /// <param name="fileName">Name of the file being processed.</param>
    /// <returns>Task ID for the file.</returns>
    public int StartNewFile(string fileName)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        _currentFileIndex++;
        _currentBookTitle = FormatBookTitle(fileName);
        return _currentFileIndex;
    }

    /// <summary>
    /// Marks the current file as complete.
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
