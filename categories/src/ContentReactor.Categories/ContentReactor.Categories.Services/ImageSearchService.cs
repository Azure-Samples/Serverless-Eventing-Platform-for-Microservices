using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace ContentReactor.Categories.Services
{
    public interface IImageSearchService
    {
        Task<string> FindImageUrlAsync(string searchTerm);
    }

    public class ImageSearchService : IImageSearchService
    {
        private static readonly string CognitiveServicesSearchApiEndpoint = Environment.GetEnvironmentVariable("CognitiveServicesSearchApiEndpoint");
        private static readonly string CognitiveServicesSearchApiKey = Environment.GetEnvironmentVariable("CognitiveServicesSearchApiKey");

        protected readonly HttpClient HttpClient;
        protected readonly Random Random;

        public ImageSearchService(Random random, HttpClient httpClient)
        {
            Random = random;
            HttpClient = httpClient;
        }

        public async Task<string> FindImageUrlAsync(string searchTerm)
        {
            HttpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", CognitiveServicesSearchApiKey);

            // construct the URI of the search request
            var uriBuilder = new UriBuilder(CognitiveServicesSearchApiEndpoint);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["q"] = searchTerm;
            uriBuilder.Query = query.ToString();
            var uriQuery = uriBuilder.ToString();

            // execute the request
            var response = await HttpClient.GetAsync(uriQuery);
            response.EnsureSuccessStatusCode();
            
            // get the results
            var contentString = await response.Content.ReadAsStringAsync();
            dynamic responseJson = JObject.Parse(contentString);
            var results = (JArray)responseJson.value;
            if (results.Count == 0)
            {
                return null;
            }

            // pick a random result
            var index = Random.Next(0, results.Count - 1);
            var topResult = (dynamic)results[index];
            return topResult.contentUrl;
        }
    }
}
