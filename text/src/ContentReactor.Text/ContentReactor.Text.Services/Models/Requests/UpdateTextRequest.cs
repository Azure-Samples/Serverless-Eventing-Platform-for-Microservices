using Newtonsoft.Json;

namespace ContentReactor.Text.Services.Models.Requests
{
    public class UpdateTextRequest
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("text")]
        public string Text;
    }
}