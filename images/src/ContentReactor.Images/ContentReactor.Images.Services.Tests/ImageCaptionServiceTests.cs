using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ContentReactor.Images.Services.Tests
{
    public class ImageCaptionServiceTests
    {
        [Fact]
        public async Task GetImageCaption_ReturnsCaption()
        {
            // arrange
            Environment.SetEnvironmentVariable("CognitiveServicesVisionApiEndpoint", "https://fake/");
            Environment.SetEnvironmentVariable("CognitiveServicesVisionApiKey", "tempkey");
            var bytes = new byte[0];
            const string successResponse = "{\"description\":{\"tags\":[\"outdoor\",\"city\",\"water\",\"building\",\"cloudy\",\"background\",\"river\",\"sitting\",\"large\",\"bridge\",\"view\",\"top\",\"clouds\",\"overlooking\",\"white\",\"bench\",\"table\",\"body\",\"ocean\",\"tall\",\"standing\",\"group\",\"beach\",\"people\",\"tower\",\"street\"],\"captions\":[{\"text\":\"test description\",\"confidence\":0.9002882684209137}]},\"requestId\":\"b6a6a90c-188f-4739-945e-cbf6fa2150cf\",\"metadata\":{\"height\":400,\"width\":605,\"format\":\"Png\"}}";
            var service = new ImageCaptionService(new HttpClient(new MockHttpMessageHandler(successResponse)));

            // act
            var result = await service.GetImageCaptionAsync(bytes);

            // assert
            Assert.Equal("test description", result);
        }

        #region Helpers
        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _response;

            public MockHttpMessageHandler(string response)
            {
                _response = response;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_response)
                };

                return await Task.FromResult(responseMessage);
            }
        }
        #endregion
    }
}
