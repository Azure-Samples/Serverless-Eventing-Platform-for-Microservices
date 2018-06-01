using Newtonsoft.Json;

namespace ContentReactor.Images.Services.Models.Responses
{
    public class ImageNoteDetails
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonProperty("previewUrl")]
        public string PreviewUrl { get; set; }
        
        [JsonProperty("caption")]
        public string Caption { get; set; }
    }
}
