using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ContentReactor.Categories.Services
{
    public interface ISynonymService
    {
        Task<IList<string>> GetSynonymsAsync(string searchTerm);
    }

    public class SynonymService : ISynonymService
    {
        private static readonly string BigHugeThesaurusApiEndpoint = Environment.GetEnvironmentVariable("BigHugeThesaurusApiEndpoint");
        private static readonly string BigHugeThesaurusApiKey = Environment.GetEnvironmentVariable("BigHugeThesaurusApiKey");

        protected readonly HttpClient HttpClient;

        public SynonymService(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        public async Task<IList<string>> GetSynonymsAsync(string searchTerm)
        {
            // construct the URI of the search request
            var uriBase = $"{BigHugeThesaurusApiEndpoint}{BigHugeThesaurusApiKey}/{UrlEncoder.Default.Encode(searchTerm)}/json";
            var uriBuilder = new UriBuilder(uriBase);
            var uriQuery = uriBuilder.ToString();

            // execute the request
            var response = await HttpClient.GetAsync(uriQuery);

            // the thesaurus API returns a 404 Not Found if it can't find any results for the specified search term
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            // if we didn't get a 404 then we expect to have received a success code
            response.EnsureSuccessStatusCode();

            // get the results
            var contentString = await response.Content.ReadAsStringAsync();
            dynamic searchResults = JObject.Parse(contentString);
            var synonyms = new List<string>();
            if (searchResults.noun?.syn is JArray nounSynonyms)
            {
                synonyms.AddRange(nounSynonyms.ToObject<string[]>());
            }
            if (searchResults.verb?.syn is JArray verbSynonyms)
            {
                synonyms.AddRange(verbSynonyms.ToObject<string[]>());
            }
            if (searchResults.adjectiveSynonyms?.syn is JArray adjectiveSynonyms)
            {
                synonyms.AddRange(adjectiveSynonyms.ToObject<string[]>());
            }

            return synonyms.Distinct().ToList();
        }
    }
}
