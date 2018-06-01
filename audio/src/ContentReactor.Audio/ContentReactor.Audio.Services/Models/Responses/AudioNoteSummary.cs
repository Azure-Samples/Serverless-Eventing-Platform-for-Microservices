using Newtonsoft.Json;

namespace ContentReactor.Audio.Services.Models.Responses
{
    public class AudioNoteSummary
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("preview")]
        public string Preview { get; set; }
    }
}
