using Newtonsoft.Json;

namespace ContentReactor.Categories.Services.Models.Request
{
    public class UpdateCategoryRequest
    {
        [JsonProperty("id")]
        public string Id;
        
        [JsonProperty("name")]
        public string Name;
    }
}