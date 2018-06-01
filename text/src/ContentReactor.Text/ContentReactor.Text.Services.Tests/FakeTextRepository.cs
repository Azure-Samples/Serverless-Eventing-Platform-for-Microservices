using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContentReactor.Shared;
using ContentReactor.Text.Services.Models.Data;
using ContentReactor.Text.Services.Models.Responses;
using ContentReactor.Text.Services.Models.Results;
using ContentReactor.Text.Services.Repositories;

namespace ContentReactor.Text.Services.Tests
{
    public class FakeTextRepository : ITextRepository
    {
        public readonly IList<TextDocument> TextDocuments = new List<TextDocument>();
        private const int MaximumTextPreviewLength = 100;

        public Task<string> AddTextNoteAsync(TextDocument textDocument)
        {
            if (string.IsNullOrEmpty(textDocument.Id))
            {
                textDocument.Id = Guid.NewGuid().ToString();
            }

            TextDocuments.Add(textDocument);
            return Task.FromResult(textDocument.Id);
        }

        public Task<DeleteTextNoteResult> DeleteTextNoteAsync(string textId, string userId)
        {
            var documentToRemove = TextDocuments.SingleOrDefault(d => d.Id == textId && d.UserId == userId);
            if (documentToRemove == null)
            {
                return Task.FromResult(DeleteTextNoteResult.NotFound);
            }

            TextDocuments.Remove(documentToRemove);
            return Task.FromResult(DeleteTextNoteResult.Success);
        }

        public Task UpdateTextNoteAsync(TextDocument textDocument)
        {
            var documentToUpdate = TextDocuments.SingleOrDefault(d => d.Id == textDocument.Id && d.UserId == textDocument.UserId);
            if (documentToUpdate == null)
            {
                throw new InvalidOperationException("UpdateTextAsync called for document that does not exist.");
            }
            documentToUpdate.Text = textDocument.Text;
            documentToUpdate.CategoryId = textDocument.CategoryId;
            return Task.CompletedTask;
        }

        public Task<TextDocument> GetTextNoteAsync(string textId, string userId)
        {
            var document = TextDocuments.SingleOrDefault(d => d.Id == textId && d.UserId == userId);
            return Task.FromResult(document);
        }

        public Task<IList<TextNoteSummary>> ListTextNotesAsync(string userId)
        {
            IList<TextNoteSummary> list = TextDocuments
                .Where(d => d.UserId == userId)
                .Select(d => new TextNoteSummary { Id = d.Id, Preview = d.Text.Truncate(MaximumTextPreviewLength) })
                .ToList();
            return Task.FromResult(list);
        }
    }
}
