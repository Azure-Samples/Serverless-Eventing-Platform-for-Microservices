using Newtonsoft.Json;

namespace ContentReactor.Audio.Services.Models.Requests
{
    public class CompleteCreateAudioRequest
    {
        [JsonProperty("categoryId")]
        public string CategoryId { get; set; }
    }
}
