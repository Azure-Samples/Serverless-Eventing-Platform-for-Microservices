using System.Collections.Generic;
using Newtonsoft.Json;

namespace ContentReactor.Categories.Services.Models.Response
{
    public class CategoryDetails
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonProperty("synonyms")]
        public IList<string> Synonyms { get; set; }

        [JsonProperty("items")]
        public IList<CategoryItemDetails> Items { get; set; }
    }
}
