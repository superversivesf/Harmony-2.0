using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace Harmony;

/// <summary>
/// Manages progress display for file conversion operations using Spectre.Console.
/// Shows a single progress bar with current book title to the right of the spinner.
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
        
        return title;
    }

    /// <summary>
    /// Gets the current description text for the progress bar.
    /// Format: [X/Y] with book title on the right side.
    /// </summary>
    private string GetDescription()
    {
        return $"[{_currentFileIndex}/{_totalFiles}]";
    }

    /// <summary>
    /// Gets the text to display after the spinner (the book title).
    /// </summary>
    private string GetBookTitleText()
    {
        return _currentBookTitle;
    }

    /// <summary>
    /// Runs the progress display with the specified async action.
    /// </summary>
    /// <param name="action">The async action to execute within the progress context.</param>
    public async Task RunAsync(Func<ProgressContextManager, Task> action)
    {
        // Create a custom columns array with the book title on the right
        var columns = new ProgressColumn[]
        {
            new TaskDescriptionColumn(),
            new ProgressBarColumn(),
            new PercentageColumn(),
            new SpinnerColumn(),
        };

        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(columns)
            .StartAsync(async ctx =>
            {
                _progressContext = ctx;
                
                // Create a single task for overall progress
                _progressTask = ctx.AddTask(GetDescription(), maxValue: _totalFiles);

                // Execute the user action
                await action(this).ConfigureAwait(false);

                // Mark complete
                if (_progressTask != null)
                {
                    _progressTask.Value = _totalFiles;
                    _currentBookTitle = "Complete";
                    _progressTask.Description($"[{_totalFiles}/{_totalFiles}] Complete");
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
        
        // Update the progress task - include book title in description
        if (_progressTask != null)
        {
            _progressTask.Description(GetDescription() + " " + GetBookTitleText());
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
