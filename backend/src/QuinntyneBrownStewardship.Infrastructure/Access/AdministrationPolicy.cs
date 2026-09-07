namespace QuinntyneBrownStewardship.Infrastructure.Access;

// The one role the authenticated session can report, and the policy every authoring endpoint requires.
public static class AdministrationPolicy
{
    public const string Name = "Administration";
    public const string Role = "Administrator";
}
