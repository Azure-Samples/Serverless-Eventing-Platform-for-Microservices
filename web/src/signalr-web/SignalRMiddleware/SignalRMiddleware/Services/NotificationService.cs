using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SignalRMiddleware.Hubs;
using SignalRMiddleware.Models;
using System.Collections.Generic;

namespace SignalRMiddleware.Services
{
    public interface INotificationService
    {
        IActionResult HandleCategoryNotification(IList<CategoryEventData> e);
        IActionResult HandleImageNotification(IList<ImageEventData> e);
        IActionResult HandleAudioNotification(IList<AudioEventData> e);
        IActionResult HandleTextNotification(IList<TextEventData> e);
        IActionResult SendEventValidationResponse(string code);
    }
    public class NotificationService : INotificationService
    {
        private IHubContext<EventHub> _context;
        private readonly ILogger _logger;

        public NotificationService(IHubContext<EventHub> eventHubContext,ILogger<NotificationService> logger)
        {
            _context = eventHubContext;
            _logger = logger;
        }

        public IActionResult SendEventValidationResponse(string code)
        {
            _logger.LogInformation("Received validation request from event grid : " + code);
            EventValidationResponse response = new EventValidationResponse();
            response.ValidationResponse = code;
            return new OkObjectResult(response);
        }

        /** Category Event Handler */
        public IActionResult HandleCategoryNotification(IList<CategoryEventData> e)
        {
            if(e[0].Data.ValidationCode != null)
            {
                return this.SendEventValidationResponse(e[0].Data.ValidationCode);
            }
            else
            {
                string[] ids = e[0].Subject.Split('/');
                string userName = ids[0];
                string categoryId = ids[1];

                _logger.LogInformation("Received category event {0} for user {1}", categoryId , userName);

                foreach (var connectionId in Connections._connections.GetConnections(userName))
                {
                    foreach (var n_event in e)
                    {
                        string notificationType = EventMapperService.getMappedEvent(n_event.EventType);
                        _context.Clients.Client(connectionId).SendAsync(notificationType, categoryId, n_event.Data);
                    }
                }
                return new OkResult();
            }
        }

        /** Image Event Handler */

        public IActionResult HandleImageNotification(IList<ImageEventData> e)
        {
            if (e[0].Data.ValidationCode != null)
            {
                return this.SendEventValidationResponse(e[0].Data.ValidationCode);
            }
            else
            {
                string[] ids = e[0].Subject.Split('/');
                string userName = ids[0];
                string imageId = ids[1];

                _logger.LogInformation("Received image event {0} for user {1}", imageId, userName);

                foreach (var connectionId in Connections._connections.GetConnections(userName))
                {
                    foreach (var n_event in e)
                    {
                        string notificationType = EventMapperService.getMappedEvent(n_event.EventType);
                        _context.Clients.Client(connectionId).SendAsync(notificationType, imageId, n_event.Data);
                    }
                }
                return new OkResult();
            }
        }

        /** Audio Event Handler */

        public IActionResult HandleAudioNotification(IList<AudioEventData> e)
        {
            if (e[0].Data.ValidationCode != null)
            {
                return this.SendEventValidationResponse(e[0].Data.ValidationCode);
            }
            else
            {
                string[] ids = e[0].Subject.Split('/');
                string userName = ids[0];
                string audioId = ids[1];

                _logger.LogInformation("Received audio event {0} for user {1}", audioId, userName);

                foreach (var connectionId in Connections._connections.GetConnections(userName))
                {
                    foreach (var n_event in e)
                    {
                        string notificationType = EventMapperService.getMappedEvent(n_event.EventType);
                        _context.Clients.Client(connectionId).SendAsync(notificationType, audioId, n_event.Data);
                    }
                }
                return new OkResult();
            }
        }

        /** Text Event Handler */

        public IActionResult HandleTextNotification(IList<TextEventData> e)
        {
            if (e[0].Data.ValidationCode != null)
            {
                return this.SendEventValidationResponse(e[0].Data.ValidationCode);
            }
            else
            {
                string[] ids = e[0].Subject.Split('/');
                string userName = ids[0];
                string textId = ids[1];

                _logger.LogInformation("Received text event {0} for user {1}", textId, userName);

                foreach (var connectionId in Connections._connections.GetConnections(userName))
                {
                    foreach (var n_event in e)
                    {
                        string notificationType = EventMapperService.getMappedEvent(n_event.EventType);
                        _context.Clients.Client(connectionId).SendAsync(notificationType, textId, n_event.Data);
                    }
                }
                return new OkResult();
            }
        }

    }
}

