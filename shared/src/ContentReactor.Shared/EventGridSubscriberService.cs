using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ContentReactor.Shared.EventSchemas.Audio;
using ContentReactor.Shared.EventSchemas.Categories;
using ContentReactor.Shared.EventSchemas.Images;
using ContentReactor.Shared.EventSchemas.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ContentReactor.Shared
{
    public interface IEventGridSubscriberService
    {
        IActionResult HandleSubscriptionValidationEvent(HttpRequest req);
        (EventGridEvent eventGridEvent, string userId, string itemId) DeconstructEventGridMessage(HttpRequest req);
    }

    public class EventGridSubscriberService : IEventGridSubscriberService
    {
        internal const string EventGridSubscriptionValidationHeaderKey = "Aeg-Event-Type";

        public IActionResult HandleSubscriptionValidationEvent(HttpRequest req)
        {
            if (req.Body.CanSeek)
            {
                req.Body.Position = 0;
            }

            var requestBody = new StreamReader(req.Body).ReadToEnd();
            if (string.IsNullOrEmpty(requestBody))
            {
                return null;
            }

            dynamic data = JsonConvert.DeserializeObject(requestBody);
            
            foreach (var dataEvent in data)
            {
                if (req.Headers.TryGetValue(EventGridSubscriptionValidationHeaderKey, out StringValues values) && values.Equals("SubscriptionValidation") &&
                    dataEvent.eventType == "Microsoft.EventGrid.SubscriptionValidationEvent")
                {
                    // this is a special event type that needs an echo response for Event Grid to work
                    var validationCode = dataEvent.data.validationCode; // TODO .ToString();
                    var echoResponse = new {validationResponse = validationCode};
                    return new OkObjectResult(echoResponse);
                }
            }

            return null;
        }

        public (EventGridEvent eventGridEvent, string userId, string itemId) DeconstructEventGridMessage(HttpRequest req)
        {
            // read the request stream
            if (req.Body.CanSeek)
            {
                req.Body.Position = 0;
            }
            var requestBody = new StreamReader(req.Body).ReadToEnd();

            // deserialise into a single Event Grid event - we won't allow multiple events to be processed
            var eventGridEvents = JsonConvert.DeserializeObject<EventGridEvent[]>(requestBody);
            if (eventGridEvents.Length == 0)
            {
                return (null, null, null);
            }
            if (eventGridEvents.Length > 1)
            {
                throw new InvalidOperationException("Expected only a single Event Grid event.");
            }
            var eventGridEvent = eventGridEvents.Single();

            // convert the 'data' property to a strongly typed object rather than a JObject
            eventGridEvent.Data = CreateStronglyTypedDataObject(eventGridEvent.Data, eventGridEvent.EventType);

            // find the user ID and item ID from the subject
            var eventGridEventSubjectComponents = eventGridEvent.Subject.Split('/');
            if (eventGridEventSubjectComponents.Length != 2)
            {
                throw new InvalidOperationException("Event Grid event subject is not in expected format.");
            }
            var userId = eventGridEventSubjectComponents[0];
            var itemId = eventGridEventSubjectComponents[1];
            
            return (eventGridEvent, userId, itemId);
        }

        private object CreateStronglyTypedDataObject(object data, string eventType)
        {
            switch (eventType)
            {
                // creates

                case EventTypes.Audio.AudioCreated:
                    return ConvertDataObjectToType<AudioCreatedEventData>(data);

                case EventTypes.Categories.CategoryCreated:
                    return ConvertDataObjectToType<CategoryCreatedEventData>(data);
                    
                case EventTypes.Images.ImageCreated:
                    return ConvertDataObjectToType<ImageCreatedEventData>(data);

                case EventTypes.Text.TextCreated:
                    return ConvertDataObjectToType<TextCreatedEventData>(data);

                // updates

                case EventTypes.Audio.AudioTranscriptUpdated:
                    return ConvertDataObjectToType<AudioTranscriptUpdatedEventData>(data);

                case EventTypes.Categories.CategoryImageUpdated:
                    return ConvertDataObjectToType<CategoryImageUpdatedEventData>(data);

                case EventTypes.Categories.CategoryItemsUpdated:
                    return ConvertDataObjectToType<CategoryItemsUpdatedEventData>(data);

                case EventTypes.Categories.CategoryNameUpdated:
                    return ConvertDataObjectToType<CategoryNameUpdatedEventData>(data);

                case EventTypes.Categories.CategorySynonymsUpdated:
                    return ConvertDataObjectToType<CategorySynonymsUpdatedEventData>(data);

                case EventTypes.Images.ImageCaptionUpdated:
                    return ConvertDataObjectToType<ImageCaptionUpdatedEventData>(data);

                case EventTypes.Text.TextUpdated:
                    return ConvertDataObjectToType<TextUpdatedEventData>(data);

                // deletes

                case EventTypes.Audio.AudioDeleted:
                    return ConvertDataObjectToType<AudioDeletedEventData>(data);

                case EventTypes.Categories.CategoryDeleted:
                    return ConvertDataObjectToType<CategoryDeletedEventData>(data);

                case EventTypes.Images.ImageDeleted:
                    return ConvertDataObjectToType<ImageDeletedEventData>(data);

                case EventTypes.Text.TextDeleted:
                    return ConvertDataObjectToType<TextDeletedEventData>(data);

                default:
                    throw new ArgumentException($"Unexpected event type '{eventType}' in {nameof(CreateStronglyTypedDataObject)}");
            }
        }

        private T ConvertDataObjectToType<T>(object dataObject)
        {
            if (dataObject is JObject o)
            {
                return o.ToObject<T>();
            }

            return (T) dataObject;
        }
    }
}
