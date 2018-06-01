using System.Collections.Generic;

namespace SignalRMiddleware.Models
{
    public class ImageEventData : EventData
    {
        public ImageData Data { get; set; }
    }

    public class ImageEvent
    {
        public IList<ImageEventData> EventList { get; set; }
    }

    public class ImageData
    {
        public string ValidationCode { get; set; }
        public string Caption { get; set; }
    }
}
