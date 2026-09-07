using System.Text;
using FluentValidation;
namespace QuinntyneBrownStewardship.Application.Administration;

// Every authored field is refused the same way: by name, with its maximum and the overage to shorten by.
// Length counts characters as an author counts them, so a four-byte character is one, not two.
public static class AuthoredFieldRules
{
    public static int Length(string value) => value.EnumerateRunes().Count();
    public static IRuleBuilderOptions<T, string> Authored<T>(this IRuleBuilder<T, string> rule, string field, int maximum)
    {
        var article = "aeiou".Contains(char.ToLowerInvariant(field[0])) ? "An" : "A";
        // An empty value is within any maximum, so the two rules never both refuse one value and need no cascade between them.
        return rule
            .NotEmpty().WithMessage($"{article} {field} is required.")
            .Must(value => value == null || Length(value) <= maximum)
            .WithMessage((_, value) => $"{article} {field} may be at most {maximum} characters. Shorten it by {Length(value) - maximum}.");
    }
    public static IRuleBuilderOptions<T, string> Key<T>(this IRuleBuilder<T, string> rule, int maximum)
        => rule.Authored("key", maximum).Matches("^[a-z0-9-]+$").WithMessage("A key uses lowercase letters, digits and hyphens only.");
}
