using System;
using System.Linq;
using System.Threading.Tasks;
using ContentReactor.Categories.Services.Models;
using ContentReactor.Categories.Services.Models.Data;
using ContentReactor.Categories.Services.Models.Response;
using ContentReactor.Categories.Services.Models.Results;
using ContentReactor.Categories.Services.Repositories;
using ContentReactor.Shared;
using ContentReactor.Shared.EventSchemas.Audio;
using ContentReactor.Shared.EventSchemas.Categories;
using ContentReactor.Shared.EventSchemas.Images;
using ContentReactor.Shared.EventSchemas.Text;

namespace ContentReactor.Categories.Services
{
    public interface ICategoriesService
    {
        Task<string> AddCategoryAsync(string name, string userId);
        Task<DeleteCategoryResult> DeleteCategoryAsync(string categoryId, string userId);
        Task<UpdateCategoryResult> UpdateCategoryAsync(string categoryId, string userId, string name);
        Task<CategoryDetails> GetCategoryAsync(string categoryId, string userId);
        Task<CategorySummaries> ListCategoriesAsync(string userId);
        Task<bool> UpdateCategoryImageAsync(string categoryId, string userId);
        Task<bool> UpdateCategorySynonymsAsync(string categoryId, string userId);
        Task ProcessAddItemEventAsync(EventGridEvent eventToProcess, string userId);
        Task ProcessUpdateItemEventAsync(EventGridEvent eventToProcess, string userId);
        Task ProcessDeleteItemEventAsync(EventGridEvent eventToProcess, string userId);
    }

    public class CategoriesService : ICategoriesService
    {
        protected ICategoriesRepository CategoriesRepository;
        protected IImageSearchService ImageSearchService;
        protected ISynonymService SynonymService;
        protected IEventGridPublisherService EventGridPublisher;
        
        public CategoriesService(ICategoriesRepository categoriesRepository, IImageSearchService imageSearchService, ISynonymService synonymService, IEventGridPublisherService eventGridPublisher)
        {
            CategoriesRepository = categoriesRepository;
            ImageSearchService = imageSearchService;
            SynonymService = synonymService;
            EventGridPublisher = eventGridPublisher;
        }

        public async Task<string> AddCategoryAsync(string name, string userId)
        {
            // create the document in Cosmos DB
            var categoryDocument = new CategoryDocument
            {
                Name = name,
                UserId = userId
            };
            var categoryId = await CategoriesRepository.AddCategoryAsync(categoryDocument);
            
            // post a CategoryCreated event to Event Grid
            var eventData = new CategoryCreatedEventData
            {
                Name = name
            };
            var subject = $"{userId}/{categoryId}";
            await EventGridPublisher.PostEventGridEventAsync(EventTypes.Categories.CategoryCreated, subject, eventData);
            
            return categoryId;
        }

        public async Task<DeleteCategoryResult> DeleteCategoryAsync(string categoryId, string userId)
        {
            // delete the document from Cosmos DB
            var result = await CategoriesRepository.DeleteCategoryAsync(categoryId, userId);
            if (result == DeleteCategoryResult.NotFound)
            {
                return DeleteCategoryResult.NotFound;
            }

            // post a CategoryDeleted event to Event Grid
            var subject = $"{userId}/{categoryId}";
            await EventGridPublisher.PostEventGridEventAsync(EventTypes.Categories.CategoryDeleted, subject, new CategoryDeletedEventData());

            return DeleteCategoryResult.Success;
        }

        public async Task<UpdateCategoryResult> UpdateCategoryAsync(string categoryId, string userId, string name)
        {
            // get the current version of the document from Cosmos DB
            var categoryDocument = await CategoriesRepository.GetCategoryAsync(categoryId, userId);
            if (categoryDocument == null)
            {
                return UpdateCategoryResult.NotFound;
            }

            // update the document with the new name
            categoryDocument.Name = name;
            await CategoriesRepository.UpdateCategoryAsync(categoryDocument);

            // post a CategoryNameUpdated event to Event Grid
            var eventData = new CategoryNameUpdatedEventData
            {
                Name = name
            };
            var subject = $"{userId}/{categoryId}";
            await EventGridPublisher.PostEventGridEventAsync(EventTypes.Categories.CategoryNameUpdated, subject, eventData);

            return UpdateCategoryResult.Success;
        }

        public async Task<CategoryDetails> GetCategoryAsync(string categoryId, string userId)
        {
            var categoryDocument = await CategoriesRepository.GetCategoryAsync(categoryId, userId);
            if (categoryDocument == null)
            {
                return null;
            }

            return new CategoryDetails
            {
                Id = categoryDocument.Id,
                ImageUrl = categoryDocument.ImageUrl,
                Name = categoryDocument.Name,
                Synonyms = categoryDocument.Synonyms,
                Items = categoryDocument.Items.Select(i => new CategoryItemDetails
                {
                    Id = i.Id,
                    Type = i.Type,
                    Preview = i.Preview
                }).ToList()
            };
        }


        public Task<CategorySummaries> ListCategoriesAsync(string userId)
        {
            return CategoriesRepository.ListCategoriesAsync(userId);
        }

        public async Task<bool> UpdateCategorySynonymsAsync(string categoryId, string userId)
        {
            // find the category document
            var categoryDocument = await CategoriesRepository.GetCategoryAsync(categoryId, userId);
            if (categoryDocument == null)
            {
                return false;
            }

            // retrieve the synonyms
            var synonyms = await SynonymService.GetSynonymsAsync(categoryDocument.Name);
            if (synonyms == null)
            {
                return false;
            }

            // get the document again, to reduce the likelihood of concurrency races
            categoryDocument = await CategoriesRepository.GetCategoryAsync(categoryId, userId);

            // update the document with the new name
            categoryDocument.Synonyms = synonyms;
            await CategoriesRepository.UpdateCategoryAsync(categoryDocument);

            // post a CategorySynonymsUpdatedEventData event to Event Grid
            var eventData = new CategorySynonymsUpdatedEventData
            {
                Synonyms = synonyms
            };
            var subject = $"{userId}/{categoryId}";
            await EventGridPublisher.PostEventGridEventAsync(EventTypes.Categories.CategorySynonymsUpdated, subject, eventData);

            return true;
        }

        public async Task<bool> UpdateCategoryImageAsync(string categoryId, string userId)
        {
            // find the category document
            var categoryDocument = await CategoriesRepository.GetCategoryAsync(categoryId, userId);
            if (categoryDocument == null)
            {
                return false;
            }

            // retrieve an image URL
            var imageUrl = await ImageSearchService.FindImageUrlAsync(categoryDocument.Name);
            if (string.IsNullOrEmpty(imageUrl))
            {
                return false;
            }

            // get the document again, to reduce the likelihood of concurrency races
            categoryDocument = await CategoriesRepository.GetCategoryAsync(categoryId, userId);

            // update the document with the new name
            categoryDocument.ImageUrl = imageUrl;
            await CategoriesRepository.UpdateCategoryAsync(categoryDocument);

            // post a CategoryImageUpdatedEventData event to Event Grid
            var eventData = new CategoryImageUpdatedEventData
            {
                ImageUrl = imageUrl
            };
            var subject = $"{userId}/{categoryId}";
            await EventGridPublisher.PostEventGridEventAsync(EventTypes.Categories.CategoryImageUpdated, subject, eventData);

            return true;
        }

        public async Task ProcessAddItemEventAsync(EventGridEvent eventToProcess, string userId)
        {
            // process the item type
            var (item, categoryId, operationType) = ConvertEventGridEventToCategoryItem(eventToProcess);
            if (operationType != OperationType.Add)
            {
                return;
            }

            // find the category document
            var categoryDocument = await CategoriesRepository.GetCategoryAsync(categoryId, userId);
            if (categoryDocument == null)
            {
                return;
            }
            
            // update the document with the new item
            // and if the item already exists in this category, replace it
            var existingItem = categoryDocument.Items.SingleOrDefault(i => i.Id == item.Id);
            if (existingItem != null)
            {
                categoryDocument.Items.Remove(existingItem);
            }
            categoryDocument.Items.Add(item);
            await CategoriesRepository.UpdateCategoryAsync(categoryDocument);

            // post a CategoryItemsUpdated event to Event Grid
            var eventData = new CategoryItemsUpdatedEventData();
            var subject = $"{userId}/{categoryDocument.Id}";
            await EventGridPublisher.PostEventGridEventAsync(EventTypes.Categories.CategoryItemsUpdated, subject, eventData);
        }

        public async Task ProcessUpdateItemEventAsync(EventGridEvent eventToProcess, string userId)
        {
            // process the item type
            var (updatedItem, _, operationType) = ConvertEventGridEventToCategoryItem(eventToProcess);
            if (operationType != OperationType.Update)
            {
                return;
            }

            // find the category document
            var categoryDocument = await CategoriesRepository.FindCategoryWithItemAsync(updatedItem.Id, updatedItem.Type, userId);
            if (categoryDocument == null)
            {
                return;
            }

            // find the item in the document
            var existingItem = categoryDocument.Items.SingleOrDefault(i => i.Id == updatedItem.Id);
            if (existingItem == null)
            {
                return;
            }

            // update the item with the latest changes
            // (the only field that can change is Preview)
            existingItem.Preview = updatedItem.Preview;
            await CategoriesRepository.UpdateCategoryAsync(categoryDocument);
            
            // post a CategoryItemsUpdated event to Event Grid
            var eventData = new CategoryItemsUpdatedEventData();
            var subject = $"{userId}/{categoryDocument.Id}";
            await EventGridPublisher.PostEventGridEventAsync(EventTypes.Categories.CategoryItemsUpdated, subject, eventData);
        }

        public async Task ProcessDeleteItemEventAsync(EventGridEvent eventToProcess, string userId)
        {
            // process the item type
            var (updatedItem, _, operationType) = ConvertEventGridEventToCategoryItem(eventToProcess);
            if (operationType != OperationType.Delete)
            {
                return;
            }

            // find the category document
            var categoryDocument = await CategoriesRepository.FindCategoryWithItemAsync(updatedItem.Id, updatedItem.Type, userId);
            if (categoryDocument == null)
            {
                return;
            }
            
            // find the item in the document
            var itemToRemove = categoryDocument.Items.SingleOrDefault(i => i.Id == updatedItem.Id);
            if (itemToRemove == null)
            {
                return;
            }

            // remove the item from the document
            categoryDocument.Items.Remove(itemToRemove);
            await CategoriesRepository.UpdateCategoryAsync(categoryDocument);

            // post a CategoryItemsUpdated event to Event Grid
            var eventData = new CategoryItemsUpdatedEventData();
            var subject = $"{userId}/{categoryDocument.Id}";
            await EventGridPublisher.PostEventGridEventAsync(EventTypes.Categories.CategoryItemsUpdated, subject, eventData);
        }
        
        private (CategoryItem categoryItem, string categoryId, OperationType operationType) ConvertEventGridEventToCategoryItem(EventGridEvent eventToProcess)
        {
            var item = new CategoryItem
            {
                Id = eventToProcess.Subject.Split('/')[1] // we assume the subject has previously been checked for its format
            };

            string categoryId;
            OperationType operationType;
            switch (eventToProcess.EventType)
            {
                case EventTypes.Audio.AudioCreated:
                    var audioCreatedEventData = (AudioCreatedEventData) eventToProcess.Data;
                    item.Type = ItemType.Audio;
                    item.Preview = audioCreatedEventData.TranscriptPreview;
                    categoryId = audioCreatedEventData.Category;
                    operationType = OperationType.Add;
                    break;
                    
                case EventTypes.Images.ImageCreated:
                    var imageCreatedEventData = (ImageCreatedEventData) eventToProcess.Data;
                    item.Type = ItemType.Image;
                    item.Preview = imageCreatedEventData.PreviewUri;
                    categoryId = imageCreatedEventData.Category;
                    operationType = OperationType.Add;
                    break;

                case EventTypes.Text.TextCreated:
                    var textCreatedEventData = (TextCreatedEventData) eventToProcess.Data;
                    item.Type = ItemType.Text;
                    item.Preview = textCreatedEventData.Preview;
                    categoryId = textCreatedEventData.Category;
                    operationType = OperationType.Add;
                    break;

                case EventTypes.Audio.AudioTranscriptUpdated:
                    var audioTranscriptUpdatedEventData = (AudioTranscriptUpdatedEventData) eventToProcess.Data;
                    item.Type = ItemType.Audio;
                    item.Preview = audioTranscriptUpdatedEventData.TranscriptPreview;
                    categoryId = null;
                    operationType = OperationType.Update;
                    break;

                case EventTypes.Text.TextUpdated:
                    var textUpdatedEventData = (TextUpdatedEventData) eventToProcess.Data;
                    item.Type = ItemType.Text;
                    item.Preview = textUpdatedEventData.Preview;
                    categoryId = null;
                    operationType = OperationType.Update;
                    break;

                case EventTypes.Audio.AudioDeleted:
                    item.Type = ItemType.Audio;
                    categoryId = null;
                    operationType = OperationType.Delete;
                    break;

                case EventTypes.Images.ImageDeleted:
                    item.Type = ItemType.Image;
                    categoryId = null;
                    operationType = OperationType.Delete;
                    break;

                case EventTypes.Text.TextDeleted:
                    item.Type = ItemType.Text;
                    categoryId = null;
                    operationType = OperationType.Delete;
                    break;

                default:
                    throw new ArgumentException($"Unexpected event type '{eventToProcess.EventType}' in {nameof(ProcessAddItemEventAsync)}");
            }

            if (operationType == OperationType.Add && string.IsNullOrEmpty(categoryId))
            {
                throw new InvalidOperationException("Category ID must be set for new items.");
            }
            
            return (item, categoryId, operationType);
        }
    }
}
