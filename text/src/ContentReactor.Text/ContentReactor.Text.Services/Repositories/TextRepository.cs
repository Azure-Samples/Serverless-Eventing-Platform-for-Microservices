using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ContentReactor.Shared;
using ContentReactor.Text.Services.Models.Data;
using ContentReactor.Text.Services.Models.Responses;
using ContentReactor.Text.Services.Models.Results;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace ContentReactor.Text.Services.Repositories
{
    public interface ITextRepository
    {
        Task<string> AddTextNoteAsync(TextDocument textObject);
        Task<DeleteTextNoteResult> DeleteTextNoteAsync(string textId, string userId);
        Task UpdateTextNoteAsync(TextDocument textDocument);
        Task<TextDocument> GetTextNoteAsync(string textId, string userId);
        Task<IList<TextNoteSummary>> ListTextNotesAsync(string userId);
    }

    public class TextRepository : ITextRepository
    {
        private static readonly string EndpointUrl = Environment.GetEnvironmentVariable("CosmosDBAccountEndpointUrl");
        private static readonly string AccountKey = Environment.GetEnvironmentVariable("CosmosDBAccountKey");
        private static readonly string DatabaseName = Environment.GetEnvironmentVariable("DatabaseName");
        private static readonly string CollectionName = Environment.GetEnvironmentVariable("CollectionName");
        private static readonly DocumentClient DocumentClient = new DocumentClient(new Uri(EndpointUrl), AccountKey);
        
        private const int MaximumTextPreviewLength = 100;

        public async Task<string> AddTextNoteAsync(TextDocument textDocument)
        {
            var documentUri = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);
            Document doc = await DocumentClient.CreateDocumentAsync(documentUri, textDocument);
            return doc.Id;
        }

        public async Task<DeleteTextNoteResult> DeleteTextNoteAsync(string textId, string userId)
        {
            var documentUri = UriFactory.CreateDocumentUri(DatabaseName, CollectionName, textId);
            try
            {
                await DocumentClient.DeleteDocumentAsync(documentUri, new RequestOptions { PartitionKey = new PartitionKey(userId)});
                return DeleteTextNoteResult.Success;
            }
            catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // we return the NotFound result to indicate the document was not found
                return DeleteTextNoteResult.NotFound;
            }
        }

        public Task UpdateTextNoteAsync(TextDocument textDocument)
        {
            var documentUri = UriFactory.CreateDocumentUri(DatabaseName, CollectionName, textDocument.Id);
            return DocumentClient.ReplaceDocumentAsync(documentUri, textDocument);
        }
        
        public async Task<TextDocument> GetTextNoteAsync(string textId, string userId)
        {
            var documentUri = UriFactory.CreateDocumentUri(DatabaseName, CollectionName, textId);
            try
            {
                var documentResponse = await DocumentClient.ReadDocumentAsync<TextDocument>(documentUri, new RequestOptions { PartitionKey = new PartitionKey(userId) });
                return documentResponse.Document;
            }
            catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // we return null to indicate the document was not found
                return null;
            }
        }

        public async Task<IList<TextNoteSummary>> ListTextNotesAsync(string userId)
        {
            var documentUri = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);

            // create a query to just get the document ids
            var query = DocumentClient
                .CreateDocumentQuery<TextDocument>(documentUri)
                .Where(d => d.UserId == userId)
                .Select(d => new TextNoteSummary { Id = d.Id, Preview = d.Text } )
                .AsDocumentQuery();
            
            // iterate until we have all of the ids
            var list = new List<TextNoteSummary>();
            while (query.HasMoreResults)
            {
                var ids = await query.ExecuteNextAsync<TextNoteSummary>();
                list.AddRange(ids.Select(d => new TextNoteSummary { Id = d.Id, Preview = d.Preview.Truncate(MaximumTextPreviewLength) }));
            }
            return list;
        }
    }
}
