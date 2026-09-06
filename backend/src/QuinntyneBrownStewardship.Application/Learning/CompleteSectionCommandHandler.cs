using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;

namespace QuinntyneBrownStewardship.Application.Learning;

public sealed class CompleteSectionCommandHandler(IProgrammeStore store, ISystemClock clock, ProgrammeReader reader) : IRequestHandler<CompleteSectionCommand, CompletionResponse>
{
    public async Task<CompletionResponse> Handle(CompleteSectionCommand request, CancellationToken ct)
    {
        return await store.Transaction(async token =>
        {
            var enrollment = await reader.Enrollment(token);
            var modules = await store.ProgressModules(enrollment.Cohort.CurriculumKey, token);
            var module = modules.SingleOrDefault(m => m.Sections.Any(s => s.Id == request.SectionId)) ?? throw new ProgrammeException(404, "Section not found.");
            var completions = await store.Completions(enrollment.Id, token);
            if (!completions.Any(x => x.SectionId == request.SectionId))
            {
                if (module.Ordinal != Progress.Current(modules, completions)) throw new ProgrammeException(409, "Complete the current module first.");
                var next = module.Sections.OrderBy(x => x.Ordinal).First(x => !completions.Any(c => c.SectionId == x.Id));
                if (next.Id != request.SectionId) throw new ProgrammeException(409, "Complete the current section first.");
                var completion = new SectionCompletion { EnrollmentId = enrollment.Id, SectionId = request.SectionId, CompletedAt = clock.UtcNow };
                store.Add(completion); completions.Add(completion);
            }
            return new CompletionResponse(Progress.Complete(module, completions), module.Sections.OrderBy(x => x.Ordinal).FirstOrDefault(x => !completions.Any(c => c.SectionId == x.Id))?.Id, Progress.Current(modules, completions));
        }, ct);
    }
}
