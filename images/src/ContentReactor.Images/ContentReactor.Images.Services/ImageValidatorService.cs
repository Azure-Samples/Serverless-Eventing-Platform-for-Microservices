using System.IO;
using SixLabors.ImageSharp;

namespace ContentReactor.Images.Services
{
    public interface IImageValidatorService
    {
        (bool isValid, string mimeType) ValidateImage(Stream imageStream);
    }

    public class ImageValidatorService : IImageValidatorService
    {
        public (bool isValid, string mimeType) ValidateImage(Stream imageStream)
        {
            if (imageStream == null || imageStream.Length == 0)
            {
                return (false, null);
            }

            var imageFormat = Image.DetectFormat(imageStream);
            if (imageFormat == null)
            {
                return (false, null);
            }

            return (true, imageFormat.DefaultMimeType);
        }
    }
}
