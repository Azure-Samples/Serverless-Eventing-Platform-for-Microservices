using System;
using ContentReactor.Audio.Services.Models.Responses;
using Newtonsoft.Json;

namespace ContentReactor.Audio.Services.Converters
{
    public class AudioNoteSummariesConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(AudioNoteSummaries));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            foreach (var summary in (AudioNoteSummaries)value)
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
