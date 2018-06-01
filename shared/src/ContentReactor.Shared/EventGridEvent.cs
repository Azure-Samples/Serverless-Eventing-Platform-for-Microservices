using System;

namespace ContentReactor.Shared
{
    public class EventGridEvent : EventGridEvent<object>
    {
    }

    public class EventGridEvent<T>
    {
        public string Topic { get; set; }
        public string Id { get; set; }
        public string EventType { get; set;}
        public string Subject { get; set; }
        public DateTime EventTime { get; set; }

        public T Data { get; set; }
    }
}
