using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SignalRMiddleware.Services
{
    public interface ICRUDService
    {
        /** Category Interfaces */
        Task<dynamic> GetCategoriesAsync(string userId);
        Task<dynamic> GetCategoryAsync(string userId, string categoryId);
        Task<dynamic> DeleteCategoryAsync(string userId,string categoryId);
        Task<dynamic> UpdateCategoriesAsync(string userId, string categoryId,string requestBody);
        Task<dynamic> CreateCategoryAsync(string userId, string requestBody);

        /** Image Interfaces */
        Task<dynamic> CreateImageUrlAsync(string userId, string requestBody);
        Task<dynamic> CreateImageAsync(string userId, string imageId, string requestBody);
        Task<dynamic> GetImageAsync(string userId, string imageId);
        Task<dynamic> GetImagesAsync(string userId);
        Task<dynamic> DeleteImageAsync(string userId, string imageId);

        /** Audio Interfaces */
        Task<dynamic> CreateAudioUrlAsync(string userId, string requestBody);
        Task<dynamic> CreateAudioAsync(string userId, string audioId, string requestBody);
        Task<dynamic> GetAudioAsync(string userId, string audioId);
        Task<dynamic> GetAudiosAsync(string userId);
        Task<dynamic> DeleteAudioAsync(string userId, string audioId);

        /** Text Interfaces */
        Task<dynamic> CreateTextAsync(string userId,string requestBody);
        Task<dynamic> GetTextAsync(string userId, string textId);
        Task<dynamic> ListTextAsync(string userId);
        Task<dynamic> UpdateTextAsync(string userId, string textId,string requestBody);
        Task<dynamic> DeleteTextAsync(string userId, string textId);
    }
    public class CRUDService : ICRUDService
    {
        static HttpClient client = new HttpClient();
        const string jsonMimeType = "application/json";
        static string functionsApiProxy = Environment.GetEnvironmentVariable("FUNCTION_API_PROXY_ROOT", EnvironmentVariableTarget.Process);

        /** HTTP Methods */
        private static async Task<string> HttpMethodAsync(string verb,string url, HttpContent httpContent)
        {
            string responseBody = "";
            HttpResponseMessage response = new HttpResponseMessage();
            switch (verb)
            {
                case "GET":
                    response = await client.GetAsync(url);
                    break;

                case "POST":
                        response = await client.PostAsync(url, httpContent);
                        break;

                case "DELETE":
                        response = await client.DeleteAsync(url);
                        break;

                case "PATCH":
                        response = await client.PatchAsync(url, httpContent);
                        break;
            }

            response.EnsureSuccessStatusCode();
            responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }

        /** CATEGORY CRUD Methods */
        public async Task<dynamic> GetCategoriesAsync(string userId)
        {
            string url = functionsApiProxy + "/categories?userId=" + userId;
            return await CRUDService.HttpMethodAsync("GET",url,null);
            
        }

        public async Task<dynamic> GetCategoryAsync(string userId, string categoryId)
        {
            string url = functionsApiProxy + "/categories/" + categoryId + "/?userId=" + userId;
            return await CRUDService.HttpMethodAsync("GET",url,null);
        }

        public async Task<dynamic> DeleteCategoryAsync(string userId,string categoryId)
        {
            string url = functionsApiProxy + "/categories/" + categoryId + "/?userId=" + userId;
            return await CRUDService.HttpMethodAsync("DELETE",url,null);
        }

        public async Task<dynamic> UpdateCategoriesAsync(string userId,string categoryId, string requestBody)
        {
            string url = functionsApiProxy + "/categories/" + categoryId + "/?userId=" + userId;
            var httpContent = new StringContent(requestBody, Encoding.UTF8, jsonMimeType);
            return await CRUDService.HttpMethodAsync("PATCH", url, httpContent);
        }

        public async Task<dynamic> CreateCategoryAsync(string userId, string requestBody)
        {
            string url = functionsApiProxy + "/categories?userId=" + userId;
            var httpContent = new StringContent(requestBody, Encoding.UTF8, jsonMimeType);
            return await CRUDService.HttpMethodAsync("POST", url, httpContent);
        }

        /** IMAGES API */

        public async Task<dynamic> CreateImageUrlAsync(string userId,string requestBody)
        {
            string url = functionsApiProxy + "/images?userId=" + userId;
            var httpContent = new StringContent(requestBody, Encoding.UTF8, jsonMimeType);
            return await CRUDService.HttpMethodAsync("POST", url, httpContent);
        }

        public async Task<dynamic> CreateImageAsync(string userId,string imageId,string requestBody)
        {
            string url = functionsApiProxy + "/images/" + imageId + "/?userId=" + userId;
            var httpContent = new StringContent(requestBody, Encoding.UTF8, jsonMimeType);
            return await CRUDService.HttpMethodAsync("POST", url, httpContent);
        }

        public async Task<dynamic> GetImageAsync(string userId,string imageId)
        {
            string url = functionsApiProxy + "/images/" + imageId + "/?userId=" + userId;
            return await CRUDService.HttpMethodAsync("GET", url,null);
        }

        public async Task<dynamic> GetImagesAsync(string userId)
        {
            string url = functionsApiProxy + "/images?userId=" + userId;
            return await CRUDService.HttpMethodAsync("GET", url, null);
        }

        public async Task<dynamic> DeleteImageAsync(string userId,string imageId)
        {
            string url = functionsApiProxy + "/images/" + imageId + "/?userId=" + userId;
            return await CRUDService.HttpMethodAsync("DELETE", url, null);
        }

        /** AUDIO API */

        public async Task<dynamic> CreateAudioUrlAsync(string userId, string requestBody)
        {
            string url = functionsApiProxy + "/audio?userId=" + userId;
            var httpContent = new StringContent(requestBody, Encoding.UTF8, jsonMimeType);
            return await CRUDService.HttpMethodAsync("POST", url, httpContent);
        }

        public async Task<dynamic> CreateAudioAsync(string userId, string audioId, string requestBody)
        {
            string url = functionsApiProxy + "/audio/" + audioId + "/?userId=" + userId;
            var httpContent = new StringContent(requestBody, Encoding.UTF8, jsonMimeType);
            return await CRUDService.HttpMethodAsync("POST", url, httpContent);
        }

        public async Task<dynamic> GetAudioAsync(string userId, string audioId)
        {
            string url = functionsApiProxy + "/audio/" + audioId + "/?userId=" + userId;
            return await CRUDService.HttpMethodAsync("GET", url, null);
        }

        public async Task<dynamic> GetAudiosAsync(string userId)
        {
            string url = functionsApiProxy + "/audio?userId=" + userId;
            return await CRUDService.HttpMethodAsync("GET", url, null);
        }

        public async Task<dynamic> DeleteAudioAsync(string userId, string audioId)
        {
            string url = functionsApiProxy + "/audio/" + audioId + "/?userId=" + userId;
            return await CRUDService.HttpMethodAsync("DELETE", url, null);
        }

        /** TEXT API */

        public async Task<dynamic> CreateTextAsync(string userId,string requestBody)
        {
            string url = functionsApiProxy + "/text?userId=" + userId;
            var httpContent = new StringContent(requestBody, Encoding.UTF8, jsonMimeType);
            return await CRUDService.HttpMethodAsync("POST", url, httpContent);
        }

        public async Task<dynamic> GetTextAsync(string userId, string textId)
        {
            string url = functionsApiProxy + "/text/" + textId + "/?userId=" + userId;
            return await CRUDService.HttpMethodAsync("GET", url, null);
        }

        public async Task<dynamic> ListTextAsync(string userId)
        {
            string url = functionsApiProxy + "/text?userId=" + userId;
            return await CRUDService.HttpMethodAsync("GET", url, null);
        }

        public async Task<dynamic> UpdateTextAsync(string userId,string textId, string requestBody)
        {
            string url = functionsApiProxy + "/text/" + textId + "/?userId=" + userId;
            var httpContent = new StringContent(requestBody, Encoding.UTF8, jsonMimeType);
            return await CRUDService.HttpMethodAsync("PATCH", url, httpContent);
        }

        public async Task<dynamic> DeleteTextAsync(string userId, string textId)
        {
            string url = functionsApiProxy + "/text/" + textId + "/?userId=" + userId;
            return await CRUDService.HttpMethodAsync("DELETE", url, null);
        }
    }
}
