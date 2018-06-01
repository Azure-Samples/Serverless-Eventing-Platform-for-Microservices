using System.Collections.Generic;
using Newtonsoft.Json;

namespace ContentReactor.Categories.Services.Models.Data
{
    public class CategoryDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("_etag")]
        public string ETag { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonProperty("synonyms")]
        public IList<string> Synonyms { get; set; } = new List<string>();

        [JsonProperty("items")]
        public IList<CategoryItem> Items { get; set; } = new List<CategoryItem>();
    }
}
