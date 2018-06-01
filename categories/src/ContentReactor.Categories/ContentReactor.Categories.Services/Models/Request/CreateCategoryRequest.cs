using Newtonsoft.Json;

namespace ContentReactor.Categories.Services.Models.Request
{
    public class CreateCategoryRequest
    {
        [JsonProperty("name")]
        public string Name;
    }
}