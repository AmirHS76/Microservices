using ApiResponses;
using SSO.Api.Contracts;
using SSO.Application.UseCases.Login;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SSO.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/sso/audits")]
public sealed class SsoAuditController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAudits(
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var audits = await mediator.Send(new GetLoginAuditsQuery(), cancellationToken);
        var auditDtos = audits
            .Select(x => new LoginAuditDto(x.UserId, x.Username, x.OccurredAtUtc))
            .ToArray();
        var page = auditDtos.Paginate(pagination);
        var response = ApiResponse<IReadOnlyCollection<LoginAuditDto>>.Ok(page.Items, "Login audits returned successfully.", page.Pagination);

        return Ok(response);
    }
}
