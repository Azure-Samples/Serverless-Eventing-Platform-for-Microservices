using System.IO;
using System.Reflection;
using Xunit;

namespace ContentReactor.Images.Services.Tests
{
    public class ImageValidatorServiceTests
    {
        #region ValidateImage Tests
        [Fact]
        public void ValidateImage_ReturnsTrue()
        {
            // arrange
            var stream = GetImageResourceStream("jpeg.jpeg");
            var service = new ImageValidatorService();

            // act
            var result = service.ValidateImage(stream);

            // assert
            Assert.True(result.isValid);
        }

        [Fact]
        public void ValidateImage_ReturnsMimeType()
        {
            // arrange
            var stream = GetImageResourceStream("jpeg.jpeg");
            var service = new ImageValidatorService();

            // act
            var result = service.ValidateImage(stream);

            // assert
            Assert.Equal("image/jpeg", result.mimeType);
        }

        [Fact]
        public void ValidateImage_ReturnsFalseForNullStream()
        {
            // arrange
            var service = new ImageValidatorService();

            // act
            var result = service.ValidateImage(null);

            // assert
            Assert.False(result.isValid);
        }

        [Fact]
        public void ValidateImage_ReturnsFalseForEmptyStream()
        {
            // arrange
            var stream = new MemoryStream();
            var service = new ImageValidatorService();

            // act
            var result = service.ValidateImage(stream);

            // assert
            Assert.False(result.isValid);
        }

        [Fact]
        public void ValidateImage_ReturnsFalseForInvalidFileFormat()
        {
            // arrange
            var stream = GetImageResourceStream("InvalidImage.txt");
            var service = new ImageValidatorService();

            // act
            var result = service.ValidateImage(stream);

            // assert
            Assert.False(result.isValid);
        }
        #endregion

        #region Helpers
        private Stream GetImageResourceStream(string filename)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"ContentReactor.Images.Services.Tests.SampleImages.{filename}";
            return assembly.GetManifestResourceStream(resourceName);
        }
        #endregion
    }
}
