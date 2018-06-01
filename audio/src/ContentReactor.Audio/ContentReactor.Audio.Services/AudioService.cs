using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ContentReactor.Audio.Services.Models.Responses;
using ContentReactor.Audio.Services.Models.Results;
using ContentReactor.Shared;
using ContentReactor.Shared.BlobRepository;
using ContentReactor.Shared.EventSchemas.Audio;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ContentReactor.Audio.Services
{
    public interface IAudioService
    {
        (string id, string url) BeginAddAudioNote(string userId);
        Task<CompleteAddAudioNoteResult> CompleteAddAudioNoteAsync(string audioId, string userId, string categoryId);
        Task<AudioNoteDetails> GetAudioNoteAsync(string id, string userId);
        Task<AudioNoteSummaries> ListAudioNotesAsync(string userId);
        Task DeleteAudioNoteAsync(string id, string userId);
        Task<string> UpdateAudioTranscriptAsync(string id, string userId);
    }

    public class AudioService : IAudioService
    {
        protected IBlobRepository BlobRepository;
        protected IAudioTranscriptionService AudioTranscriptionService;
        protected IEventGridPublisherService EventGridPublisherService;

        protected internal const string AudioBlobContainerName = "audio";
        protected internal const string TranscriptMetadataName = "transcript";
        protected internal const string CategoryIdMetadataName = "categoryId";
        protected internal const string UserIdMetadataName = "userId";
        protected internal const int TranscriptPreviewLength = 100;
        
        public AudioService(IBlobRepository blobRepository, IAudioTranscriptionService audioTranscriptionService, IEventGridPublisherService eventGridPublisherService)
        {
            BlobRepository = blobRepository;
            AudioTranscriptionService = audioTranscriptionService;
            EventGridPublisherService = eventGridPublisherService;
        }

        public (string id, string url) BeginAddAudioNote(string userId)
        {
            // generate an ID for this image note
            var audioId = Guid.NewGuid().ToString();

            // create a blob placeholder (which will not have any contents yet)
            var blob = BlobRepository.CreatePlaceholderBlob(AudioBlobContainerName, userId, audioId);

            // get a SAS token to allow the client to write the blob
            var writePolicy = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5), // to allow for clock skew
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.Create | SharedAccessBlobPermissions.Write
            };
            var url = BlobRepository.GetSasTokenForBlob(blob, writePolicy);

            return (audioId, url);
        }

        public async Task<CompleteAddAudioNoteResult> CompleteAddAudioNoteAsync(string audioId, string userId, string categoryId)
        {
            var imageBlob = await BlobRepository.GetBlobAsync(AudioBlobContainerName, userId, audioId, true);
            if (imageBlob == null || !await BlobRepository.BlobExistsAsync(imageBlob))
            {
                // the blob hasn't actually been uploaded yet, so we can't process it
                return CompleteAddAudioNoteResult.AudioNotUploaded;
            }
            
            // if the blob already contains metadata then that means it has already been added
            if (imageBlob.Metadata.ContainsKey(CategoryIdMetadataName))
            {
                return CompleteAddAudioNoteResult.AudioAlreadyCreated;
            }

            // set the blob metadata
            imageBlob.Metadata.Add(CategoryIdMetadataName, categoryId);
            imageBlob.Metadata.Add(UserIdMetadataName, userId);
            await BlobRepository.UpdateBlobMetadataAsync(imageBlob);

            // publish an event into the Event Grid topic
            var subject = $"{userId}/{audioId}";
            await EventGridPublisherService.PostEventGridEventAsync(EventTypes.Audio.AudioCreated, subject, new AudioCreatedEventData { Category = categoryId });

            return CompleteAddAudioNoteResult.Success;
        }

        public async Task<AudioNoteDetails> GetAudioNoteAsync(string id, string userId)
        {
            // get the blob, if it exists
            var audioBlob = await BlobRepository.GetBlobAsync(AudioBlobContainerName, userId, id, true);
            if (audioBlob == null)
            {
                return null;
            }

            // get a SAS token for the blob
            var readPolicy = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5), // to allow for clock skew
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.Read
            };
            var audioUrl = BlobRepository.GetSasTokenForBlob(audioBlob, readPolicy);

            // get the transcript out of the blob metadata
            audioBlob.Metadata.TryGetValue(TranscriptMetadataName, out var transcript);
            
            return new AudioNoteDetails 
            {
                Id = id, 
                AudioUrl = audioUrl, 
                Transcript = transcript
            };
        }

        public async Task<AudioNoteSummaries> ListAudioNotesAsync(string userId)
        {
            var blobs = await BlobRepository.ListBlobsInFolderAsync(AudioBlobContainerName, userId);
            var blobSummaries = blobs
                .Select(b => new AudioNoteSummary
                {
                    Id =  b.Name.Split('/')[1], 
                    Preview = b.Metadata.ContainsKey(TranscriptMetadataName) ? b.Metadata[TranscriptMetadataName].Truncate(TranscriptPreviewLength) : string.Empty
                })
                .ToList();

            var audioNoteSummaries = new AudioNoteSummaries();
            audioNoteSummaries.AddRange(blobSummaries);

            return audioNoteSummaries;
        }

        public async Task DeleteAudioNoteAsync(string id, string userId)
        {
            // delete the blog
            await BlobRepository.DeleteBlobAsync(AudioBlobContainerName, userId, id);

            // fire an event into the Event Grid topic
            var subject = $"{userId}/{id}";
            await EventGridPublisherService.PostEventGridEventAsync(EventTypes.Audio.AudioDeleted, subject, new AudioDeletedEventData());
        }

        public async Task<string> UpdateAudioTranscriptAsync(string id, string userId)
        {
            // get the blob
            var audioBlob = await BlobRepository.GetBlobAsync(AudioBlobContainerName, userId, id, true);
            if (audioBlob == null)
            {
                return null;
            }

            // download file to MemoryStream
            string transcript;
            using (var audioBlobStream = new MemoryStream())
            {
                await BlobRepository.DownloadBlobAsync(audioBlob, audioBlobStream);

                // send to Cognitive Services and get back a transcript
                transcript = await AudioTranscriptionService.GetAudioTranscriptFromCognitiveServicesAsync(audioBlobStream);
            }
            
            // update the blob's metadata
            audioBlob.Metadata[TranscriptMetadataName] = transcript;
            await BlobRepository.UpdateBlobMetadataAsync(audioBlob);

            // create a preview form of the transcript
            var transcriptPreview = transcript.Truncate(TranscriptPreviewLength);

            // fire an event into the Event Grid topic
            var subject = $"{userId}/{id}";
            await EventGridPublisherService.PostEventGridEventAsync(EventTypes.Audio.AudioTranscriptUpdated, subject, new AudioTranscriptUpdatedEventData { TranscriptPreview = transcriptPreview });

            return transcriptPreview;
        }
    }
}
