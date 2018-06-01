using Newtonsoft.Json;

namespace ContentReactor.Text.Services.Models.Responses
{
    public class TextNoteDetails
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
