using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignalRMiddleware.Services;

namespace SignalRMiddleware.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class ApplicationSettingController : Controller
    {
        private readonly ILogger _logger;
        private readonly ICRUDService _crudService;

        public ApplicationSettingController(ILogger<ApplicationSettingController> logger, ICRUDService crudService)
        {
            _logger = logger;
            _crudService = crudService;
        }

        [HttpGet]
        public IActionResult GetApplicationInsightsInstrumentationKey()
        {
            try
            {
                return Ok(new { key = _crudService.GetAppInsightskey() });
            }
            catch (Exception e)
            {
                _logger.LogError("Get AppInsightsKey Failed " + e);
                return BadRequest(e.Message);
            }
        }
    }
}