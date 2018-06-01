using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SignalRMiddleware.Models;
using SignalRMiddleware.Services;

namespace SignalRMiddleware.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class ImageNotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        public ImageNotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // POST: api/ImageNotification
        [HttpPost]
        public IActionResult Post([FromBody] IList<ImageEventData> e)
        {
            return _notificationService.HandleImageNotification(e);
        }
    }
}