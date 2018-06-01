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
    public class SynonymServiceTests
    {
        [Fact]
        public async Task GetSynonyms_ReturnsSynonyms()
        {
            // arrange
            Environment.SetEnvironmentVariable("BigHugeThesaurusApiEndpoint", "https://fake/");
            Environment.SetEnvironmentVariable("BigHugeThesaurusApiKey", "tempkey");
            var service = new SynonymService(new HttpClient(new MockHttpMessageHandler(GetFileResourceString("sample.json"))));

            // act
            var result = await service.GetSynonymsAsync("searchterm");

            // assert
            Assert.Equal(67, result.Count);
        }

        [Fact]
        public async Task GetSynonyms_ReturnsNull()
        {
            // arrange
            Environment.SetEnvironmentVariable("BigHugeThesaurusApiEndpoint", "https://fake/");
            Environment.SetEnvironmentVariable("BigHugeThesaurusApiKey", "tempkey");
            var service = new SynonymService(new HttpClient(new NotFoundHttpMessageHandler()));

            // act
            var result = await service.GetSynonymsAsync("searchterm");

            // assert
            Assert.Null(result);
        }
        
        #region Helpers
        private string GetFileResourceString(string filename)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"ContentReactor.Categories.Services.Tests.SynonymServiceSampleResponses.{filename}";
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

        private class NotFoundHttpMessageHandler : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => await Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
        #endregion
    }
}
