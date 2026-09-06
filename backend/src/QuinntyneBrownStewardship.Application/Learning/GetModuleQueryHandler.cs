using MediatR;
using QuinntyneBrownStewardship.Application.Abstractions;
using QuinntyneBrownStewardship.Application.Common;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Learning;

namespace QuinntyneBrownStewardship.Application.Learning;

public sealed class GetModuleQueryHandler(ProgrammeReader reader) : IRequestHandler<GetModuleQuery, ModuleResponse>
{
    public async Task<ModuleResponse> Handle(GetModuleQuery request, CancellationToken ct)
    {
        return await reader.Module(request.Ordinal, ct);
    }
}
