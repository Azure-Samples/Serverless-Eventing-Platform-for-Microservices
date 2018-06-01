using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using ContentReactor.Categories.Services;
using ContentReactor.Categories.Services.Converters;
using ContentReactor.Categories.Services.Models.Request;
using ContentReactor.Categories.Services.Models.Results;
using ContentReactor.Categories.Services.Repositories;
using ContentReactor.Shared;
using ContentReactor.Shared.UserAuthentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace ContentReactor.Categories.Api
{
    public static class ApiFunctions
    {
        private const string JsonContentType = "application/json";
        private static readonly ICategoriesService CategoriesService = new CategoriesService(new CategoriesRepository(), new ImageSearchService(new Random(), new HttpClient()), new SynonymService(new HttpClient()), new EventGridPublisherService());
        public static IUserAuthenticationService UserAuthenticationService = new QueryStringUserAuthenticationService();

        [FunctionName("AddCategory")]
        public static async Task<IActionResult> AddCategory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "categories")]HttpRequest req,
            TraceWriter log)
        {
            // get the request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            CreateCategoryRequest data;
            try
            {
                data = JsonConvert.DeserializeObject<CreateCategoryRequest>(requestBody);
            }
            catch (JsonReaderException)
            {
                return new BadRequestObjectResult(new { error = "Body should be provided in JSON format." });
            }

            // validate request
            if (data == null || string.IsNullOrEmpty(data.Name))
            {
                return new BadRequestObjectResult(new { error = "Missing required property 'name'." });
            }

            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }

            // create category
            try
            {
                var categoryId = await CategoriesService.AddCategoryAsync(data.Name, userId);
                return new OkObjectResult(new { id = categoryId });
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }

        [FunctionName("DeleteCategory")]
        public static async Task<IActionResult> DeleteCategory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "categories/{id}")]HttpRequest req,
            TraceWriter log,
            string id)
        {
            // validate request
            if (string.IsNullOrEmpty(id))
            {
                return new BadRequestObjectResult(new { error = "Missing required argument 'id'." });
            }

            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }

            // delete category
            try
            {
                await CategoriesService.DeleteCategoryAsync(id, userId); // we ignore the result of this call - whether it's Success or NotFound, we return an 'Ok' back to the client
                return new NoContentResult();
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }

        [FunctionName("UpdateCategory")]
        public static async Task<IActionResult> UpdateCategory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "categories/{id}")]HttpRequest req,
            TraceWriter log,
            string id)
        {
            // get the request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            UpdateCategoryRequest data;
            try
            {
                data = JsonConvert.DeserializeObject<UpdateCategoryRequest>(requestBody);
            }
            catch (JsonReaderException)
            {
                return new BadRequestObjectResult(new { error = "Body should be provided in JSON format." });
            }

            // validate request
            if (data == null)
            {
                return new BadRequestObjectResult(new { error = "Missing required property 'name'." });
            }
            if (data.Id != null && id != null && data.Id != id)
            {
                return new BadRequestObjectResult(new { error = "Property 'id' does not match the identifier specified in the URL path." });
            }
            if (string.IsNullOrEmpty(data.Id))
            {
                data.Id = id;
            }
            if (string.IsNullOrEmpty(data.Name))
            {
                return new BadRequestObjectResult(new { error = "Missing required property 'name'." });
            }

            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }

            // update category name
            try
            {
                var result = await CategoriesService.UpdateCategoryAsync(data.Id, userId, data.Name);
                if (result == UpdateCategoryResult.NotFound)
                {
                    return new NotFoundResult();
                }

                return new NoContentResult();
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }

        [FunctionName("GetCategory")]
        public static async Task<IActionResult> GetCategory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "categories/{id}")]HttpRequest req,
            TraceWriter log,
            string id)
        {
            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }

            // get the category details
            try
            {
                var document = await CategoriesService.GetCategoryAsync(id, userId);
                if (document == null)
                {
                    return new NotFoundResult();
                }

                return new OkObjectResult(document);
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }

        [FunctionName("ListCategories")]
        public static async Task<IActionResult> ListCategories(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "categories")]HttpRequest req,
            TraceWriter log)
        {
            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }

            // list the categories
            try
            {
                var summaries = await CategoriesService.ListCategoriesAsync(userId);
                if (summaries == null)
                {
                    return new NotFoundResult();
                }

                // serialise the summaries using a custom converter
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };
                settings.Converters.Add(new CategorySummariesConverter());
                var json = JsonConvert.SerializeObject(summaries, settings);

                return new ContentResult
                {
                    Content = json,
                    ContentType = JsonContentType,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }
    }
}
