using System;
using System.Threading.Tasks;
using System.Web.Http;
using ContentReactor.Images.Services;
using ContentReactor.Images.Services.Models.Results;
using ContentReactor.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using ContentReactor.Shared.BlobRepository;
using System.IO;
using System.Net.Http;
using ContentReactor.Images.Services.Converters;
using ContentReactor.Images.Services.Models.Requests;
using ContentReactor.Shared.UserAuthentication;
using Newtonsoft.Json;

namespace ContentReactor.Images.Api
{
    public static class ApiFunctions
    {
        private const string JsonContentType = "application/json";
        public static IImagesService ImagesService = new ImagesService(new BlobRepository(), new ImageValidatorService(),  new ImagePreviewService(), new ImageCaptionService(new HttpClient()), new EventGridPublisherService());
        public static IUserAuthenticationService UserAuthenticationService = new QueryStringUserAuthenticationService();

        [FunctionName("BeginCreateImage")]
        public static async Task<IActionResult> BeginCreateImage
            ([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "images")]HttpRequest req,
            TraceWriter log)
        {
            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }

            try
            {
                var (id, url) = ImagesService.BeginAddImageNote(userId);

                return new OkObjectResult(new
                {
                    id = id,
                    url = url
                });
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }

        [FunctionName("CompleteCreateImage")]
        public static async Task<IActionResult> CompleteCreateImage
            ([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "images/{id}")]HttpRequest req,
            TraceWriter log,
            string id)
        {
            // get request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            CompleteCreateImageRequest data;
            try
            {
                data = JsonConvert.DeserializeObject<CompleteCreateImageRequest>(requestBody);
            }
            catch (JsonReaderException)
            {
                return new BadRequestObjectResult(new { error = "Body should be provided in JSON format." });
            }

            // validate request body
            if (data == null || string.IsNullOrEmpty(data.CategoryId))
            {
                return new BadRequestObjectResult(new { error = "Missing required property 'categoryId'." });
            }

            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }

            try
            {
                var (result, previewUrl) = await ImagesService.CompleteAddImageNoteAsync(id, userId, data.CategoryId);
                switch (result)
                {
                    case CompleteAddImageNoteResult.Success:
                        return new OkObjectResult(new
                        {
                            previewUrl = previewUrl
                        });
                    case CompleteAddImageNoteResult.ImageTooLarge:
                        return new BadRequestObjectResult(new { error = "Image is too large. Images must be 4MB or less." });
                    case CompleteAddImageNoteResult.InvalidImage:
                        return new BadRequestObjectResult(new { error = "Image is not in a valid format. Supported formats are JPEG (image/jpeg) and PNG (image/png)." });
                    case CompleteAddImageNoteResult.ImageNotUploaded:
                        return new BadRequestObjectResult(new { error = "Image has not yet been uploaded." });
                    case CompleteAddImageNoteResult.ImageAlreadyCreated:
                        return new BadRequestObjectResult(new { error = "Image has already been created." });
                    default:
                        throw new InvalidOperationException($"Unexpected result '{result}' from {nameof(ImagesService)}.{nameof(ImagesService.CompleteAddImageNoteAsync)}");
                }
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }

        [FunctionName("GetImage")]
        public static async Task<IActionResult> GetImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "images/{id}")]HttpRequest req,
            TraceWriter log,
            string id)
        {
            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }

            // get the image
            try
            {
                var imageNote = await ImagesService.GetImageNoteAsync(id, userId);
                if (imageNote == null)
                {
                    return new NotFoundResult();
                }

                return new OkObjectResult(imageNote);
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }

        [FunctionName("ListImages")]
        public static async Task<IActionResult> ListImages(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "images")]HttpRequest req,
            TraceWriter log)
        {
            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }

            // list the image notes
            try
            {
                var summaries = await ImagesService.ListImageNotesAsync(userId);
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
                settings.Converters.Add(new ImageNoteSummariesConverter());
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

        [FunctionName("DeleteImage")]
        public static async Task<IActionResult> DeleteImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "images/{id}")]HttpRequest req,
            TraceWriter log,
            string id)
        {
            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }

            // delete the image
            try
            {
                await ImagesService.DeleteImageNoteAsync(id, userId);
                return new NoContentResult();
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }
    }
}
