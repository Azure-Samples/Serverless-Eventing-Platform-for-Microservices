using SignalRMiddleware.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SignalRMiddlewareTests
{
    class NotificationTestsSetup
    {

        /** Base Setup Data for Category/Image/Text/Audio Events */
        private static CategoryEventData SetupCategoryBaseData()
        {
            CategoryEventData catEventData = new CategoryEventData();
            catEventData.Id = "eventId-1234";
            catEventData.Subject = "foo/catId-1234";
            catEventData.EventType = "CategoryImageUpdated";
            return catEventData;
        }
        private static CategoryEventData SetupCategoryBaseImageData()
        {
            CategoryEventData catEventData = new CategoryEventData();
            catEventData.Id = "eventId-1234";
            catEventData.Subject = "foo/catId-1234";
            catEventData.EventType = "CategoryImageUpdated";
            return catEventData;
        }

        private static CategoryEventData SetupCategoryBaseSynonymData()
        {
            CategoryEventData catEventData = new CategoryEventData();
            catEventData.Id = "eventId-1234";
            catEventData.Subject = "foo/catId-1234";
            catEventData.EventType = "CategorySynonymsUpdated";
            return catEventData;
        }

        private static ImageEventData SetupImageBaseData()
        {
            ImageEventData imgEventData = new ImageEventData();
            imgEventData.Id = "eventId-1234";
            imgEventData.Subject = "foo/imageId-1234";
            imgEventData.EventType = "ImageCaptionUpdated";
            return imgEventData;
        }

        private static AudioEventData SetupAudioBaseData()
        {
            AudioEventData audioEventData = new AudioEventData();
            audioEventData.Id = "eventId-1234";
            audioEventData.Subject = "foo/audioId-1234";
            audioEventData.EventType = "AudioTranscriptUpdated";
            return audioEventData;
        }

        private static TextEventData SetupTextBaseData()
        {
            TextEventData textEventData = new TextEventData();
            textEventData.Id = "eventId-1234";
            textEventData.Subject = "foo/textId-1234";
            textEventData.EventType = "TextUpdated";
            return textEventData;
        }

        public static IList<CategoryEventData> SetupValidationCodeData()
        {
            List<CategoryEventData> categoryEvents = new List<CategoryEventData>();
            CategoryEventData categoryEventData = SetupCategoryBaseData();
            categoryEventData.Data = new CategoryData();
            categoryEventData.Data.ValidationCode = "testValidationCode";
            categoryEvents.Add(categoryEventData);

            return categoryEvents;
        }

        public static IList<CategoryEventData> SetupCategoryImageData()
        {
            List<CategoryEventData> categoryEvents = new List<CategoryEventData>();
            CategoryEventData categoryEventData = SetupCategoryBaseImageData();
            categoryEventData.Data = new CategoryData();
            categoryEventData.Data.ImageUrl = "https://microsoft.dummy.jpeg";
            categoryEvents.Add(categoryEventData);

            return categoryEvents;
        }

        public static IList<CategoryEventData> SetupCategorySynonymsData()
        {
            List<CategoryEventData> categoryEvents = new List<CategoryEventData>();
            CategoryEventData categoryEventData = SetupCategoryBaseSynonymData();
            categoryEventData.Data = new CategoryData();
            categoryEventData.Data.Synonyms = new List<string>(new string[] { "syn1", "syn2" });
            categoryEvents.Add(categoryEventData);

            return categoryEvents;
        }

        public static IList<ImageEventData> SetupImageCaptionData()
        {
            List<ImageEventData> imageEvents = new List<ImageEventData>();
            ImageEventData imageEventData = SetupImageBaseData();
            imageEventData.Data = new ImageData();
            imageEventData.Data.Caption = "a closeup of a logo";
            imageEvents.Add(imageEventData);

            return imageEvents;
        }

        public static IList<AudioEventData> SetupAudioTranscriptData()
        {
            List<AudioEventData> audioEvents = new List<AudioEventData>();
            AudioEventData audioEventData = SetupAudioBaseData();
            audioEventData.Data = new AudioData();
            audioEventData.Data.TranscriptPreview = "The sun rose in the east";
            audioEvents.Add(audioEventData);

            return audioEvents;
        }

        public static IList<TextEventData> SetupTextData()
        {
            List<TextEventData> textEvents = new List<TextEventData>();
            TextEventData textEvent = SetupTextBaseData();
            textEvent.Data = new TextEventData.TextData();
            textEvent.Data.Text = "The sun rose in the east";
            textEvents.Add(textEvent);

            return textEvents;
        }
    }

    /** Develop further test setup data here... */
}
