namespace QuinntyneBrownStewardship.Application.Administration.Curricula;

// The maxima the validators enforce, carried on every authoring read so the client states them without a constant of its own.
public sealed record AuthoringLimits(int Key, int Title, int Summary, int Reading, int EffortEstimate, int PracticeStep, int Prompt)
{
    public static AuthoringLimits From(CurriculumOptions options)
        => new(options.KeyMaxLength, options.TitleMaxLength, options.SummaryMaxLength, options.ReadingMaxLength, options.EffortEstimateMaxLength, options.StepMaxLength, options.PromptMaxLength);
}
