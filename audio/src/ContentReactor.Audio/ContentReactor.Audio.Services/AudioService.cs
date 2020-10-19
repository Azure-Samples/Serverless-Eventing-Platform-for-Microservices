using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using ContentReactor.Audio.Services.Models.Responses;
using ContentReactor.Audio.Services.Models.Results;
using ContentReactor.Shared;
using ContentReactor.Shared.BlobHelper;
using ContentReactor.Shared.EventSchemas.Audio;

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
        protected BlobHelper BlobHelper;
        protected IAudioTranscriptionService AudioTranscriptionService;
        protected IEventGridPublisherService EventGridPublisherService;

        protected internal const string AudioBlobContainerName = "audio";
        protected internal const string TranscriptMetadataName = "transcript";
        protected internal const string CategoryIdMetadataName = "categoryId";
        protected internal const string UserIdMetadataName = "userId";
        protected internal const int TranscriptPreviewLength = 100;

        public AudioService(BlobHelper blobHelper, IAudioTranscriptionService audioTranscriptionService, IEventGridPublisherService eventGridPublisherService)
        {
            BlobHelper = blobHelper;
            AudioTranscriptionService = audioTranscriptionService;
            EventGridPublisherService = eventGridPublisherService;
        }

        public (string id, string url) BeginAddAudioNote(string userId)
        {
            // generate an ID for this image note
            string audioId = Guid.NewGuid().ToString();

            // create a blob placeholder (which will not have any contents yet)
            BlockBlobClient blob = BlobHelper.GetBlobClient(AudioBlobContainerName, audioId);


            string urlAndSas = BlobHelper.GetSasUriForBlob(blob, BlobSasPermissions.Create | BlobSasPermissions.Write);

            return (audioId, urlAndSas);
        }

        public async Task<CompleteAddAudioNoteResult> CompleteAddAudioNoteAsync(string audioId, string userId, string categoryId)
        {
            BlockBlobClient imageBlob = BlobHelper.GetBlobClient(AudioBlobContainerName, audioId);
            if (imageBlob == null || !await imageBlob.ExistsAsync())
            {
                // the blob hasn't actually been uploaded yet, so we can't process it
                return CompleteAddAudioNoteResult.AudioNotUploaded;
            }

            // if the blob already contains metadata then that means it has already been added
            Response<BlobProperties> response = await imageBlob.GetPropertiesAsync();
            if (response.Value.Metadata.ContainsKey(CategoryIdMetadataName))
            {
                return CompleteAddAudioNoteResult.AudioAlreadyCreated;
            }

            // set the blob metadata
            var metadata = new Dictionary<string, string>
            {
                { CategoryIdMetadataName, categoryId },
                { UserIdMetadataName, userId }
            };

            await imageBlob.SetMetadataAsync(metadata);

            // publish an event into the Event Grid topic
            var subject = $"{userId}/{audioId}";
            await EventGridPublisherService.PostEventGridEventAsync(EventTypes.Audio.AudioCreated, subject, new AudioCreatedEventData { Category = categoryId });

            return CompleteAddAudioNoteResult.Success;
        }

        public async Task<AudioNoteDetails> GetAudioNoteAsync(string id, string userId)
        {
            // get the blob, if it exists
            BlockBlobClient audioBlob = BlobHelper.GetBlobClient(AudioBlobContainerName, id);
            if (audioBlob == null)
            {
                return null;
            }

            string audioUrlAndSas = BlobHelper.GetSasUriForBlob(audioBlob, BlobSasPermissions.Read);

            // get the transcript out of the blob metadata
            Response<BlobProperties> response = await audioBlob.GetPropertiesAsync();
            response.Value.Metadata.TryGetValue(TranscriptMetadataName, out var transcript);

            return new AudioNoteDetails
            {
                Id = id,
                AudioUrl = audioUrlAndSas,
                Transcript = transcript
            };
        }

        public async Task<AudioNoteSummaries> ListAudioNotesAsync(string userId)
        {
            IList<BlobItem> blobs = await BlobHelper.ListBlobsAsync(AudioBlobContainerName);
            var blobSummaries = blobs
                .Select(b => new AudioNoteSummary
                {
                    Id = b.Name.Split('/')[1],
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
            BlockBlobClient blob = BlobHelper.GetBlobClient(AudioBlobContainerName, id);
            await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);

            // fire an event into the Event Grid topic
            var subject = $"{userId}/{id}";
            await EventGridPublisherService.PostEventGridEventAsync(EventTypes.Audio.AudioDeleted, subject, new AudioDeletedEventData());
        }

        public async Task<string> UpdateAudioTranscriptAsync(string id, string userId)
        {
            // get the blob
            BlockBlobClient audioBlob = BlobHelper.GetBlobClient(AudioBlobContainerName, id);
            if (audioBlob == null)
            {
                return null;
            }

            // download file to MemoryStream
            string transcript;
            using (var audioBlobStream = new MemoryStream())
            {
                await audioBlob.DownloadToAsync(audioBlobStream);

                // send to Cognitive Services and get back a transcript
                transcript = await AudioTranscriptionService.GetAudioTranscriptFromCognitiveServicesAsync(audioBlobStream);
            }

            // update the blob's metadata
            var metadata = new Dictionary<string, string>
            {
                { TranscriptMetadataName, transcript }
            };

            await audioBlob.SetMetadataAsync(metadata);
            // create a preview form of the transcript
            string transcriptPreview = transcript.Truncate(TranscriptPreviewLength);

            // fire an event into the Event Grid topic
            var subject = $"{userId}/{id}";
            await EventGridPublisherService.PostEventGridEventAsync(EventTypes.Audio.AudioTranscriptUpdated, subject, new AudioTranscriptUpdatedEventData { TranscriptPreview = transcriptPreview });

            return transcriptPreview;
        }
    }
}
