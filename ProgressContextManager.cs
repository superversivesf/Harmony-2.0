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

    // Thread-safe access to shared state
    private readonly ReaderWriterLockSlim _stateLock = new();
    private readonly object _progressTaskLock = new();

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
    /// Gets the current file index (thread-safe).
    /// </summary>
    public int CurrentFileIndex
    {
        get
        {
            _stateLock.EnterReadLock();
            try { return _currentFileIndex; }
            finally { _stateLock.ExitReadLock(); }
        }
    }

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
    /// Gets the description text (just the counter) - thread-safe.
    /// </summary>
    private string GetDescription()
    {
        _stateLock.EnterReadLock();
        try
        {
            // Use double brackets to escape them from Spectre.Console markup parsing
            return $"[[{_currentFileIndex}/{_totalFiles}]]";
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Runs the progress display with the specified async action.
    /// Layout: Counter | ProgressBar | Percentage | Spinner | BookTitle
    /// Runs the action on a background thread to prevent blocking the UI.
    /// </summary>
    /// <param name="action">The async action to execute within the progress context.</param>
    public async Task RunAsync(Func<ProgressContextManager, Task> action)
    {
        // Check cancellation before starting
        _cancellationToken.ThrowIfCancellationRequested();

        var progressTaskCompletionSource = new TaskCompletionSource();
        var workTaskCompletionSource = new TaskCompletionSource();

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
            .StartAsync(ctx =>
            {
                _progressContext = ctx;

                // Create a single task for overall progress
                lock (_progressTaskLock)
                {
                    _progressTask = ctx.AddTask(GetDescription(), maxValue: _totalFiles);
                }

                // Run the work on a background thread
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await action(this).ConfigureAwait(false);
                        workTaskCompletionSource.TrySetResult();
                    }
                    catch (Exception ex)
                    {
                        workTaskCompletionSource.TrySetException(ex);
                    }
                }, _cancellationToken);

                // Wait for work completion while keeping UI responsive
                return workTaskCompletionSource.Task;
            }).ConfigureAwait(false);

        // Mark complete after work is done
        // Check cancellation before updating UI state
        _cancellationToken.ThrowIfCancellationRequested();

        lock (_progressTaskLock)
        {
            if (_progressTask is not null)
            {
                _progressTask.Value = _totalFiles;
                _progressTask.Description($"[[{_totalFiles}/{_totalFiles}]]");
            }
        }

        _stateLock.EnterWriteLock();
        try
        {
            _currentBookTitle = "Complete";
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Starts tracking a new file. Thread-safe for background execution.
    /// </summary>
    /// <param name="fileName">Name of the file being processed.</param>
    public void StartNewFile(string fileName)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        _stateLock.EnterWriteLock();
        try
        {
            _currentFileIndex++;
            _currentBookTitle = FormatBookTitle(fileName);
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }

        // Update the progress task description - lock for thread safety
        lock (_progressTaskLock)
        {
            _progressTask?.Description(GetDescription());
        }
    }

    /// <summary>
    /// Marks the current file as complete and increments progress. Thread-safe.
    /// </summary>
    public void CompleteFile()
    {
        _cancellationToken.ThrowIfCancellationRequested();

        lock (_progressTaskLock)
        {
            _progressTask?.Increment(1);
        }
    }

    /// <summary>
    /// Disposes the progress manager.
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            _stateLock.Dispose();
        }
    }
}
