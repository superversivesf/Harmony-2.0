using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Harmony;

/// <summary>
/// Custom column that displays the current book title on the right side.
/// </summary>
internal sealed class BookTitleColumn : ProgressColumn
{
    private readonly ProgressContextManager _manager;

    public BookTitleColumn(ProgressContextManager manager)
    {
        _manager = manager;
    }

    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        var title = _manager.CurrentBookTitle;
        return new Markup($"[grey]{title.EscapeMarkup()}[/]");
    }
}

/// <summary>
/// Manages progress display for file conversion operations using Spectre.Console.
/// Shows: [[X/Y]] ProgressBar Percentage Spinner BookTitle
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
    /// Gets the description text (just the counter).
    /// </summary>
    private string GetDescription()
    {
        // Use double brackets to escape them from Spectre.Console markup parsing
        return $"[[{_currentFileIndex}/{_totalFiles}]]";
    }

    /// <summary>
    /// Runs the progress display with the specified async action.
    /// Layout: Counter | ProgressBar | Percentage | Spinner | BookTitle
    /// </summary>
    /// <param name="action">The async action to execute within the progress context.</param>
    public async Task RunAsync(Func<ProgressContextManager, Task> action)
    {
        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),      // [[X/Y]]
                new ProgressBarColumn(),          // ━━━━━━━━━━
                new PercentageColumn(),           // 22%
                new SpinnerColumn(),              // ⣟
                new BookTitleColumn(this),        // Book Title
            })
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
                    _progressTask.Description($"[[{_totalFiles}/{_totalFiles}]]");
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
        
        // Update the progress task description
        if (_progressTask != null)
        {
            _progressTask.Description(GetDescription());
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
