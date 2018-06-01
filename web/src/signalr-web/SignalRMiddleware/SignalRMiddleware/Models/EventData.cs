namespace SignalRMiddleware.Models
{
    public class EventData
    {
        public string Id { get; set; }

        public string Topic { get; set; }

        public string Subject { get; set; }

        public string EventType { get; set; }

        public string EventTime { get; set; }

        public string MetadataVersion { get; set; }

        public string DataVersion { get; set; }
    }

    public class EventValidationResponse
    {
        public string ValidationResponse { get; set; }
    }
}
