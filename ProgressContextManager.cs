using System;
using System.Collections.Generic;
using System.Threading;
using Spectre.Console;

namespace Harmony;

/// <summary>
/// Manages progress display for file conversion operations using Spectre.Console.
/// Thread-safe for concurrent updates.
/// </summary>
internal class ProgressContextManager : IDisposable
{
    private readonly CancellationToken _cancellationToken;
    private readonly int _totalFiles;
    private int _currentFileIndex;
    private ProgressTask? _overallTask;
    private readonly List<ProgressTask> _operationTasks = new();
    private bool _isDisposed;

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
    /// Runs the progress display with the specified action.
    /// This is the main entry point for using the progress manager.
    /// </summary>
    /// <param name="action">The action to execute within the progress context.</param>
    public void Run(Action<ProgressContextManager> action)
    {
        AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn(),
            })
            .Start(ctx =>
            {
                // Create the overall progress task
                _overallTask = ctx.AddTask($"Overall Progress (0 of {_totalFiles})", maxValue: _totalFiles);

                // Execute the user action
                action(this);

                // Ensure all tasks are complete
                if (_overallTask.Value < _overallTask.MaxValue)
                {
                    _overallTask.Value = _overallTask.MaxValue;
                }
            });
    }

    private ProgressTask? _currentFileTask;

    /// <summary>
    /// Runs the progress display with the specified async action.
    /// This is the main entry point for using the progress manager with async operations.
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
                // Create the overall progress task
                _overallTask = ctx.AddTask($"Overall Progress (0 of {_totalFiles})", maxValue: _totalFiles);
                
                // Create a task to show current file name
                _currentFileTask = ctx.AddTask("Ready", maxValue: 1);
                _currentFileTask.Value = 0; // Keep it at 0 so it shows as active

                // Execute the user action
                await action(this).ConfigureAwait(false);

                // Ensure all tasks are complete
                if (_overallTask.Value < _overallTask.MaxValue)
                {
                    _overallTask.Value = _overallTask.MaxValue;
                }
                if (_currentFileTask != null)
                {
                    _currentFileTask.Description("Complete");
                    _currentFileTask.Value = 1;
                }
            }).ConfigureAwait(false);
    }

    /// <summary>
    /// Starts tracking a new file with its own progress bar.
    /// </summary>
    /// <param name="fileName">Name of the file being processed.</param>
    /// <returns>Task ID for the file's progress bar.</returns>
    public int StartNewFile(string fileName)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        _currentFileIndex++;

        // Update overall progress description
        _overallTask?.Description($"Overall Progress ({_currentFileIndex} of {_totalFiles})");
        
        // Update current file task to show the file name
        _currentFileTask?.Description(fileName);

        return _currentFileIndex;
    }

    /// <summary>
    /// Marks the current file as complete.
    /// </summary>
    public void CompleteFile()
    {
        _cancellationToken.ThrowIfCancellationRequested();

        _overallTask?.Increment(1);

        // Clear operation tasks for the next file
        foreach (var task in _operationTasks)
        {
            task.StopTask();
        }
        _operationTasks.Clear();
    }

    /// <summary>
    /// Adds an operation task for the current file (e.g., probing, converting, extracting cover).
    /// Must be called within the Run/RunAsync context.
    /// </summary>
    /// <param name="progressContext">The Spectre.Console progress context.</param>
    /// <param name="operationName">Name of the operation.</param>
    /// <param name="maxValue">Maximum value for the operation (default 100 for percentage).</param>
    /// <returns>A ProgressTask for tracking this operation.</returns>
    public ProgressTask AddOperationTask(ProgressContext progressContext, string operationName, double maxValue = 100)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        var task = progressContext.AddTask(operationName, maxValue: maxValue);
        _operationTasks.Add(task);
        return task;
    }

    /// <summary>
    /// Updates the progress of a specific task.
    /// </summary>
    /// <param name="task">The task to update.</param>
    /// <param name="value">The current value.</param>
    public void UpdateProgress(ProgressTask? task, double value)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        task?.Value(value);
    }

    /// <summary>
    /// Updates the progress of a specific task by incrementing it.
    /// </summary>
    /// <param name="task">The task to update.</param>
    /// <param name="increment">The amount to increment.</param>
    public void IncrementProgress(ProgressTask? task, double increment)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        task?.Increment(increment);
    }

    /// <summary>
    /// Sets the description of a task.
    /// </summary>
    /// <param name="task">The task to update.</param>
    /// <param name="description">The new description.</param>
    public void SetTaskDescription(ProgressTask? task, string description)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        task?.Description(description);
    }

    /// <summary>
    /// Disposes the progress manager and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            _operationTasks.Clear();
        }
    }
}
