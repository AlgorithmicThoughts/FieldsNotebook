using DataContract.Reports;
using Managers;
using Managers.Delays;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(ManagerFactory managerFactory) : ControllerBase
{
    private readonly IDelayInsightManager _insightManager = managerFactory.CreateManager<IDelayInsightManager>();

    [HttpGet("delays", Name = "GetDelayReport")]
    [ProducesResponseType<IReadOnlyList<DelayReportRowResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<DelayReportRowResponse>>> GetDelayReport([FromQuery] DelayReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _insightManager.GetDelayReportAsync(DemoIdentity.GetUserId(HttpContext), request, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpGet("delay-tags", Name = "GetDelayTagNames")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetDelayTagNames(CancellationToken cancellationToken)
    {
        var result = await _insightManager.GetDelayTagNamesAsync(DemoIdentity.GetUserId(HttpContext), cancellationToken);

        return this.ToActionResult(result);
    }
}
