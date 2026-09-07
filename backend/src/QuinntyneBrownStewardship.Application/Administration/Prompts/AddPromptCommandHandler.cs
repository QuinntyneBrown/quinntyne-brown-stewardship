using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Administration.Modules;
using QuinntyneBrownStewardship.Domain.Learning;
namespace QuinntyneBrownStewardship.Application.Administration.Prompts;

public sealed class AddPromptCommandHandler(ICurriculumStore store, CurriculumAuditor auditor) : IRequestHandler<AddPromptCommand, Guid>
{
    public Task<Guid> Handle(AddPromptCommand request, CancellationToken ct) => store.Transaction(async token =>
    {
        var module = await store.RequiredModule(request.ModuleId, token);
        var prompt = new PreparationPrompt { ModuleId = module.Id, Ordinal = OrdinalSequence.Next(module.PreparationPrompts), Text = request.Text };
        store.Add(prompt);
        auditor.Record("PromptAdded", prompt.Id);
        return prompt.Id;
    }, ct);
}
