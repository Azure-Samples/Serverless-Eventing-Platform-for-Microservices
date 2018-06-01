using System;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using ContentReactor.Audio.Services;
using ContentReactor.Shared;
using ContentReactor.Shared.BlobRepository;

namespace ContentReactor.Audio.WorkerApi
{
    public static class WorkerApiFunctions
    {
        private static readonly IEventGridSubscriberService EventGridSubscriberService = new EventGridSubscriberService();
        private static readonly IAudioService AudioService = new AudioService(new BlobRepository(), new AudioTranscriptionService(), new EventGridPublisherService());

        [FunctionName("UpdateAudioTranscript")]
        public static async Task<IActionResult> UpdateAudioTranscript(
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
                var (_, userId, audioId) = EventGridSubscriberService.DeconstructEventGridMessage(req);

                // update the audio transcript
                var transcriptPreview = await AudioService.UpdateAudioTranscriptAsync(audioId, userId);
                if (transcriptPreview == null)
                {
                    return new NotFoundResult();
                }

                return new OkResult();
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }
    }
}
