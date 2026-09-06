using System.Globalization;
using System.Text;
using FluentValidation;

namespace QuinntyneBrownStewardship.Application.Notes;

public sealed record NoteCursor(DateTimeOffset RevisedAt, Guid Id)
{
    public string Encode() => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{RevisedAt:O}|{Id}"));

    public static NoteCursor? Parse(string? value)
    {
        if (value == null) return null;
        if (value.Length <= 200)
        {
            try
            {
                var parts = Encoding.UTF8.GetString(Convert.FromBase64String(value)).Split('|');
                if (parts.Length == 2 && DateTimeOffset.TryParseExact(parts[0], "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var at)
                    && Guid.TryParse(parts[1], out var id)) return new(at, id);
            }
            catch (FormatException) { }
        }
        throw new ValidationException([new("Cursor", "The notes cursor is invalid. Reload the notes list.")]);
    }
}
