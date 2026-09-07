using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

// A key names one programme. The refusal names the key so the author knows which one to change.
public static class KeyRules
{
    public static async Task Free(ICurriculumStore store, string key, Guid? except, CancellationToken ct)
    {
        var holder = await store.CurriculumByKey(key, ct);
        if (holder != null && holder.Id != except) throw new ProgrammeException(409, $"The key {key} is already used by another programme. Choose a different key.");
    }
    public static string Following(int cohorts) => $"{cohorts} cohort{(cohorts == 1 ? "" : "s")} follow{(cohorts == 1 ? "s" : "")} this programme.";
}
