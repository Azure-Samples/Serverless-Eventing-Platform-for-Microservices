using System;
using ContentReactor.Images.Services.Models.Responses;
using Newtonsoft.Json;

namespace ContentReactor.Images.Services.Converters
{
    public class ImageNoteSummariesConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(ImageNoteSummaries));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            foreach (var summary in (ImageNoteSummaries)value)
            {
                writer.WritePropertyName(summary.Id);
                summary.Id = null;
                serializer.Serialize(writer, summary);
            }
            writer.WriteEndObject();
        }
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
