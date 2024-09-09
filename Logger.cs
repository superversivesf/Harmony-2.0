namespace Harmony;

internal class Logger
{
    private readonly bool _quietMode;
    //private readonly string _spinnerString = "/-\\|";
    //private readonly string _spinnerString = "⣾⣽⣻⢿⡿⣟⣯⣷";
    //private readonly string _spinnerString = "🌑🌒🌓🌔🌕🌖🌗🌘";
    //private readonly string _spinnerString = "⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏";
    private readonly string _spinnerString = "←↖↑↗→↘↓↙";
    //private readonly string _spinnerString = "▁▂▃▄▅▆▇█▇▆▅▄▃▂";
    //private readonly string _spinnerString = "▉▊▋▌▍▎▏▎▍▌▋▊▉";




    private int _spinnerPos;
    //private string spinnerString = ".oO0Oo";
    //private string spinnerString = "<^>v";
    //private string spinnerString = "└┘┐┌";
    //private string spinnerString = "▄▀";
    //private string spinnerString = "#■.";
    //private string spinnerString = "+x";
    //private string spinnerString = "1234567890";
    //private string spinnerString = ",.oO0***0Oo.,";
    //private string spinnerString = ",.!|T";


    public Logger(bool quietMode)
    {
        _quietMode = quietMode;
        _spinnerPos = 0;
    }

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