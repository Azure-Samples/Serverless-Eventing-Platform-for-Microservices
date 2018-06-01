using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ContentReactor.Categories.Services.Models.Response
{
    public class CategoryItemDetails
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemType Type { get; set; }

        [JsonProperty("preview")]
        public string Preview { get; set; }
    }
}