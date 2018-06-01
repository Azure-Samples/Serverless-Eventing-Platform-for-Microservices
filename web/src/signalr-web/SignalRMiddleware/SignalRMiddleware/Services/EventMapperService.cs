using System.Collections.Generic;

namespace SignalRMiddleware.Services
{
    public static class EventMapperService
    {
        /** Mapping between events from microservices to the event handler in the front end */
        private static Dictionary<string, string> _eventMap = new Dictionary<string, string>()
        {
            // Category event mapping
            { "CategoryCreated","onCategoryCreated" },
            { "CategoryDeleted","onCategoryDeleted" },
            { "CategoryImageUpdated","onCategoryImageUpdated"},
            { "CategorySynonymsUpdated","onCategorySynonymsUpdated"},
            { "CategoryNameUpdated","onCategoryNameUpdated"},
            { "CategoryItemsUpdated","onCategoryItemsUpdated"},

            // Image event mapping
            { "ImageCaptionUpdated","onImageCaptionUpdated" },
            { "ImageCreated","onImageCreated" },
            { "ImageDeleted","onImageDeleted" },

            // Audio event mapping
            { "AudioCreated","onAudioCreated" },
            { "AudioDeleted","onAudioDeleted" },
            { "AudioTranscriptUpdated", "onAudioTranscriptUpdated" },

            // Text event mapping
            { "TextCreated", "onTextCreated" },
            { "TextDeleted", "onTextDeleted" },
            { "TextUpdated", "onTextUpdated" }
        };  

        public static string getMappedEvent(string egEvent)
        {
            return _eventMap[egEvent];
        }
    }
}
