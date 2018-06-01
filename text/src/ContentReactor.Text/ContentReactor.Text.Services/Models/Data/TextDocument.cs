using Newtonsoft.Json;

namespace ContentReactor.Text.Services.Models.Data
{
    public class TextDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("categoryId")]
        public string CategoryId { get; set; }
    }
}
