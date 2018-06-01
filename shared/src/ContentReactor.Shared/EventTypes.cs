namespace ContentReactor.Shared
{
    public static class EventTypes
    {
        public static class Audio
        {
            public const string AudioCreated = nameof(AudioCreated);
            public const string AudioDeleted = nameof(AudioDeleted);
            public const string AudioTranscriptUpdated = nameof(AudioTranscriptUpdated);
        }
        
        public static class Categories
        {
            public const string CategoryCreated = nameof(CategoryCreated);
            public const string CategoryDeleted = nameof(CategoryDeleted);
            public const string CategoryNameUpdated = nameof(CategoryNameUpdated);
            public const string CategoryImageUpdated = nameof(CategoryImageUpdated);
            public const string CategorySynonymsUpdated = nameof(CategorySynonymsUpdated);
            public const string CategoryItemsUpdated = nameof(CategoryItemsUpdated);
        }
        
        public static class Images
        {
            public const string ImageCaptionUpdated = nameof(ImageCaptionUpdated);
            public const string ImageCreated = nameof(ImageCreated);
            public const string ImageDeleted = nameof(ImageDeleted);
        }
        
        public static class Text
        {
            public const string TextCreated = nameof(TextCreated);
            public const string TextDeleted = nameof(TextDeleted);
            public const string TextUpdated = nameof(TextUpdated);
        }
    }
}
