using System.Collections.Generic;

namespace SignalRMiddleware.Models
{
    
        public class AudioEventData : EventData
        {
            public AudioData Data { get; set; }
        }

        public class AudioEvent
        {
            public IList<AudioEventData> EventList { get; set; }
        }

        public class AudioData
        {
            public string ValidationCode { get; set; }
            public string TranscriptPreview { get; set; }
        }
    
}
