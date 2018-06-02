using Microsoft.AspNetCore.SignalR;
using SignalRMiddleware.Models;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SignalRMiddlewareTests.TestProxies
{
    public class CategoryImageUpdatedProxy : IClientProxy
    {
        public Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            Assert.Equal(2, args.Length);
            Assert.Equal("onCategoryImageUpdated", method);
            Assert.Equal("catId-1234", args[0]);
            CategoryData data = (CategoryData)args[1];
            Assert.Equal("https://microsoft.dummy.jpeg", data.ImageUrl);
            return Task.CompletedTask;
        }
    }

    public class CategorySynonymsUpdatedProxy : IClientProxy
    {
        public Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            Assert.Equal(2, args.Length);
            Assert.Equal("onCategorySynonymsUpdated", method);
            Assert.Equal("catId-1234", args[0]);
            CategoryData data = (CategoryData)args[1];
            Assert.Equal(2, args.Length);
            Assert.Contains("syn1", data.Synonyms);
            Assert.Contains("syn2", data.Synonyms);
            return Task.CompletedTask;
        }
    }

    public class ImageCaptionUpdatedProxy: IClientProxy
    {
        public Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            Assert.Equal(2, args.Length);
            Assert.Equal("onImageCaptionUpdated", method);
            Assert.Equal("imageId-1234", args[0]);
            ImageData data = (ImageData)args[1];
            Assert.Equal("a closeup of a logo", data.Caption);
            return Task.CompletedTask;
        }
    }

    public class AudioTranscriptUpdatedProxy : IClientProxy
    {
        public Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            Assert.Equal(2, args.Length);
            Assert.Equal("onAudioTranscriptUpdated", method);
            Assert.Equal("audioId-1234", args[0]);
            AudioData data = (AudioData)args[1];
            Assert.Equal("The sun rose in the east", data.TranscriptPreview);
            return Task.CompletedTask;
        }
    }

    public class TextUpdatedProxy : IClientProxy
    {
        public Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            Assert.Equal(2, args.Length);
            Assert.Equal("onTextUpdated", method);
            Assert.Equal("textId-1234", args[0]);
            TextEventData.TextData data = (TextEventData.TextData)args[1];
            Assert.Equal("The sun rose in the east", data.Text);
            return Task.CompletedTask;
        }
    }

    /** Develop further proxies here ... */
}
