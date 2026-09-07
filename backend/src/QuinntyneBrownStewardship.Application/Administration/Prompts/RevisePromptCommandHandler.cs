using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Administration.Modules;
namespace QuinntyneBrownStewardship.Application.Administration.Prompts;

public sealed class RevisePromptCommandHandler(ICurriculumStore store, CurriculumAuditor auditor) : IRequestHandler<RevisePromptCommand>
{
    public Task Handle(RevisePromptCommand request, CancellationToken ct) => store.Transaction<object?>(async token =>
    {
        var prompt = await store.RequiredPrompt(request.Id, token);
        prompt.Text = request.Text;
        auditor.Record("PromptRevised", prompt.Id);
        return null;
    }, ct);
}
