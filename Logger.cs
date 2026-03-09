using Spectre.Console;

namespace Harmony;

internal class Logger(bool quietMode, bool useAnsiConsole = false)
{
    private readonly bool _quietMode = quietMode;
    private readonly bool _useAnsiConsole = useAnsiConsole;

    // Spinner animation options (uncomment to change):
    // private readonly string _spinnerString = "/-\\|";
    // private readonly string _spinnerString = "⣾⣽⣻⢿⡿⣟⣯⣷";
    // private readonly string _spinnerString = "🌑🌒🌓🌔🌕🌖🌗🌘";
    // private readonly string _spinnerString = "⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏";
    private readonly string _spinnerString = "←↖↑↗→↘↓↙";
    // private readonly string _spinnerString = "▁▂▃▄▅▆▇█▇▆▅▄▃▂";
    // private readonly string _spinnerString = "▉▊▋▌▍▎▏▎▍▌▋▊▉";

    private int _spinnerPos;

    /// <summary>
    /// Returns true if we're in TUI mode (progress bars active).
    /// When TUI is active, detailed logging should be suppressed.
    /// </summary>
    public bool IsTuiMode => _useAnsiConsole;

    internal void WriteLine(string v)
    {
        if (_quietMode || _useAnsiConsole) return;
        Console.WriteLine(v);
    }

    internal void Write(string v)
    {
        if (_quietMode || _useAnsiConsole) return;
        Console.Write(v);
    }

    internal void AdvanceSpinner()
    {
        if (!_quietMode && !_useAnsiConsole)
            Console.Write("\b" + _spinnerString[_spinnerPos++ % _spinnerString.Length]);
    }
}