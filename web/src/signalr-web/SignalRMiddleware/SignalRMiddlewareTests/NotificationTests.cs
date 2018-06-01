using Xunit;
using Moq;
using SignalRMiddleware.Controllers;
using SignalRMiddleware.Services;
using Microsoft.AspNetCore.SignalR;
using SignalRMiddleware.Hubs;
using Microsoft.Extensions.Logging;

namespace SignalRMiddlewareTests
{
    public class NotificationTests
    {
        private CategoryNotificationController _categoryNotificationController;
        private NotificationService _notificationService;

        [Fact]
        public void testSignalR()
        {
            bool sendCalled = false;
            var hub = new EventHub();
            var mockClients = new Mock<IHubContext<EventHub>>();

        }


        [Fact]
        public void HandleCategoryNotificationTests()
        {
           /** Mock<IHubContext<EventHub>> mockHubContext = new Mock<IHubContext<EventHub>>();
            ILogger<NotificationService> mockLogger = Mock.Of<ILogger<NotificationService>>();
            var fakeConnectionId = "user1";
            _notificationService = new NotificationService(mockHubContext, mockLogger);
            CategoryNotificationController _categoryNotificationController = 
                new CategoryNotificationController(_notificationService);

            mockHubContext.Setup(it => 
            it.Clients.Client(fakeConnectionId).SendAsync().Returns(GetListBlog());
            // call _categoryNotificationController.HandleCategoryNotifications
            // fake event data..
            // inspect IHubContext class to see what it got.
            // 1. Mock Connections object. */
        }
    }
}
