using Newtonsoft.Json;

namespace ContentReactor.Text.Services.Models.Requests
{
    public class CreateTextRequest
    {
        [JsonProperty("text")]
        public string Text;

        [JsonProperty("categoryId")]
        public string CategoryId;
    }
}