using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ContentReactor.Images.Services
{
    public interface IImageCaptionService
    {
        Task<string> GetImageCaptionAsync(byte[] imageBytes);
    }

    public class ImageCaptionService : IImageCaptionService
    {
        protected readonly HttpClient HttpClient;

        private static readonly string CognitiveServicesVisionApiEndpoint = Environment.GetEnvironmentVariable("CognitiveServicesVisionApiEndpoint");
        private static readonly string CognitiveServicesVisionApiKey = Environment.GetEnvironmentVariable("CognitiveServicesVisionApiKey");

        public ImageCaptionService(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        public async Task<string> GetImageCaptionAsync(byte[] imageBytes)
        {
            HttpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", CognitiveServicesVisionApiKey);
            
            var uri = CognitiveServicesVisionApiEndpoint + "/analyze?visualFeatures=Description&language=en";
            
            using (var content = new ByteArrayContent(imageBytes))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                var response = await HttpClient.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();

                // read the response text and find the description
                var contentString = await response.Content.ReadAsStringAsync();
                dynamic responseJson = JObject.Parse(contentString);
                var description = responseJson.description;
                if (description == null)
                {
                    return null;
                }
                var captions = (JArray)description.captions;
                if (captions == null || captions.Count == 0)
                {
                    return null;
                }
                dynamic caption = (JObject)captions[0];
                
                return caption.text;
            }
        }
    }
}
