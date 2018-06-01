namespace ContentReactor.Shared
{
    public static class StringExtensions
    {
        public static string Truncate(this string value, int maximumLength, string continuationMarker = "...")
        {
            if (string.IsNullOrEmpty(value) || (value.Length <= maximumLength))
            {
                return value;
            }
            
            var truncatedString = value.Substring(0, maximumLength - continuationMarker.Length);
            return truncatedString + continuationMarker;
        }
    }
}
