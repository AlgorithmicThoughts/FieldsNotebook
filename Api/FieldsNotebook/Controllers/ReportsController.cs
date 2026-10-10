using DataContract.Reports;
using Managers;
using Managers.Reports;
using Microsoft.AspNetCore.Mvc;

namespace FieldsNotebook.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(ManagerFactory managerFactory) : ControllerBase
{
    [HttpGet("delays", Name = "GetDelayReport")]
    [ProducesResponseType<IReadOnlyList<DelayReportRowResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IReadOnlyList<DelayReportRowResponse>> GetDelayReport([FromQuery] DelayReportRequest request)
    {
        var manager = managerFactory.CreateManager<IReportManager>();
        return manager.GetDelayReportAsync(DemoIdentity.GetUserId(HttpContext), request, HttpContext.RequestAborted);
    }

    [HttpGet("delay-tags", Name = "GetDelayTagNames")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IReadOnlyList<string>> GetDelayTagNames()
    {
        var manager = managerFactory.CreateManager<IReportManager>();
        return manager.GetDelayTagNamesAsync(DemoIdentity.GetUserId(HttpContext), HttpContext.RequestAborted);
    }
}
