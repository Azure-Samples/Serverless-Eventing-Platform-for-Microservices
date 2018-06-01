using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ContentReactor.Audio.Services.Models.Results;
using ContentReactor.Shared;
using ContentReactor.Shared.EventSchemas.Audio;
using ContentReactor.Tests.FakeBlobRepository;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using Xunit;

namespace ContentReactor.Audio.Services.Tests
{
    public class AudioServiceTests
    {
        #region BeginAddAudioNote Tests
        [Fact]
        public void BeginAddAudioNote_ReturnsIdAndUrl()
        {
            // arrange
            var service = new AudioService(new FakeBlobRepository(), new Mock<IAudioTranscriptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var output = service.BeginAddAudioNote("fakeuserid");

            // assert
            Assert.NotEmpty(output.id);
            Assert.Equal($"https://fakerepository/audio/fakeuserid/{output.id}?sasToken=Write, Create", output.url);
        }
        #endregion

        #region CompleteAddAudioNote Tests
        [Fact]
        public async Task CompleteAddAudioNote_ReturnsSuccess()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid/fakeid");
            var service = new AudioService(fakeBlobRepository, new Mock<IAudioTranscriptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.CompleteAddAudioNoteAsync("fakeid", "fakeuserid", "fakecategory");

            // assert
            Assert.Equal(CompleteAddAudioNoteResult.Success, result);
        }

        [Fact]
        public async Task CompleteAddAudioNote_UpdatesBlobMetadata()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid/fakeid");
            var service = new AudioService(fakeBlobRepository, new Mock<IAudioTranscriptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.CompleteAddAudioNoteAsync("fakeid", "fakeuserid", "fakecategory");

            // assert
            Assert.Equal("fakecategory", fakeBlobRepository.Blobs.Single().Blob.Metadata[AudioService.CategoryIdMetadataName]);
            Assert.Equal("fakeuserid", fakeBlobRepository.Blobs.Single().Blob.Metadata[AudioService.UserIdMetadataName]);
        }

        [Fact]
        public async Task CompleteAddAudioNote_PublishesAudioCreatedEventToEventGrid()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid/fakeid");
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var service = new AudioService(fakeBlobRepository, new Mock<IAudioTranscriptionService>().Object, mockEventGridPublisherService.Object);

            // act
            await service.CompleteAddAudioNoteAsync("fakeid", "fakeuserid", "fakecategory");

            // assert
            mockEventGridPublisherService.Verify(m => 
                m.PostEventGridEventAsync(EventTypes.Audio.AudioCreated, 
                    "fakeuserid/fakeid", 
                    It.Is<AudioCreatedEventData>(d => d.Category == "fakecategory")),
                Times.Once);
        }

        [Fact]
        public async Task CompleteAddAudioNote_ReturnsAudioNotUploaded()
        {
            // arrange
            var service = new AudioService(new FakeBlobRepository(), new Mock<IAudioTranscriptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.CompleteAddAudioNoteAsync("fakeid", "fakeuserid", "fakecategory");

            // assert
            Assert.Equal(CompleteAddAudioNoteResult.AudioNotUploaded, result);
        }

        [Fact]
        public async Task CompleteAddAudioNote_ReturnsAudioAlreadyCreated()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            var blob = new CloudBlockBlob(new Uri("https://fakeblobrepository/audio/fakeuserid/fakeid"));
            blob.Metadata.Add(AudioService.CategoryIdMetadataName, "fakecategory");
            blob.Metadata.Add(AudioService.UserIdMetadataName, "fakeuserid");
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid/fakeid", blob);
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var service = new AudioService(fakeBlobRepository, new Mock<IAudioTranscriptionService>().Object, mockEventGridPublisherService.Object);

            // act
            var result = await service.CompleteAddAudioNoteAsync("fakeid", "fakeuserid", "fakecategory");

            // assert
            Assert.Equal(CompleteAddAudioNoteResult.AudioAlreadyCreated, result);
        }

        [Fact]
        public async Task CompleteAddAudioNote_IncorrectUserId_ReturnsAudioNotUploaded()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid2/fakeid");
            var service = new AudioService(fakeBlobRepository, new Mock<IAudioTranscriptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.CompleteAddAudioNoteAsync("fakeid", "fakeuserid", "fakecategory");

            // assert
            Assert.Equal(CompleteAddAudioNoteResult.AudioNotUploaded, result);
        }
        #endregion
        
        #region GetAudioNote Tests
        [Fact]
        public async Task GetAudioNote_ReturnsAudio()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            var fullAudioBlob = new CloudBlockBlob(new Uri("https://fakeblobrepository/audio/fakeuserid/fakeid"));
            fullAudioBlob.Metadata[AudioService.TranscriptMetadataName] = "faketranscript";
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid/fakeid", fullAudioBlob);
            var service = new AudioService(fakeBlobRepository, new Mock<IAudioTranscriptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.GetAudioNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal("https://fakeblobrepository/audio/fakeuserid/fakeid?sasToken=Read", result.AudioUrl);
            Assert.Equal("faketranscript", result.Transcript);
        }

        [Fact]
        public async Task GetAudioNote_TranscriptMissing()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid/fakeid");
            var service = new AudioService(fakeBlobRepository, new Mock<IAudioTranscriptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.GetAudioNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal("https://fakeblobrepository/audio/fakeuserid/fakeid?sasToken=Read", result.AudioUrl);
            Assert.Null(result.Transcript);
        }

        [Fact]
        public async Task GetAudioNote_InvalidAudioId_ReturnsNull()
        {
            // arrange
            var service = new AudioService(new FakeBlobRepository(), new Mock<IAudioTranscriptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.GetAudioNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAudioNote_IncorrectUserId_ReturnsNull()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            var fullAudioBlob = new CloudBlockBlob(new Uri("https://fakeblobrepository/audio/fakeuserid/audio"));
            fullAudioBlob.Metadata[AudioService.TranscriptMetadataName] = "faketranscript";
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid2/fakeid", fullAudioBlob);
            var service = new AudioService(fakeBlobRepository, new Mock<IAudioTranscriptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.GetAudioNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.Null(result);
        }
        #endregion

        #region ListAudioNotes Tests
        [Fact]
        public async Task ListAudioNotes_ReturnsSummaries()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            var blob1 = new CloudBlockBlob(new Uri("https://fakeblobrepository/audio/fakeuserid1/fakeid1"));
            blob1.Metadata.Add(AudioService.TranscriptMetadataName, "transcript1");
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid/fakeid1", blob1);
            var blob2 = new CloudBlockBlob(new Uri("https://fakeblobrepository/audio/fakeuserid2/fakeid2"));
            blob2.Metadata.Add(AudioService.TranscriptMetadataName, "transcript2");
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid/fakeid2", blob2);
            var service = new AudioService(fakeBlobRepository, new Mock<IAudioTranscriptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.ListAudioNotesAsync("fakeuserid");

            // assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == "fakeid1" && r.Preview == "transcript1");
            Assert.Contains(result, r => r.Id == "fakeid2" && r.Preview == "transcript2");
        }

        [Fact]
        public async Task ListAudioNotes_DoesNotReturnsSummariesForAnotherUser()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            var blob1 = new CloudBlockBlob(new Uri("https://fakeblobrepository/audio/fakeuserid1/fakeid1"));
            blob1.Metadata.Add(AudioService.TranscriptMetadataName, "transcript1");
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid1/fakeid1", blob1);
            var blob2 = new CloudBlockBlob(new Uri("https://fakeblobrepository/audio/fakeuserid2/fakeid2"));
            blob2.Metadata.Add(AudioService.TranscriptMetadataName, "transcript2");
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid2/fakeid2", blob2);
            var service = new AudioService(fakeBlobRepository, new Mock<IAudioTranscriptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.ListAudioNotesAsync("fakeuserid1");

            // assert
            Assert.Single(result);
            Assert.Equal("fakeid1", result.Single().Id);
            Assert.Equal("transcript1", result.Single().Preview);
        }
        #endregion

        #region DeleteAudioNote Tests
        [Fact]
        public async Task DeleteAudioNote_DeletesBlob()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid/fakeid");
            var service = new AudioService(fakeBlobRepository, new Mock<IAudioTranscriptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.DeleteAudioNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.Empty(fakeBlobRepository.Blobs);
        }

        [Fact]
        public async Task DeleteAudioNote_PublishesAudioDeletedEventToEventGrid()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid/fakeid");
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var service = new AudioService(fakeBlobRepository, new Mock<IAudioTranscriptionService>().Object, mockEventGridPublisherService.Object);

            // act
            await service.DeleteAudioNoteAsync("fakeid", "fakeuserid");

            // assert
            mockEventGridPublisherService.Verify(m => 
                m.PostEventGridEventAsync(EventTypes.Audio.AudioDeleted, 
                    "fakeuserid/fakeid",
                    It.IsAny<AudioDeletedEventData>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAudioNote_InvalidAudioId_AudioNotFound()
        {
            // arrange
            var service = new AudioService(new FakeBlobRepository(), new Mock<IAudioTranscriptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.DeleteAudioNoteAsync("invalidaudioid", "fakeuserid");

            // assert
            // no exception thrown
        }

        [Fact]
        public async Task DeleteAudioNote_IncorrectUserId_AudioNotFound()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid/fakeid");
            var service = new AudioService(fakeBlobRepository, new Mock<IAudioTranscriptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.DeleteAudioNoteAsync("fakeid", "fakeuserid");

            // assert
            // no exception thrown
        }
        #endregion

        #region UpdateAudioTranscript Tests
        [Fact]
        public async Task UpdateAudioTranscript_ReturnsTranscript()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid/fakeid");
            var mockAudioTranscriptionService = new Mock<IAudioTranscriptionService>();
            mockAudioTranscriptionService
                .Setup(m => m.GetAudioTranscriptFromCognitiveServicesAsync(It.IsAny<Stream>()))
                .ReturnsAsync("transcript");
            var service = new AudioService(fakeBlobRepository, mockAudioTranscriptionService.Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateAudioTranscriptAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal("transcript", result);
        }

        [Fact]
        public async Task UpdateAudioTranscript_UpdatesTranscriptBlobMetadata()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid/fakeid");
            var mockAudioTranscriptionService = new Mock<IAudioTranscriptionService>();
            mockAudioTranscriptionService
                .Setup(m => m.GetAudioTranscriptFromCognitiveServicesAsync(It.IsAny<Stream>()))
                .ReturnsAsync("transcript");
            var service = new AudioService(fakeBlobRepository, mockAudioTranscriptionService.Object, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.UpdateAudioTranscriptAsync("fakeid", "fakeuserid");

            // assert
            Assert.True(fakeBlobRepository.Blobs.Single().Blob.Metadata.ContainsKey(AudioService.TranscriptMetadataName));
            Assert.Equal("transcript",fakeBlobRepository.Blobs.Single().Blob.Metadata[AudioService.TranscriptMetadataName]);
        }

        [Fact]
        public async Task UpdateAudioTranscript_PublishesAudioTranscriptUpdatedEventToEventGrid()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid/fakeid");
            var mockAudioTranscriptionService = new Mock<IAudioTranscriptionService>();
            mockAudioTranscriptionService
                .Setup(m => m.GetAudioTranscriptFromCognitiveServicesAsync(It.IsAny<Stream>()))
                .ReturnsAsync("transcript");
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var service = new AudioService(fakeBlobRepository, mockAudioTranscriptionService.Object, mockEventGridPublisherService.Object);

            // act
            await service.UpdateAudioTranscriptAsync("fakeid", "fakeuserid");

            // assert
            mockEventGridPublisherService.Verify(m => 
                m.PostEventGridEventAsync(EventTypes.Audio.AudioTranscriptUpdated,
                    "fakeuserid/fakeid", 
                    It.Is<AudioTranscriptUpdatedEventData>(d => d.TranscriptPreview == "transcript")),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAudioTranscript_InvalidAudioId_AudioNotFound()
        {
            // arrange
            var service = new AudioService(new FakeBlobRepository(), new Mock<IAudioTranscriptionService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateAudioTranscriptAsync("invalidaudioid", "fakeuserid");

            // assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAudioTranscript_IncorrectUserId_ReturnsTranscript()
        {
            // arrange
            var fakeBlobRepository = new FakeBlobRepository();
            fakeBlobRepository.AddFakeBlob(AudioService.AudioBlobContainerName, "fakeuserid2/fakeid");
            var mockAudioTranscriptionService = new Mock<IAudioTranscriptionService>();
            mockAudioTranscriptionService
                .Setup(m => m.GetAudioTranscriptFromCognitiveServicesAsync(It.IsAny<Stream>()))
                .ReturnsAsync("transcript");
            var service = new AudioService(fakeBlobRepository, mockAudioTranscriptionService.Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateAudioTranscriptAsync("fakeid", "fakeuserid");

            // assert
            Assert.Null(result);
        }
        #endregion
    }
}
