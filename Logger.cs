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

    internal void WriteLine(string v)
    {
        if (_quietMode) return;
        if (_useAnsiConsole)
            AnsiConsole.MarkupLine($"[grey]{v.EscapeMarkup()}[/]");
        else
            Console.WriteLine(v);
    }

    internal void Write(string v)
    {
        if (_quietMode) return;
        if (_useAnsiConsole)
            AnsiConsole.Markup($"[grey]{v.EscapeMarkup()}[/]");
        else
            Console.Write(v);
    }

    internal void AdvanceSpinner()
    {
        if (!_quietMode && !_useAnsiConsole)
            Console.Write("\b" + _spinnerString[_spinnerPos++ % _spinnerString.Length]);
    }
}