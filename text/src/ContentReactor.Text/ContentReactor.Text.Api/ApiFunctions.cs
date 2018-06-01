using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using ContentReactor.Shared;
using ContentReactor.Shared.UserAuthentication;
using ContentReactor.Text.Services;
using ContentReactor.Text.Services.Converters;
using ContentReactor.Text.Services.Models.Requests;
using ContentReactor.Text.Services.Models.Results;
using ContentReactor.Text.Services.Repositories;

namespace ContentReactor.Text.Api
{
    public static class ApiFunctions
    {
        private const string JsonContentType = "application/json";
        public static ITextService TextService = new TextService(new TextRepository(), new EventGridPublisherService());
        public static IUserAuthenticationService UserAuthenticationService = new QueryStringUserAuthenticationService();

        [FunctionName("AddText")]
        public static async Task<IActionResult> AddText(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "text")]HttpRequest req,
            TraceWriter log)
        {
            // get the request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            CreateTextRequest data;
            try
            {
                data = JsonConvert.DeserializeObject<CreateTextRequest>(requestBody);
            }
            catch (JsonReaderException)
            {
                return new BadRequestObjectResult(new { error = "Body should be provided in JSON format." });
            }

            // validate request
            if (data == null)
            {
                return new BadRequestObjectResult(new { error = "Missing required properties 'text', and 'categoryId'." });
            }
            else if (string.IsNullOrEmpty(data.Text))
            {
                return new BadRequestObjectResult(new { error = "Missing required property 'text'." });
            }
            else if (string.IsNullOrEmpty(data.CategoryId))
            {
                return new BadRequestObjectResult(new { error = "Missing required property 'categoryId'." });
            }

            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }

            // create text
            try
            {
                var textId = await TextService.AddTextNoteAsync(data.Text, userId, data.CategoryId);
                return new OkObjectResult(new { id = textId });
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }

        [FunctionName("DeleteText")]
        public static async Task<IActionResult> DeleteText(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "text/{id}")]HttpRequest req,
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

            // delete text
            try
            {
                await TextService.DeleteTextNoteAsync(id, userId); // we ignore the result of this call - whether it's Success or NotFound, we return an 'Ok' back to the client
                return new NoContentResult();
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }

        [FunctionName("UpdateText")]
        public static async Task<IActionResult> UpdateText(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "text/{id}")]HttpRequest req,
            TraceWriter log,
            string id)
        {
            // get the request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            UpdateTextRequest data;
            try
            {
                data = JsonConvert.DeserializeObject<UpdateTextRequest>(requestBody);
            }
            catch (JsonReaderException)
            {
                return new BadRequestObjectResult(new { error = "Body should be provided in JSON format." });
            }

            // validate request
            if (data == null)
            {
                return new BadRequestObjectResult(new { error = "Missing required properties 'userId' and 'text'." });
            }
            if (data.Id != null && id != null && data.Id != id)
            {
                return new BadRequestObjectResult(new { error = "Property 'id' does not match the identifier specified in the URL path." });
            }
            if (string.IsNullOrEmpty(data.Id))
            {
                data.Id = id;
            }
            if (string.IsNullOrEmpty(data.Text))
            {
                return new BadRequestObjectResult(new { error = "Missing required property 'text'." });
            }

            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }

            // update text
            try
            {
                var result = await TextService.UpdateTextNoteAsync(data.Id, userId, data.Text);
                if (result == UpdateTextNoteResult.NotFound)
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
        
        [FunctionName("GetText")]
        public static async Task<IActionResult> GetText(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "text/{id}")]HttpRequest req,
            TraceWriter log,
            string id)
        {
            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }

            // get the text note
            try
            {
                var document = await TextService.GetTextNoteAsync(id, userId);
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

        [FunctionName("ListText")]
        public static async Task<IActionResult> ListText(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "text")]HttpRequest req,
            TraceWriter log)
        {
            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }

            // lilst the text notes
            try
            {
                var summaries = await TextService.ListTextNotesAsync(userId);
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
                settings.Converters.Add(new TextNoteSummariesConverter());
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
