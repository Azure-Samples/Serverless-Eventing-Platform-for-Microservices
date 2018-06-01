using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;
using ContentReactor.Audio.Services;
using ContentReactor.Audio.Services.Converters;
using ContentReactor.Audio.Services.Models.Requests;
using ContentReactor.Audio.Services.Models.Results;
using ContentReactor.Shared;
using ContentReactor.Shared.BlobRepository;
using ContentReactor.Shared.UserAuthentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace ContentReactor.Audio.Api
{
    public static class ApiFunctions
    {
        private const string JsonContentType = "application/json";
        public static IAudioService AudioService = new AudioService(new BlobRepository(), new AudioTranscriptionService(), new EventGridPublisherService());
        public static IUserAuthenticationService UserAuthenticationService = new QueryStringUserAuthenticationService();

        [FunctionName("BeginCreateAudio")]
        public static async Task<IActionResult> BeginCreateAudio
            ([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "audio")]HttpRequest req,
            TraceWriter log)
        {
            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }

            // create the audio note
            try
            {
                var (id, url) = AudioService.BeginAddAudioNote(userId);
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

        [FunctionName("CompleteCreateAudio")]
        public static async Task<IActionResult> CompleteCreateAudio(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "audio/{id}")]HttpRequest req,
            TraceWriter log,
            string id)
        {
            // get request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            CompleteCreateAudioRequest data;
            try
            {
                data = JsonConvert.DeserializeObject<CompleteCreateAudioRequest>(requestBody);
            }
            catch (JsonReaderException)
            {
                return new BadRequestObjectResult(new { error = "Body should be provided in JSON format." });
            }

            // validate the request body
            if (data == null || string.IsNullOrEmpty(data.CategoryId))
            {
                return new BadRequestObjectResult(new { error = "Missing required property 'categoryId'." });
            }

            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }

            // finish creating the audio note
            try
            {
                var result = await AudioService.CompleteAddAudioNoteAsync(id, userId, data.CategoryId);

                switch (result)
                {
                    case CompleteAddAudioNoteResult.Success:
                        return new NoContentResult();
                    case CompleteAddAudioNoteResult.AudioNotUploaded:
                        return new BadRequestObjectResult(new { error = "Audio has not yet been uploaded." });
                    case CompleteAddAudioNoteResult.AudioAlreadyCreated:
                        return new BadRequestObjectResult(new { error = "Image has already been created." });
                    default:
                        throw new InvalidOperationException($"Unexpected result '{result}' from {nameof(AudioService)}.{nameof(AudioService.CompleteAddAudioNoteAsync)}");
                }
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }

        [FunctionName("GetAudio")]
        public static async Task<IActionResult> GetAudio(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "audio/{id}")]HttpRequest req,
            TraceWriter log,
            string id)
        {
            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }
            
            // get the audio note
            try
            {
                var audioNoteDetails = await AudioService.GetAudioNoteAsync(id, userId);
                if (audioNoteDetails == null)
                {
                    return new NotFoundResult();
                }

                return new OkObjectResult(audioNoteDetails);
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }

        [FunctionName("ListAudio")]
        public static async Task<IActionResult> ListAudio(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "audio")]HttpRequest req,
            TraceWriter log)
        {
            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }

            // lilst the audio notes
            try
            {
                var summaries = await AudioService.ListAudioNotesAsync(userId);
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
                settings.Converters.Add(new AudioNoteSummariesConverter());
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

        [FunctionName("DeleteAudio")]
        public static async Task<IActionResult> DeleteAudio(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "audio/{id}")]HttpRequest req,
            TraceWriter log,
            string id)
        {
            // get the user ID
            if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            {
                return responseResult;
            }
            
            // delete the audio note
            try
            {
                await AudioService.DeleteAudioNoteAsync(id, userId);
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
