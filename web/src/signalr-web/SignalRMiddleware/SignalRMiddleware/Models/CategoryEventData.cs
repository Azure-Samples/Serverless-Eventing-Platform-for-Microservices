using System.Collections.Generic;

namespace SignalRMiddleware.Models
{
    public class CategoryEventData : EventData
    {
        public CategoryData Data { get; set; }
    }
    public class CategoryEvent
    {
        public IList<CategoryEventData> EventList { get; set; }
    }

    public class CategoryData
    {
        public string ValidationCode { get; set; }
        public IList<string> Synonyms { get; set; }
        public string ImageUrl { get; set; }
         
    } 
}
