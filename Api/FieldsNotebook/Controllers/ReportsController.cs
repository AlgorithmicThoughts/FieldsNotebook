using DataContract.Reports;
using Managers;
using Managers.Reports;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(ManagerFactory managerFactory) : ControllerBase
{
    private readonly IReportManager _reportManager = managerFactory.CreateManager<IReportManager>();

    [HttpGet("delays", Name = "GetDelayReport")]
    [ProducesResponseType<IReadOnlyList<DelayReportRowResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<DelayReportRowResponse>>> GetDelayReport([FromQuery] DelayReportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reportManager.GetDelayReportAsync(DemoIdentity.GetUserId(HttpContext), request, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpGet("delay-tags", Name = "GetDelayTagNames")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetDelayTagNames(CancellationToken cancellationToken)
    {
        var tagNames = await _reportManager.GetDelayTagNamesAsync(cancellationToken);

        return Ok(tagNames);
    }
}
