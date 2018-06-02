using Xunit;
using Moq;
using SignalRMiddleware.Services;
using SignalRMiddleware.Hubs;
using SignalRMiddleware.Models;
using SignalRMiddlewareTests.TestProxies;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;


namespace SignalRMiddlewareTests
{
    public class NotificationTests
    {
        private NotificationService _notificationService;
        private Mock<IHubContext<EventHub>> _mockContext;
        private ILogger<NotificationService> _logger;
        private Mock<IHubClients> _mockClients;
        public NotificationTests()
        {
            // Initialize mocks
            _mockContext = new Mock<IHubContext<EventHub>>();
            _logger = new Mock<ILogger<NotificationService>>().Object;
            _mockClients = new Mock<IHubClients>();
            _mockContext.Setup(mock => mock.Clients).Returns(_mockClients.Object);
            _notificationService =
                new NotificationService(_mockContext.Object, _logger);
        }

        [Fact]
        public void Test_EventGrid_ValidationResponse()
        {
            IList<CategoryEventData> eventList = NotificationTestsSetup.SetupValidationCodeData();
            IActionResult result = _notificationService.HandleCategoryNotification(eventList);
            var okObjectResult = result as OkObjectResult;
            var response = okObjectResult.Value as EventValidationResponse;
            Assert.Equal("testValidationCode", response.ValidationResponse);
        }

        [Fact]
        public void Test_Category_ImageUpdatedEvent()
        {
            IList<CategoryEventData> eventList = NotificationTestsSetup.SetupCategoryImageData();

            // Setup connections for the same user foo
            Connections._connections.Add("foo", "conn1");
            _mockClients.Setup(mock => mock.Client("conn1")).Returns(new CategoryImageUpdatedProxy());

            IActionResult result = _notificationService.HandleCategoryNotification(eventList);

        }

        [Fact]
        public void Test_Category_SynonymsUpdatedEvent()
        {
            IList<CategoryEventData> eventList = NotificationTestsSetup.SetupCategorySynonymsData();

            // Setup connections for the same user foo
            Connections._connections.Add("foo", "conn1");
            _mockClients.Setup(mock => mock.Client("conn1")).Returns(new CategorySynonymsUpdatedProxy());

            IActionResult result = _notificationService.HandleCategoryNotification(eventList);

        }

        [Fact]
        public void Test_Image_CaptionUpdatedEvent()
        {
            IList<ImageEventData> eventList = NotificationTestsSetup.SetupImageCaptionData();

            // Setup connections for the same user foo
            Connections._connections.Add("foo", "conn1");
            _mockClients.Setup(mock => mock.Client("conn1")).Returns(new ImageCaptionUpdatedProxy());

            IActionResult result = _notificationService.HandleImageNotification(eventList);

        }
        [Fact]
        public void Test_Audio_TranscriptUpdatedEvent()
        {
            IList<AudioEventData> eventList = NotificationTestsSetup.SetupAudioTranscriptData();

            // Setup connections for the same user foo
            Connections._connections.Add("foo", "conn1");
            _mockClients.Setup(mock => mock.Client("conn1")).Returns(new AudioTranscriptUpdatedProxy());

            IActionResult result = _notificationService.HandleAudioNotification(eventList);

        }

        [Fact]
        public void Test_Text_TextUpdatedEvent()
        {
            IList<TextEventData> eventList = NotificationTestsSetup.SetupTextData();

            // Setup connections for the same user foo
            Connections._connections.Add("foo", "conn1");
            _mockClients.Setup(mock => mock.Client("conn1")).Returns(new TextUpdatedProxy());

            IActionResult result = _notificationService.HandleTextNotification(eventList);

        }

        /** Develop further tests here ... */


    }
}
