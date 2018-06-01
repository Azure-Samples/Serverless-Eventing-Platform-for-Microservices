using System.Collections.Generic;

namespace ContentReactor.Shared.EventSchemas.Categories
{
    public class CategorySynonymsUpdatedEventData
    {
        public string Name { get; set; }
        public IEnumerable<string> Synonyms { get; set; }
    }
}