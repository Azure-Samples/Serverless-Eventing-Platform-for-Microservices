using System.IO;
using System.Reflection;
using SixLabors.ImageSharp;
using SixLabors.Primitives;
using Xunit;

namespace ContentReactor.Images.Services.Tests
{
    public class ImagePreviewServiceTests
    {
        #region CreatePreviewImage Tests
        [Fact]
        public void CreatePreviewImage_Jpeg()
        {
            // arrange
            var stream = GetImageResourceStream("jpeg.jpeg");
            var service = new ImagePreviewService();

            // act
            var result = service.CreatePreviewImage(stream);
            var image = Image.Load(result, out var imageFormat);

            // assert
            Assert.Equal(new Size(150, 150), image.Size());
            Assert.Equal(ImageFormats.Jpeg, imageFormat);
        }

        [Fact]
        public void CreatePreviewImage_Png()
        {
            // arrange
            var stream = GetImageResourceStream("png.png");
            var service = new ImagePreviewService();

            // act
            var result = service.CreatePreviewImage(stream);
            var image = Image.Load(result, out var imageFormat);

            // assert
            Assert.Equal(new Size(150, 150), image.Size());
            Assert.Equal(ImageFormats.Jpeg, imageFormat);
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
