using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using ContentReactor.Images.Services;
using ContentReactor.Images.Services.Models.Results;
using ContentReactor.Shared;
using ContentReactor.Shared.BlobRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;

namespace ContentReactor.Images.WorkerApi
{
    public static class WorkerApiFunctions
    {
        private static readonly IEventGridSubscriberService EventGridSubscriberService = new EventGridSubscriberService();
        private static readonly IImagesService ImagesService = new ImagesService(new BlobRepository(), new ImageValidatorService(),  new ImagePreviewService(), new ImageCaptionService(new HttpClient()), new EventGridPublisherService());
        
        [FunctionName("UpdateImageCaption")]
        public static async Task<IActionResult> UpdateImageCaption(
            [HttpTrigger(AuthorizationLevel.Function, "post")]HttpRequest req,
            TraceWriter log)
        {
            // authenticate to Event Grid if this is a validation event
            var eventGridValidationOutput = EventGridSubscriberService.HandleSubscriptionValidationEvent(req);
            if (eventGridValidationOutput != null)
            {
                log.Info("Responding to Event Grid subscription verification.");
                return eventGridValidationOutput;
            }
            
            try
            {
                var (_, userId, imageId) = EventGridSubscriberService.DeconstructEventGridMessage(req);

                // process the image caption
                var result = await ImagesService.UpdateImageNoteCaptionAsync(imageId, userId);
                switch (result)
                {
                    case UpdateImageNoteCaptionResult.Success:
                        return new OkResult();
                    case UpdateImageNoteCaptionResult.NotFound:
                        return new NotFoundResult();
                    default:
                        throw new InvalidOperationException($"{nameof(ImagesService.UpdateImageNoteCaptionAsync)} returned unexpected result {result}.");
                }
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }
    }
}
