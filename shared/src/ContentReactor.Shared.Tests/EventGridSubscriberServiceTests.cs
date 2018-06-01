using System.Collections.Generic;
using System.IO;
using ContentReactor.Shared.EventSchemas.Audio;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Xunit;

namespace ContentReactor.Shared.Tests
{
    public class EventGridSubscriberServiceTests
    {
        [Fact]
        public void HandleSubscriptionValidationEvent_ReturnsValidationResponse()
        {
            // arrange
            var requestBody = "[{\r\n  \"id\": \"2d1781af-3a4c-4d7c-bd0c-e34b19da4e66\",\r\n  \"topic\": \"/subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx\",\r\n  \"subject\": \"\",\r\n  \"data\": {\r\n    \"validationCode\": \"512d38b6-c7b8-40c8-89fe-f46f9e9622b6\"\r\n  },\r\n  \"eventType\": \"Microsoft.EventGrid.SubscriptionValidationEvent\",\r\n  \"eventTime\": \"2018-01-25T22:12:19.4556811Z\",\r\n  \"metadataVersion\": \"1\",\r\n  \"dataVersion\": \"1\"\r\n}]";
            var req = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GetStreamFromString(requestBody),
                Headers =
                {
                    new KeyValuePair<string, StringValues>(EventGridSubscriberService.EventGridSubscriptionValidationHeaderKey, new StringValues("SubscriptionValidation"))
                }
            };
            var service = new EventGridSubscriberService();

            // act
            var result = service.HandleSubscriptionValidationEvent(req);

            // assert
            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result);
            dynamic dynamicallyTypedResult = ((OkObjectResult)result).Value;
            Assert.Equal("512d38b6-c7b8-40c8-89fe-f46f9e9622b6", (string)dynamicallyTypedResult.validationResponse);
        }

        [Fact]
        public void HandleSubscriptionValidationEvent_ReturnsNullWhenNotSubscriptionValidationEvent()
        {
            // arrange
            var requestBody = "[{\r\n  \"id\": \"2d1781af-3a4c-4d7c-bd0c-e34b19da4e66\",\r\n  \"topic\": \"/subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx\",\r\n  \"subject\": \"\",\r\n  \"data\": {},\r\n  \"eventType\": \"Custom\",\r\n  \"eventTime\": \"2018-01-25T22:12:19.4556811Z\",\r\n  \"metadataVersion\": \"1\",\r\n  \"dataVersion\": \"1\"\r\n}]";
            var req = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GetStreamFromString(requestBody)
            };
            var service = new EventGridSubscriberService();

            // act
            var result = service.HandleSubscriptionValidationEvent(req);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void HandleSubscriptionValidationEvent_ThrowsWhenInvalidJsonProvided()
        {
            // arrange
            var requestBody = "invalidjson";
            var req = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GetStreamFromString(requestBody)
            };
            var service = new EventGridSubscriberService();

            // act and assert
            Assert.Throws<JsonReaderException>(() => service.HandleSubscriptionValidationEvent(req));
        }
        
        [Fact]
        public void DeconstructEventGridMessage_ParsesSingleEvent()
        {
            // arrange
            var requestBody = "[{\r\n  \"id\": \"eventid\",\r\n  \"topic\": \"topicid\",\r\n  \"subject\": \"fakeuserid/fakeitemid\",\r\n  \"data\": {},\r\n  \"eventType\": \"AudioCreated\",\r\n  \"eventTime\": \"2018-01-25T22:12:19.4556811Z\",\r\n  \"metadataVersion\": \"1\",\r\n  \"dataVersion\": \"1\"\r\n}]";
            var req = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GetStreamFromString(requestBody)
            };
            var service = new EventGridSubscriberService();

            // act
            var result = service.DeconstructEventGridMessage(req);
            
            // assert
            Assert.Equal("fakeuserid", result.userId);
            Assert.Equal("fakeitemid", result.itemId);
            Assert.NotNull(result.eventGridEvent);
            Assert.IsType<AudioCreatedEventData>(result.eventGridEvent.Data);
        }
        
        [Fact]
        public void DeconstructEventGridMessage_ThrowsWhenInvalidJsonProvided()
        {
            // arrange
            var requestBody = "invalidjson";
            var req = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GetStreamFromString(requestBody)
            };
            var service = new EventGridSubscriberService();

            // act and assert
            Assert.Throws<JsonReaderException>(() => service.DeconstructEventGridMessage(req));
        }

        #region Helpers
        private static Stream GetStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        #endregion
    }
}
