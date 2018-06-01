using Xunit;

namespace ContentReactor.Shared.Tests
{
    public class StringExtensionsTests
    {
        [Fact]
        public void Truncate_ReturnsShortString()
        {
            // arrange
            var originalString = "short string should be returned as-is";

            // act
            var result = originalString.Truncate(100);

            // assert
            Assert.Equal(originalString, result);
        }

        [Fact]
        public void Truncate_ReturnsShortStringWhenEqualToMaximumLength()
        {
            // arrange
            var originalString = "a longer string should be truncated";

            // act
            var result = originalString.Truncate(originalString.Length);

            // assert
            Assert.Equal(originalString, result);
        }

        [Fact]
        public void Truncate_ReturnsShortenedVersionOfLongString()
        {
            // arrange
            var originalString = "a longer string should be truncated";

            // act
            var result = originalString.Truncate(10);

            // assert
            Assert.Equal("a longe...", result);
        }
    }
}
