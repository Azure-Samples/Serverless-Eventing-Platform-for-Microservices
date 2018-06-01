using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContentReactor.Categories.Services.Models;
using ContentReactor.Categories.Services.Models.Data;
using ContentReactor.Categories.Services.Models.Response;
using ContentReactor.Categories.Services.Models.Results;
using ContentReactor.Categories.Services.Repositories;

namespace ContentReactor.Categories.Services.Tests
{
    public class FakeCategoriesRepository : ICategoriesRepository
    {
        public readonly IList<CategoryDocument> CategoryDocuments = new List<CategoryDocument>();

        public Task<string> AddCategoryAsync(CategoryDocument categoryDocument)
        {
            if (string.IsNullOrEmpty(categoryDocument.Id))
            {
                categoryDocument.Id = Guid.NewGuid().ToString();
            }

            CategoryDocuments.Add(categoryDocument);
            return Task.FromResult(categoryDocument.Id);
        }

        public Task<DeleteCategoryResult> DeleteCategoryAsync(string categoryId, string userId)
        {
            var documentToRemove = CategoryDocuments.SingleOrDefault(d => d.Id == categoryId && d.UserId == userId);
            if (documentToRemove == null)
            {
                return Task.FromResult(DeleteCategoryResult.NotFound);
            }

            CategoryDocuments.Remove(documentToRemove);
            return Task.FromResult(DeleteCategoryResult.Success);
        }

        public Task UpdateCategoryAsync(CategoryDocument categoryDocument)
        {
            var documentToUpdate = CategoryDocuments.SingleOrDefault(d => d.Id == categoryDocument.Id && d.UserId == categoryDocument.UserId);
            if (documentToUpdate == null)
            {
                throw new InvalidOperationException("UpdateTextAsync called for document that does not exist.");
            }
            documentToUpdate.Name = categoryDocument.Name;
            return Task.CompletedTask;
        }

        public Task<CategoryDocument> GetCategoryAsync(string categoryId, string userId)
        {
            var document = CategoryDocuments.SingleOrDefault(d => d.Id == categoryId && d.UserId == userId);
            return Task.FromResult(document);
        }

        public Task<CategorySummaries> ListCategoriesAsync(string userId)
        {
            var list = CategoryDocuments
                .Where(d => d.UserId == userId)
                .Select(d => new CategorySummary { Id = d.Id, Name = d.Name})
                .ToList();
            var categorySummaries = new CategorySummaries();
            categorySummaries.AddRange(list);
            return Task.FromResult(categorySummaries);
        }

        public Task<CategoryDocument> FindCategoryWithItemAsync(string itemId, ItemType itemType, string userId)
        {
            var list = CategoryDocuments
                .Where(d => d.UserId == userId && d.Items.Any(i => i.Id == itemId && i.Type == itemType))
                .ToList();
            return Task.FromResult(list.SingleOrDefault());
        }
    }
}
