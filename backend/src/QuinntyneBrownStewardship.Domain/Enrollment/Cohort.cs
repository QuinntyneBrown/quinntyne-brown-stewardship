namespace QuinntyneBrownStewardship.Domain.Enrollment;

public sealed class Cohort
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly StartDate { get; set; }
    public string MentorName { get; set; } = "";
    public int DurationWeeks => 12;
    public int SessionCadenceWeeks => 2;
    public int SessionAllowance => DurationWeeks / SessionCadenceWeeks;
    public DateOnly EndDate => StartDate.AddDays(DurationWeeks * 7);
    public int CurrentWeek(DateOnly today) => Math.Clamp((today.DayNumber - StartDate.DayNumber) / 7 + 1, 1, DurationWeeks);
    public bool HasEnded(DateOnly today) => today >= EndDate;
}
