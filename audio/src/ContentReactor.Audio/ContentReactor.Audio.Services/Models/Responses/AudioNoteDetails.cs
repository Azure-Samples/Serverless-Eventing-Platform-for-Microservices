using Newtonsoft.Json;

namespace ContentReactor.Audio.Services.Models.Responses
{
    public class AudioNoteDetails
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("audioUrl")]
        public string AudioUrl { get; set; }

        [JsonProperty("transcript")]
        public string Transcript { get; set; }
    }
}
