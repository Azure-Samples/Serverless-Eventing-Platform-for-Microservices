using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ContentReactor.Categories.Services.Models;
using ContentReactor.Categories.Services.Models.Data;
using ContentReactor.Categories.Services.Models.Response;
using ContentReactor.Categories.Services.Models.Results;
using ContentReactor.Shared;
using ContentReactor.Shared.EventSchemas.Audio;
using ContentReactor.Shared.EventSchemas.Categories;
using ContentReactor.Shared.EventSchemas.Images;
using ContentReactor.Shared.EventSchemas.Text;
using Moq;
using Xunit;

namespace ContentReactor.Categories.Services.Tests
{
    public class CategoriesServiceTests
    {
        #region AddCategory Tests
        [Fact]
        public async Task AddCategory_ReturnsDocumentId()
        {
            // arrange
            var service = new CategoriesService(new FakeCategoriesRepository(), new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.AddCategoryAsync("name", "fakeuserid");

            // assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task AddCategory_AddsDocumentToRepository()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.AddCategoryAsync("name", "fakeuserid");

            // assert
            Assert.Equal(1, fakeCategoriesRepository.CategoryDocuments.Count);
            Assert.Contains(fakeCategoriesRepository.CategoryDocuments, d => d.Name == "name" && d.UserId == "fakeuserid");
        }

        [Fact]
        public async Task AddCategory_PublishesCategoryCreatedEventToEventGrid()
        {
            // arrange
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var service = new CategoriesService(new FakeCategoriesRepository(), new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, mockEventGridPublisherService.Object);

            // act
            var categoryId = await service.AddCategoryAsync("name", "fakeuserid");

            // assert
            mockEventGridPublisherService.Verify(m => 
                    m.PostEventGridEventAsync(EventTypes.Categories.CategoryCreated, 
                        $"fakeuserid/{categoryId}", 
                        It.Is<CategoryCreatedEventData>(d => d.Name == "name")),
                Times.Once);
        }
        #endregion

        #region DeleteCategory Tests
        [Fact]
        public async Task DeleteCategory_ReturnsSuccess()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", UserId = "fakeuserid" });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.DeleteCategoryAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal(DeleteCategoryResult.Success, result);
        }

        [Fact]
        public async Task DeleteCategory_DeletesDocumentFromRepository()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", UserId = "fakeuserid" });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.DeleteCategoryAsync("fakeid", "fakeuserid");

            // assert
            Assert.Empty(fakeCategoriesRepository.CategoryDocuments);
        }

        [Fact]
        public async Task DeleteCategory_PublishesCategoryDeletedEventToEventGrid()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", UserId = "fakeuserid" });
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, mockEventGridPublisherService.Object);

            // act
            await service.DeleteCategoryAsync("fakeid", "fakeuserid");

            // assert
            mockEventGridPublisherService.Verify(m => 
                    m.PostEventGridEventAsync(EventTypes.Categories.CategoryDeleted, 
                        "fakeuserid/fakeid", 
                        It.IsAny<CategoryDeletedEventData>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteCategory_InvalidCategoryId_ReturnsNotFound()
        {
            // arrange
            var service = new CategoriesService(new FakeCategoriesRepository(), new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.DeleteCategoryAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal(DeleteCategoryResult.NotFound, result);
        }

        [Fact]
        public async Task DeleteCategory_IncorrectUserId_ReturnsNotFound()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", UserId = "fakeuserid2" });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.DeleteCategoryAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal(DeleteCategoryResult.NotFound, result);
        }
        #endregion

        #region UpdateCategory Tests
        [Fact]
        public async Task UpdateCategory_ReturnsSuccess()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", UserId = "fakeuserid" });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateCategoryAsync("fakeid", "fakeuserid", "newname");

            // assert
            Assert.Equal(UpdateCategoryResult.Success, result);
        }

        [Fact]
        public async Task UpdateCategory_UpdatesDocumentInRepository()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", Name = "oldname", UserId = "fakeuserid" });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);
            
            // act
            await service.UpdateCategoryAsync("fakeid", "fakeuserid", "newname");

            // assert
            Assert.Equal("newname", fakeCategoriesRepository.CategoryDocuments.Single().Name);
        }

        [Fact]
        public async Task UpdateCategory_PublishesCategoryNameUpdatedEventToEventGrid()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", UserId = "fakeuserid"});
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, mockEventGridPublisherService.Object);

            // act
            await service.UpdateCategoryAsync("fakeid", "fakeuserid", "newname");

            // assert
            mockEventGridPublisherService.Verify(m => 
                    m.PostEventGridEventAsync(EventTypes.Categories.CategoryNameUpdated, 
                        "fakeuserid/fakeid",
                        It.Is<CategoryNameUpdatedEventData>(d => d.Name == "newname")),
                Times.Once);
        }

        [Fact]
        public async Task UpdateCategory_InvalidCategoryId_ReturnsNotFound()
        {
            // arrange
            var service = new CategoriesService(new FakeCategoriesRepository(), new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateCategoryAsync("fakeid", "fakeuserid", "newname");

            // assert
            Assert.Equal(UpdateCategoryResult.NotFound, result);
        }

        [Fact]
        public async Task UpdateCategory_IncorrectUserId_ReturnsNotFound()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", UserId = "fakeuserid2" });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateCategoryAsync("fakeid", "fakeuserid", "newname");

            // assert
            Assert.Equal(UpdateCategoryResult.NotFound, result);
        }
        #endregion

        #region GetCategory Tests
        [Fact]
        public async Task GetCategory_ReturnsCorrectText()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", Name = "fakename", UserId = "fakeuserid"});
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.GetCategoryAsync("fakeid", "fakeuserid");

            // assert
            Assert.NotNull(result);
            Assert.Equal("fakeid", result.Id);
            Assert.Equal("fakename", result.Name);
        }

        [Fact]
        public async Task GetCategory_InvalidCategoryId_ReturnsNull()
        {
            // arrange
            var service = new CategoriesService(new FakeCategoriesRepository(), new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.GetCategoryAsync("fakeid", "fakeuserid");

            // assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCategory_IncorrectUserId_ReturnsNull()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", Name = "fakename", UserId = "fakeuserid2"});
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.GetCategoryAsync("fakeid", "fakeuserid");

            // assert
            Assert.Null(result);
        }
        #endregion

        #region ListCategories Tests
        [Fact]
        public async Task ListCategories_ReturnsIds()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid1", Name = "fakename1", UserId = "fakeuserid" });
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid2", Name = "fakename2", UserId = "fakeuserid" });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.ListCategoriesAsync("fakeuserid");

            // assert
            Assert.Equal(2, result.Count);
            var comparer = new CategorySummaryComparer();
            Assert.Contains(new CategorySummary {Id = "fakeid1", Name = "fakename1"}, result, comparer);
            Assert.Contains(new CategorySummary {Id = "fakeid2", Name = "fakename2"}, result, comparer);
        }

        [Fact]
        public async Task ListCategories_DoesNotReturnsIdsForAnotherUser()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid1", Name = "fakename1", UserId = "fakeuserid1" });
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid2", Name = "fakename2", UserId = "fakeuserid2" });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.ListCategoriesAsync("fakeuserid1");

            // assert
            Assert.Single(result);
            var comparer = new CategorySummaryComparer();
            Assert.Contains(new CategorySummary {Id = "fakeid1", Name = "fakename1"}, result, comparer);
        }
        #endregion

        #region UpdateCategoryImage Tests
        [Fact]
        public async Task UpdateCategoryImage_ReturnsTrue()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", Name = "fakename", UserId = "fakeuserid" });
            var mockImageSearchService = new Mock<IImageSearchService>();
            mockImageSearchService
                .Setup(m => m.FindImageUrlAsync("fakename"))
                .ReturnsAsync("http://fake/imageurl.jpg");
            var service = new CategoriesService(fakeCategoriesRepository, mockImageSearchService.Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateCategoryImageAsync("fakeid", "fakeuserid");

            // assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateCategoryImage_UpdatesCategoryDocumentWithImageUrl()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", Name = "fakename", UserId = "fakeuserid" });
            var mockImageSearchService = new Mock<IImageSearchService>();
            mockImageSearchService
                .Setup(m => m.FindImageUrlAsync("fakename"))
                .ReturnsAsync("http://fake/imageurl.jpg");
            var service = new CategoriesService(fakeCategoriesRepository, mockImageSearchService.Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.UpdateCategoryImageAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal("http://fake/imageurl.jpg", fakeCategoriesRepository.CategoryDocuments.Single().ImageUrl);
        }

        [Fact]
        public async Task UpdateCategoryImage_PublishesCategoryImageUpdatedEventToEventGrid()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", Name = "fakename", UserId = "fakeuserid"});
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var mockImageSearchService = new Mock<IImageSearchService>();
            mockImageSearchService
                .Setup(m => m.FindImageUrlAsync("fakename"))
                .ReturnsAsync("http://fake/imageurl.jpg");
            var service = new CategoriesService(fakeCategoriesRepository, mockImageSearchService.Object, new Mock<ISynonymService>().Object, mockEventGridPublisherService.Object);

            // act
            await service.UpdateCategoryImageAsync("fakeid", "fakeuserid");

            // assert
            mockEventGridPublisherService.Verify(m => 
                m.PostEventGridEventAsync(EventTypes.Categories.CategoryImageUpdated, 
                    "fakeuserid/fakeid", 
                    It.Is<CategoryImageUpdatedEventData>(c => c.ImageUrl == "http://fake/imageurl.jpg")),
                Times.Once);
        }

        [Fact]
        public async Task UpdateCategoryImage_ImageNotFound_ReturnsFalse()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", Name = "fakename", UserId = "fakeuserid" });
            var mockImageSearchService = new Mock<IImageSearchService>();
            mockImageSearchService
                .Setup(m => m.FindImageUrlAsync("fakename"))
                .ReturnsAsync((string)null);
            var service = new CategoriesService(fakeCategoriesRepository, mockImageSearchService.Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateCategoryImageAsync("fakeid", "fakeuserid");

            // assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateCategoryImage_UserIdIncorrect_ReturnsFalse()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", Name = "fakename", UserId = "fakeuserid1" });
            var mockImageSearchService = new Mock<IImageSearchService>();
            mockImageSearchService
                .Setup(m => m.FindImageUrlAsync("fakename"))
                .ReturnsAsync("http://fake/imageurl.jpg");
            var service = new CategoriesService(fakeCategoriesRepository, mockImageSearchService.Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateCategoryImageAsync("fakeid", "fakeuserid2");

            // assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateCategoryImage_UserIdIncorrect_DoesNotUpdateCategoryDocumentWithImageUrl()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", Name = "fakename", UserId = "fakeuserid1" });
            var mockImageSearchService = new Mock<IImageSearchService>();
            mockImageSearchService
                .Setup(m => m.FindImageUrlAsync("fakename"))
                .ReturnsAsync("http://fake/imageurl.jpg");
            var service = new CategoriesService(fakeCategoriesRepository, mockImageSearchService.Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.UpdateCategoryImageAsync("fakeid", "fakeuserid2");

            // assert
            Assert.Null(fakeCategoriesRepository.CategoryDocuments.Single().ImageUrl);
        }
        #endregion

        #region UpdateCategorySynonyms Tests
        [Fact]
        public async Task UpdateCategorySynonyms_ReturnsTrue()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", Name = "fakename", UserId = "fakeuserid" });
            var mockSynonymService = new Mock<ISynonymService>();
            mockSynonymService
                .Setup(m => m.GetSynonymsAsync("fakename"))
                .ReturnsAsync(new[] { "a", "b" });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, mockSynonymService.Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateCategorySynonymsAsync("fakeid", "fakeuserid");

            // assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateCategorySynonyms_UpdatesCategoryDocumentWithSynonyms()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", Name = "fakename", UserId = "fakeuserid"});
            var mockSynonymService = new Mock<ISynonymService>();
            mockSynonymService
                .Setup(m => m.GetSynonymsAsync("fakename"))
                .ReturnsAsync(new[] { "a", "b" });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, mockSynonymService.Object, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.UpdateCategorySynonymsAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal(2, fakeCategoriesRepository.CategoryDocuments.Single().Synonyms.Count);
            Assert.Contains("a", fakeCategoriesRepository.CategoryDocuments.Single().Synonyms);
            Assert.Contains("b", fakeCategoriesRepository.CategoryDocuments.Single().Synonyms);
        }

        [Fact]
        public async Task UpdateCategorySynonyms_PublishesCategorySynonymsUpdatedEventToEventGrid()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", Name = "fakename", UserId = "fakeuserid" });
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var mockSynonymService = new Mock<ISynonymService>();
            mockSynonymService
                .Setup(m => m.GetSynonymsAsync("fakename"))
                .ReturnsAsync(new[] { "a", "b" });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, mockSynonymService.Object, mockEventGridPublisherService.Object);

            // act
            await service.UpdateCategorySynonymsAsync("fakeid", "fakeuserid");

            // assert
            mockEventGridPublisherService.Verify(m => 
                m.PostEventGridEventAsync(EventTypes.Categories.CategorySynonymsUpdated, 
                    "fakeuserid/fakeid", 
                    It.Is<CategorySynonymsUpdatedEventData>(c => c.Synonyms.Contains("a") && c.Synonyms.Contains("b"))),
                Times.Once);
        }

        [Fact]
        public async Task UpdateCategorySynonyms_SynonymsNotFound_ReturnsFalse()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", Name = "fakename", UserId = "fakeuserid"});
            var mockSynonymService = new Mock<ISynonymService>();
            mockSynonymService
                .Setup(m => m.GetSynonymsAsync("fakename"))
                .ReturnsAsync((string[])null);
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, mockSynonymService.Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateCategorySynonymsAsync("fakeid", "fakeuserid");

            // assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateCategorySynonyms_UserIdIncorrect_ReturnsFalse()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", Name = "fakename", UserId = "fakeuserid1"});
            var mockSynonymService = new Mock<ISynonymService>();
            mockSynonymService
                .Setup(m => m.GetSynonymsAsync("fakename"))
                .ReturnsAsync(new[] { "a", "b" });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, mockSynonymService.Object, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateCategorySynonymsAsync("fakeid", "fakeuserid2");

            // assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateCategorySynonyms_UserIdIncorrect_DoesNotUpdateCategoryDocumentWithSynonyms()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakeid", Name = "fakename", UserId = "fakeuserid1"});
            var mockSynonymService = new Mock<ISynonymService>();
            mockSynonymService
                .Setup(m => m.GetSynonymsAsync("fakename"))
                .ReturnsAsync(new[] { "a", "b" });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, mockSynonymService.Object, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.UpdateCategorySynonymsAsync("fakeid", "fakeuserid2");

            // assert
            Assert.Empty(fakeCategoriesRepository.CategoryDocuments.Single().Synonyms);
        }
        #endregion

        #region ProcessAddItemEvent Tests
        [Fact]
        public async Task ProcessAddItemEventAsync_AddsTextItem()
        {            
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakecategoryid", Name = "fakename", UserId = "fakeuserid" });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);
            var eventToProcess = new EventGridEvent
            {
                Subject = "fakeuserid/fakeitemid", 
                EventType = EventTypes.Text.TextCreated, 
                Data = new TextCreatedEventData
                {
                    Category = "fakecategoryid",
                    Preview = "fakepreview"
                }
            };

            // act
            await service.ProcessAddItemEventAsync(eventToProcess, "fakeuserid");

            // assert
            var itemsCollection = fakeCategoriesRepository.CategoryDocuments.Single().Items;

            Assert.Equal(1, itemsCollection.Count);
            Assert.Contains(new CategoryItem { Id = "fakeitemid", Preview = "fakepreview", Type = ItemType.Text}, itemsCollection, new CategoryItemComparer());
        }

        [Fact]
        public async Task ProcessAddItemEventAsync_AddsImageItem()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakecategoryid", Name = "fakename", UserId = "fakeuserid"});
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);
            var eventToProcess = new EventGridEvent
            {
                Subject = "fakeuserid/fakeitemid", 
                EventType = EventTypes.Images.ImageCreated, 
                Data = new ImageCreatedEventData()
                {
                    Category = "fakecategoryid",
                    PreviewUri = "http://fake/preview.jpg"
                }
            };

            // act
            await service.ProcessAddItemEventAsync(eventToProcess, "fakeuserid");

            // assert
            var itemsCollection = fakeCategoriesRepository.CategoryDocuments.Single().Items;

            Assert.Equal(1, itemsCollection.Count);
            Assert.Contains(new CategoryItem { Id = "fakeitemid", Preview = "http://fake/preview.jpg", Type = ItemType.Image}, itemsCollection, new CategoryItemComparer());
        }

        [Fact]
        public async Task ProcessAddItemEventAsync_AddsAudioItem()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakecategoryid", Name = "fakename", UserId = "fakeuserid"});
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);
            var eventToProcess = new EventGridEvent
            {
                Subject = "fakeuserid/fakeitemid", 
                EventType = EventTypes.Audio.AudioCreated, 
                Data = new AudioCreatedEventData
                {
                    Category = "fakecategoryid",
                    TranscriptPreview = "faketranscript"
                }
            };

            // act
            await service.ProcessAddItemEventAsync(eventToProcess, "fakeuserid");

            // assert
            var itemsCollection = fakeCategoriesRepository.CategoryDocuments.Single().Items;

            Assert.Equal(1, itemsCollection.Count);
            Assert.Contains(new CategoryItem { Id = "fakeitemid", Preview = "faketranscript", Type = ItemType.Audio}, itemsCollection, new CategoryItemComparer());
        }

        [Fact]
        public async Task ProcessAddItemEventAsync_PublishesCategoryItemsUpdatedEventToEventGrid()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakecategoryid", Name = "fakename", UserId = "fakeuserid"});
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, mockEventGridPublisherService.Object);
            var eventToProcess = new EventGridEvent
            {
                Subject = "fakeuserid/fakeitemid", 
                EventType = EventTypes.Audio.AudioCreated, 
                Data = new AudioCreatedEventData
                {
                    Category = "fakecategoryid",
                    TranscriptPreview = "faketranscript"
                }
            };

            // act
            await service.ProcessAddItemEventAsync(eventToProcess, "fakeuserid");

            // assert
            mockEventGridPublisherService.Verify(m => 
                m.PostEventGridEventAsync(EventTypes.Categories.CategoryItemsUpdated,
                    "fakeuserid/fakecategoryid",
                    It.IsAny<CategoryItemsUpdatedEventData>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessAddItemEventAsync_UpdatesItemWhenAlreadyExists()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakecategoryid", Name = "fakename", UserId = "fakeuserid", Items = new List<CategoryItem>() { new CategoryItem { Id = "fakeitemid", Preview = "oldpreview" } } });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);
            var eventToProcess = new EventGridEvent
            {
                Subject = "fakeuserid/fakeitemid", 
                EventType = EventTypes.Audio.AudioCreated, 
                Data = new AudioCreatedEventData
                {
                    Category = "fakecategoryid",
                    TranscriptPreview = "newpreview"
                }
            };

            // act
            await service.ProcessAddItemEventAsync(eventToProcess, "fakeuserid");

            // assert
            var itemsCollection = fakeCategoriesRepository.CategoryDocuments.Single().Items;

            Assert.Equal(1, itemsCollection.Count);
            Assert.Contains(new CategoryItem { Id = "fakeitemid", Preview = "newpreview", Type = ItemType.Audio}, itemsCollection, new CategoryItemComparer());
        }

        [Fact]
        public async Task ProcessAddItemEventAsync_DoesNotAddItemWhenUserIdDoesNotMatch()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakecategoryid", Name = "fakename", UserId = "fakeuserid1" });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);
            var eventToProcess = new EventGridEvent
            {
                Subject = "fakeuserid2/fakeitemid", 
                EventType = EventTypes.Audio.AudioCreated, 
                Data = new AudioCreatedEventData
                {
                    Category = "fakecategoryid",
                    TranscriptPreview = "newpreview"
                }
            };

            // act
            await service.ProcessAddItemEventAsync(eventToProcess, "fakeuserid2");

            // assert
            var itemsCollection = fakeCategoriesRepository.CategoryDocuments.Single().Items;
            Assert.Equal(0, itemsCollection.Count);
        }

        [Fact]
        public async Task ProcessAddItemEventAsync_ThrowsWhenCategoryNotProvided()
        {
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakecategoryid", Name = "fakename", UserId = "fakeuserid"});
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);
            var eventToProcess = new EventGridEvent
            {
                Subject = "fakeuserid/fakeitemid", 
                EventType = EventTypes.Audio.AudioCreated, 
                Data = new AudioCreatedEventData
                {
                    TranscriptPreview = "faketranscript"
                }
            };

            // act and assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessAddItemEventAsync(eventToProcess, "fakeuserid"));
        }
        #endregion
        
        #region ProcessUpdateItemEvent Tests
        [Fact]
        public async Task ProcessUpdateItemEventAsync_UpdatesTextItem()
        {            
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakecategoryid", Name = "fakename", UserId = "fakeuserid", Items = new List<CategoryItem> { new CategoryItem { Id = "fakeitemid", Type = ItemType.Text, Preview = "oldpreview" } } });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);
            var eventToProcess = new EventGridEvent
            {
                Subject = "fakeuserid/fakeitemid", 
                EventType = EventTypes.Text.TextUpdated, 
                Data = new TextUpdatedEventData
                {
                    Preview = "newpreview"
                }
            };

            // act
            await service.ProcessUpdateItemEventAsync(eventToProcess, "fakeuserid");

            // assert
            var itemsCollection = fakeCategoriesRepository.CategoryDocuments.Single().Items;
            Assert.Equal("newpreview", itemsCollection.Single().Preview);
        }

        [Fact]
        public async Task ProcessUpdateItemEventAsync_UpdatesAudioItem()
        {            
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakecategoryid", Name = "fakename", UserId = "fakeuserid", Items = new List<CategoryItem> { new CategoryItem { Id = "fakeitemid", Type = ItemType.Audio, Preview = "oldpreview" } } });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);
            var eventToProcess = new EventGridEvent
            {
                Subject = "fakeuserid/fakeitemid", 
                EventType = EventTypes.Audio.AudioTranscriptUpdated, 
                Data = new AudioTranscriptUpdatedEventData
                {
                    TranscriptPreview = "newpreview"
                }
            };

            // act
            await service.ProcessUpdateItemEventAsync(eventToProcess, "fakeuserid");

            // assert
            var itemsCollection = fakeCategoriesRepository.CategoryDocuments.Single().Items;
            Assert.Equal("newpreview", itemsCollection.Single().Preview);
        }

        [Fact]
        public async Task ProcessUpdateItemEventAsync_PublishesCategoryItemsUpdatedEventToEventGrid()
        {            
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakecategoryid", Name = "fakename", UserId = "fakeuserid", Items = new List<CategoryItem> { new CategoryItem { Id = "fakeitemid", Type = ItemType.Text, Preview = "oldpreview" } } });
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, mockEventGridPublisherService.Object);
            var eventToProcess = new EventGridEvent
            {
                Subject = "fakeuserid/fakeitemid", 
                EventType = EventTypes.Text.TextUpdated, 
                Data = new TextUpdatedEventData
                {
                    Preview = "newpreview"
                }
            };

            // act
            await service.ProcessUpdateItemEventAsync(eventToProcess, "fakeuserid");

            // assert
            mockEventGridPublisherService.Verify(m => 
                m.PostEventGridEventAsync(EventTypes.Categories.CategoryItemsUpdated,
                    "fakeuserid/fakecategoryid",
                    It.IsAny<CategoryItemsUpdatedEventData>()),
                Times.Once);
        }
        #endregion
        
        #region ProcessDeleteItemEvent Tests
        [Fact]
        public async Task ProcessDeleteItemEventAsync_DeletesTextItem()
        {            
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakecategoryid", Name = "fakename", UserId = "fakeuserid", Items = new List<CategoryItem> { new CategoryItem { Id = "fakeitemid", Type = ItemType.Text } } });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);
            var eventToProcess = new EventGridEvent
            {
                Subject = "fakeuserid/fakeitemid", 
                EventType = EventTypes.Text.TextDeleted, 
                Data = new TextDeletedEventData()
            };

            // act
            await service.ProcessDeleteItemEventAsync(eventToProcess, "fakeuserid");

            // assert
            Assert.Empty(fakeCategoriesRepository.CategoryDocuments.Single().Items);
        }

        [Fact]
        public async Task ProcessDeleteItemEventAsync_DeletesAudioItem()
        {            
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakecategoryid", Name = "fakename", UserId = "fakeuserid", Items = new List<CategoryItem> { new CategoryItem { Id = "fakeitemid", Type = ItemType.Audio } } });
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, new Mock<IEventGridPublisherService>().Object);
            var eventToProcess = new EventGridEvent
            {
                Subject = "fakeuserid/fakeitemid", 
                EventType = EventTypes.Audio.AudioDeleted, 
                Data = new AudioDeletedEventData()
            };

            // act
            await service.ProcessDeleteItemEventAsync(eventToProcess, "fakeuserid");

            // assert
            Assert.Empty(fakeCategoriesRepository.CategoryDocuments.Single().Items);
        }

        [Fact]
        public async Task ProcessDeleteItemEventAsync_PublishesCategoryItemsUpdatedEventToEventGrid()
        {            
            // arrange
            var fakeCategoriesRepository = new FakeCategoriesRepository();
            fakeCategoriesRepository.CategoryDocuments.Add(new CategoryDocument { Id = "fakecategoryid", Name = "fakename", UserId = "fakeuserid", Items = new List<CategoryItem> { new CategoryItem { Id = "fakeitemid", Type = ItemType.Audio } } });
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var service = new CategoriesService(fakeCategoriesRepository, new Mock<IImageSearchService>().Object, new Mock<ISynonymService>().Object, mockEventGridPublisherService.Object);
            var eventToProcess = new EventGridEvent
            {
                Subject = "fakeuserid/fakeitemid", 
                EventType = EventTypes.Audio.AudioDeleted, 
                Data = new AudioDeletedEventData()
            };

            // act
            await service.ProcessDeleteItemEventAsync(eventToProcess, "fakeuserid");

            // assert
            mockEventGridPublisherService.Verify(m => 
                m.PostEventGridEventAsync(EventTypes.Categories.CategoryItemsUpdated,
                    "fakeuserid/fakecategoryid",
                    It.IsAny<CategoryItemsUpdatedEventData>()),
                Times.Once);
        }
        #endregion

        #region Helpers
        private class CategorySummaryComparer: IEqualityComparer<CategorySummary>
        {
            public bool Equals(CategorySummary x, CategorySummary y) => x.Id == y.Id &&
                                                                        x.Name == y.Name;

            public int GetHashCode(CategorySummary obj) => obj.GetHashCode();
        }

        private class CategoryItemComparer: IEqualityComparer<CategoryItem>
        {
            public bool Equals(CategoryItem x, CategoryItem y) => x.Id == y.Id &&
                                                                  x.Preview == y.Preview && 
                                                                  x.Type == y.Type;

            public int GetHashCode(CategoryItem obj) => obj.GetHashCode();
        }
        #endregion
    }
}
