using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Administration.Modules;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration.Prompts;

public sealed class RemovePromptCommandHandler(ICurriculumStore store, CurriculumAuditor auditor) : IRequestHandler<RemovePromptCommand>
{
    public Task Handle(RemovePromptCommand request, CancellationToken ct) => store.Transaction<object?>(async token =>
    {
        var prompt = await store.RequiredPrompt(request.Id, token);
        var answers = await store.PromptAnswerCount(prompt.Id, token);
        if (answers > 0) throw new ProgrammeException(409, $"{answers} participant{(answers == 1 ? " has" : "s have")} answered this prompt. It cannot be removed while {(answers == 1 ? "that answer stands" : "their answers stand")}.");
        var module = await store.RequiredModule(prompt.ModuleId, token);
        store.Remove(prompt);
        await store.Flush(token);
        OrdinalSequence.Compact(module.PreparationPrompts.Where(x => x.Id != prompt.Id).ToList());
        auditor.Record("PromptRemoved", prompt.Id);
        return null;
    }, ct);
}
