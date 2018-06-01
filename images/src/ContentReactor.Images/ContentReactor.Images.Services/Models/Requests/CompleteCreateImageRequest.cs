using Newtonsoft.Json;

namespace ContentReactor.Images.Services.Models.Requests
{
    public class CompleteCreateImageRequest
    {
        [JsonProperty("categoryId")]
        public string CategoryId { get; set; }
    }
}