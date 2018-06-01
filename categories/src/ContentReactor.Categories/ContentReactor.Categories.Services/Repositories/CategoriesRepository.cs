using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ContentReactor.Categories.Services.Models;
using ContentReactor.Categories.Services.Models.Data;
using ContentReactor.Categories.Services.Models.Response;
using ContentReactor.Categories.Services.Models.Results;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace ContentReactor.Categories.Services.Repositories
{
    public interface ICategoriesRepository
    {
        Task<string> AddCategoryAsync(CategoryDocument categoryObject);
        Task<DeleteCategoryResult> DeleteCategoryAsync(string categoryId, string userId);
        Task UpdateCategoryAsync(CategoryDocument categoryDocument);
        Task<CategoryDocument> GetCategoryAsync(string categoryId, string userId);
        Task<CategorySummaries> ListCategoriesAsync(string userId);
        Task<CategoryDocument> FindCategoryWithItemAsync(string itemId, ItemType itemType, string userId);
    }

    public class CategoriesRepository : ICategoriesRepository
    {
        private static readonly string EndpointUrl = Environment.GetEnvironmentVariable("CosmosDBAccountEndpointUrl");
        private static readonly string AccountKey = Environment.GetEnvironmentVariable("CosmosDBAccountKey");
        private static readonly string DatabaseName = Environment.GetEnvironmentVariable("DatabaseName");
        private static readonly string CollectionName = Environment.GetEnvironmentVariable("CollectionName");
        private static readonly DocumentClient DocumentClient = new DocumentClient(new Uri(EndpointUrl), AccountKey);
        
        public async Task<string> AddCategoryAsync(CategoryDocument categoryDocument)
        {
            var documentUri = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);
            Document doc = await DocumentClient.CreateDocumentAsync(documentUri, categoryDocument);
            return doc.Id;
        }

        public async Task<DeleteCategoryResult> DeleteCategoryAsync(string categoryId, string userId)
        {
            var documentUri = UriFactory.CreateDocumentUri(DatabaseName, CollectionName, categoryId);
            try
            {
                await DocumentClient.DeleteDocumentAsync(documentUri, new RequestOptions { PartitionKey = new PartitionKey(userId) });
                return DeleteCategoryResult.Success;
            }
            catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // we return the NotFound result to indicate the document was not found
                return DeleteCategoryResult.NotFound;
            }
        }

        public Task UpdateCategoryAsync(CategoryDocument categoryDocument)
        {
            var documentUri = UriFactory.CreateDocumentUri(DatabaseName, CollectionName, categoryDocument.Id);
            var concurrencyCondition = new AccessCondition
            {
                Condition = categoryDocument.ETag,
                Type = AccessConditionType.IfMatch
            };
            return DocumentClient.ReplaceDocumentAsync(documentUri, categoryDocument, new RequestOptions { AccessCondition = concurrencyCondition });
        }

        public async Task<CategoryDocument> GetCategoryAsync(string categoryId, string userId)
        {
            var documentUri = UriFactory.CreateDocumentUri(DatabaseName, CollectionName, categoryId);
            try
            {
                var documentResponse = await DocumentClient.ReadDocumentAsync<CategoryDocument>(documentUri, new RequestOptions { PartitionKey = new PartitionKey(userId) } );
                return documentResponse.Document;
            }
            catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // we return null to indicate the document was not found
                return null;
            }
        }

        public async Task<CategoryDocument> FindCategoryWithItemAsync(string itemId, ItemType itemType, string userId)
        {
            var documentUri = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);
            
            // create a query to find the category with this item in it
            var sqlQuery = "SELECT * FROM c WHERE c.userId = @userId AND ARRAY_CONTAINS(c.items, { id: @itemId, type: @itemType }, true)";
            var sqlParameters = new SqlParameterCollection
            {
                new SqlParameter("@userId", userId),
                new SqlParameter("@itemId", itemId),
                new SqlParameter("@itemType", itemType.ToString())
            };
            var query = DocumentClient
                .CreateDocumentQuery<CategoryDocument>(documentUri, new SqlQuerySpec(sqlQuery, sqlParameters))
                .AsDocumentQuery();
            
            // execute the query
            var response = await query.ExecuteNextAsync<CategoryDocument>();
            return response.SingleOrDefault();
        }

        public async Task<CategorySummaries> ListCategoriesAsync(string userId)
        {
            var documentUri = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);

            // create a query to just get the document ids
            var query = DocumentClient
                .CreateDocumentQuery<CategoryDocument>(documentUri)
                .Where(d => d.UserId == userId)
                .Select(d => new CategorySummary { Id = d.Id, Name = d.Name })
                .AsDocumentQuery();
            
            // iterate until we have all of the ids
            var list = new CategorySummaries();
            while (query.HasMoreResults)
            {
                var summaries = await query.ExecuteNextAsync<CategorySummary>();
                list.AddRange(summaries);
            }
            return list;
        }
    }
}
