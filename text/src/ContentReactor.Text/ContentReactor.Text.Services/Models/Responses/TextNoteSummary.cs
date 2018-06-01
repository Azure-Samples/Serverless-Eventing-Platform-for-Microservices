using Newtonsoft.Json;

namespace ContentReactor.Text.Services.Models.Responses
{
    public class TextNoteSummary
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("preview")]
        public string Preview { get; set; }
    }
}