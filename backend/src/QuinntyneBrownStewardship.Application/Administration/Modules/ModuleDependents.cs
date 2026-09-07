namespace QuinntyneBrownStewardship.Application.Administration.Modules;

// What participants have recorded against a module. Any of it keeps the module in place, and the reason names each part that does.
public sealed record ModuleDependents(int Completions, int Notes, int Answers)
{
    public bool Any => Completions > 0 || Notes > 0 || Answers > 0;
    public string? Reason
    {
        get
        {
            if (!Any) return null;
            var parts = new List<string>();
            if (Completions > 0) parts.Add($"{Completions} completion{(Completions == 1 ? " is" : "s are")} recorded against the sections of this module.");
            if (Notes > 0) parts.Add($"{Notes} note{(Notes == 1 ? " is" : "s are")} attached to this module.");
            if (Answers > 0) parts.Add($"{Answers} participant{(Answers == 1 ? " has" : "s have")} answered a prompt in this module.");
            return string.Join(" ", parts) + " It cannot be removed while those records stand.";
        }
    }
}
