using System.Threading.Tasks;
using ContentReactor.Shared;
using ContentReactor.Shared.EventSchemas.Text;
using ContentReactor.Text.Services.Models.Data;
using ContentReactor.Text.Services.Models.Responses;
using ContentReactor.Text.Services.Models.Results;
using ContentReactor.Text.Services.Repositories;

namespace ContentReactor.Text.Services
{
    public interface ITextService
    {
        Task<string> AddTextNoteAsync(string text, string userId, string categoryId);
        Task<DeleteTextNoteResult> DeleteTextNoteAsync(string textId, string userId);
        Task<UpdateTextNoteResult> UpdateTextNoteAsync(string textId, string userId, string text);
        Task<TextNoteDetails> GetTextNoteAsync(string textId, string userId);
        Task<TextNoteSummaries> ListTextNotesAsync(string userId);
    }

    public class TextService : ITextService
    {
        protected ITextRepository TextRepository;
        protected IEventGridPublisherService EventGridPublisher;

        private const int MaximumTextPreviewLength = 100;

        public TextService(ITextRepository textRepository, IEventGridPublisherService eventGridPublisher)
        {
            TextRepository = textRepository;
            EventGridPublisher = eventGridPublisher;
        }

        public async Task<string> AddTextNoteAsync(string text, string userId, string categoryId)
        {
            // create the document in Cosmos DB
            var textDocument = new TextDocument
            {
                UserId = userId,
                Text = text,
                CategoryId = categoryId
            };
            var textId = await TextRepository.AddTextNoteAsync(textDocument);
            
            // post a TextCreated event to Event Grid
            var eventData = new TextCreatedEventData
            {
                Preview = text.Truncate(MaximumTextPreviewLength),
                Category = categoryId
            };
            var subject = $"{userId}/{textId}";
            await EventGridPublisher.PostEventGridEventAsync(EventTypes.Text.TextCreated, subject, eventData);
            
            return textId;
        }

        public async Task<DeleteTextNoteResult> DeleteTextNoteAsync(string textId, string userId)
        {
            // delete the document from Cosmos DB
            var result = await TextRepository.DeleteTextNoteAsync(textId, userId);
            if (result == DeleteTextNoteResult.NotFound)
            {
                return DeleteTextNoteResult.NotFound;
            }

            // post a TextDeleted event to Event Grid
            var subject = $"{userId}/{textId}";
            await EventGridPublisher.PostEventGridEventAsync(EventTypes.Text.TextDeleted, subject, new TextDeletedEventData());

            return DeleteTextNoteResult.Success;
        }

        public async Task<UpdateTextNoteResult> UpdateTextNoteAsync(string textId, string userId, string text)
        {
            // get the current version of the document from Cosmos DB
            var textDocument = await TextRepository.GetTextNoteAsync(textId, userId);
            if (textDocument == null)
            {
                return UpdateTextNoteResult.NotFound;
            }

            // update the document with the new text
            textDocument.Text = text;
            await TextRepository.UpdateTextNoteAsync(textDocument);

            // post a TextUpdated event to Event Grid
            var eventData = new TextUpdatedEventData
            {
                Preview = text.Truncate(MaximumTextPreviewLength)
            };
            var subject = $"{userId}/{textId}";
            await EventGridPublisher.PostEventGridEventAsync(EventTypes.Text.TextUpdated, subject, eventData);

            return UpdateTextNoteResult.Success;
        }

        public async Task<TextNoteDetails> GetTextNoteAsync(string textId, string userId)
        {
            var textDocument = await TextRepository.GetTextNoteAsync(textId, userId);
            if (textDocument == null)
            {
                return null;
            }

            return new TextNoteDetails
            {
                Id = textDocument.Id,
                Text = textDocument.Text
            };
        }

        public async Task<TextNoteSummaries> ListTextNotesAsync(string userId)
        {
            var blobs = await TextRepository.ListTextNotesAsync(userId);

            var summaries = new TextNoteSummaries();
            summaries.AddRange(blobs);
            return summaries;
        }
    }
}
