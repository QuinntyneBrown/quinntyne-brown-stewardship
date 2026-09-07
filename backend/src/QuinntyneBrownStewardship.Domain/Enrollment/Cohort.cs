using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Domain.Enrollment;

public sealed class Cohort
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CurriculumId { get; set; }
    public Curriculum Curriculum { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public string MentorName { get; set; } = "";
    public Guid? MentorId { get; set; }
    public string TimeZone { get; set; } = "America/Toronto";
    public int DurationWeeks { get; set; }
    public int SessionCadenceWeeks { get; set; }
    // Integer division yields the whole number of cadence intervals the duration contains.
    public int SessionAllowance => SessionCadenceWeeks > 0 ? DurationWeeks / SessionCadenceWeeks : 0;
    public DateOnly EndDate => StartDate.AddDays(DurationWeeks * 7);
    public int CurrentWeek(DateOnly today) => Math.Clamp((today.DayNumber - StartDate.DayNumber) / 7 + 1, 1, Math.Max(1, DurationWeeks));
    public bool HasEnded(DateOnly today) => today >= EndDate;
}
