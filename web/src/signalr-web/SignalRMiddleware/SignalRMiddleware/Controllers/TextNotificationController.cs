using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SignalRMiddleware.Models;
using SignalRMiddleware.Services;

namespace SignalRMiddleware.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class TextNotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        public TextNotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // POST: api/TextNotification
        [HttpPost]
        public IActionResult Post([FromBody] IList<TextEventData> e)
        {
            return _notificationService.HandleTextNotification(e);
        }
    }
}