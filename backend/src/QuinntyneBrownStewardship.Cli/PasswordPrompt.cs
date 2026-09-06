using System.Text;
namespace QuinntyneBrownStewardship.Cli;

public static class PasswordPrompt
{
    public static string Read()
    {
        if (Console.IsInputRedirected) return Console.ReadLine() ?? "";
        var password = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return password.ToString(); }
            if (key.Key == ConsoleKey.Backspace) { if (password.Length > 0) password.Length--; }
            else if (!char.IsControl(key.KeyChar) && password.Length < 1024) password.Append(key.KeyChar);
        }
    }
}
