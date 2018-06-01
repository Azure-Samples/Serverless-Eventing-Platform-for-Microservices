using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SignalRMiddleware.Models;
using SignalRMiddleware.Services;

namespace SignalRMiddleware.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class CategoryNotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        public CategoryNotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // POST: api/CategoryNotification
        [HttpPost]
        public IActionResult Post([FromBody] IList<CategoryEventData> e)
        {
            return _notificationService.HandleCategoryNotification(e);
        }
    }
}