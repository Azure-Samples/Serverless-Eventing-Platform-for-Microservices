using System.Collections.Generic;

namespace SignalRMiddleware.Models
{
    public class TextEventData : EventData
    {
        public TextData Data { get; set; }

        public class TextEvent
        {
            public IList<TextEventData> EventList { get; set; }
        }

        public class TextData
        {
            public string ValidationCode { get; set; }
            public string Text { get; set; }
        }
    }
}
