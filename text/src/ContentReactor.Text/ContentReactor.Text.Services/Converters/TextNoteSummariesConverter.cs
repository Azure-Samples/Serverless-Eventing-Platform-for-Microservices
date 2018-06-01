using System;
using ContentReactor.Text.Services.Models.Responses;
using Newtonsoft.Json;

namespace ContentReactor.Text.Services.Converters
{
    public class TextNoteSummariesConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(TextNoteSummaries));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            foreach (var summary in (TextNoteSummaries)value)
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
