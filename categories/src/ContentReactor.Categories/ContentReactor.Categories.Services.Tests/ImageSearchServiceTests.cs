using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ContentReactor.Categories.Services.Tests
{
    public class ImageSearchServiceTests
    {
        [Fact]
        public async Task FindImageUrl_ReturnsExpectedUrl()
        {
            // arrange
            Environment.SetEnvironmentVariable("CognitiveServicesSearchApiEndpoint", "https://fake/");
            Environment.SetEnvironmentVariable("CognitiveServicesSearchApiKey", "tempkey");
            var service = new ImageSearchService(new Random(), new HttpClient(new MockHttpMessageHandler(GetFileResourceString("1.json"))));

            // act
            var result = await service.FindImageUrlAsync("searchterm");

            // assert
            Assert.Equal("http://images2.fanpop.com/image/photos/9400000/Funny-Cats-cats-9473312-1600-1200.jpg", result);
        }

        [Fact]
        public async Task FindImageUrl_ReturnsNull()
        {
            // arrange
            Environment.SetEnvironmentVariable("CognitiveServicesSearchApiEndpoint", "https://fake/");
            Environment.SetEnvironmentVariable("CognitiveServicesSearchApiKey", "tempkey");
            var service = new ImageSearchService(new Random(), new HttpClient(new MockHttpMessageHandler(GetFileResourceString("0.json"))));

            // act
            var result = await service.FindImageUrlAsync("searchterm");

            // assert
            Assert.Null(result);
        }
        
        #region Helpers
        private string GetFileResourceString(string filename)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"ContentReactor.Categories.Services.Tests.ImageSearchServiceSampleResponses.{filename}";
            var stream = assembly.GetManifestResourceStream(resourceName);
            using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

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
