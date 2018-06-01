using Newtonsoft.Json;

namespace ContentReactor.Categories.Services.Models.Response
{
    public class CategorySummary
    {
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
