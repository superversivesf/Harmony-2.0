namespace Harmony;

internal class Logger(bool quietMode)
{
    private readonly bool _quietMode = quietMode;

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
        if (!_quietMode) Console.WriteLine(v);
    }

    internal void Write(string v)
    {
        if (!_quietMode) Console.Write(v);
    }

    internal void AdvanceSpinner()
    {
        Console.Write("\b" + _spinnerString[_spinnerPos++ % _spinnerString.Length]);
    }
}