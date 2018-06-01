using System.Linq;
using System.Threading.Tasks;
using ContentReactor.Shared;
using ContentReactor.Shared.EventSchemas.Text;
using ContentReactor.Text.Services.Models.Data;
using ContentReactor.Text.Services.Models.Results;
using Moq;
using Xunit;

namespace ContentReactor.Text.Services.Tests
{
    public class TextServiceTests
    {
        #region AddTextNote Tests
        [Fact]
        public async Task AddTextNote_ReturnsDocumentId()
        {
            // arrange
            var service = new TextService(new FakeTextRepository(), new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.AddTextNoteAsync("text", "fakeuserid", "categoryid");

            // assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task AddTextNote_AddsDocumentToRepository()
        {
            // arrange
            var fakeTextRepository = new FakeTextRepository();
            var service = new TextService(fakeTextRepository, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.AddTextNoteAsync("text", "fakeuserid", "categoryid");

            // assert
            Assert.Contains(fakeTextRepository.TextDocuments, d => d.Text == "text" && d.CategoryId == "categoryid" && d.UserId == "fakeuserid" );
        }

        [Fact]
        public async Task AddTextNote_PublishesTextCreatedEventToEventGrid()
        {
            // arrange
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var service = new TextService(new FakeTextRepository(), mockEventGridPublisherService.Object);

            // act
            var textId = await service.AddTextNoteAsync("text", "fakeuserid", "fakecategory");

            // assert
            mockEventGridPublisherService.Verify(m => 
                m.PostEventGridEventAsync(EventTypes.Text.TextCreated, $"fakeuserid/{textId}", It.Is<TextCreatedEventData>(d => d.Preview == "text" && d.Category == "fakecategory")),
                Times.Once);
        }
        #endregion

        #region DeleteTextNote Tests
        [Fact]
        public async Task DeleteTextNote_ReturnsSuccess()
        {
            // arrange
            var fakeTextRepository = new FakeTextRepository();
            fakeTextRepository.TextDocuments.Add(new TextDocument { Id = "fakeid", UserId = "fakeuserid" });
            var service = new TextService(fakeTextRepository, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.DeleteTextNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal(DeleteTextNoteResult.Success, result);
        }

        [Fact]
        public async Task DeleteTextNote_DeletesDocumentFromRepository()
        {
            // arrange
            var fakeTextRepository = new FakeTextRepository();
            fakeTextRepository.TextDocuments.Add(new TextDocument { Id = "fakeid", UserId = "fakeuserid" });
            var service = new TextService(fakeTextRepository, new Mock<IEventGridPublisherService>().Object);

            // act
            await service.DeleteTextNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.Empty(fakeTextRepository.TextDocuments);
        }

        [Fact]
        public async Task DeleteTextNote_PublishesTextDeletedEventToEventGrid()
        {
            // arrange
            var fakeTextRepository = new FakeTextRepository();
            fakeTextRepository.TextDocuments.Add(new TextDocument { Id = "fakeid", UserId = "fakeuserid" });
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var service = new TextService(fakeTextRepository, mockEventGridPublisherService.Object);

            // act
            await service.DeleteTextNoteAsync("fakeid", "fakeuserid");

            // assert
            mockEventGridPublisherService.Verify(m => 
                    m.PostEventGridEventAsync(EventTypes.Text.TextDeleted, "fakeuserid/fakeid", It.IsAny<TextDeletedEventData>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteTextNote_InvalidTextId_ReturnsNotFound()
        {
            // arrange
            var service = new TextService(new FakeTextRepository(), new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.DeleteTextNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal(DeleteTextNoteResult.NotFound, result);
        }

        [Fact]
        public async Task DeleteTextNote_IncorrectUserId_ReturnsSuccess()
        {
            // arrange
            var fakeTextRepository = new FakeTextRepository();
            fakeTextRepository.TextDocuments.Add(new TextDocument { Id = "fakeid", UserId = "fakeuserid2" });
            var service = new TextService(fakeTextRepository, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.DeleteTextNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.Equal(DeleteTextNoteResult.NotFound, result);
        }
        #endregion

        #region UpdateTextNote Tests
        [Fact]
        public async Task UpdateTextNote_ReturnsSuccess()
        {
            // arrange
            var fakeTextRepository = new FakeTextRepository();
            fakeTextRepository.TextDocuments.Add(new TextDocument { Id = "fakeid", UserId = "fakeuserid" });
            var service = new TextService(fakeTextRepository, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateTextNoteAsync("fakeid", "fakeuserid", "newtext");

            // assert
            Assert.Equal(UpdateTextNoteResult.Success, result);
        }

        [Fact]
        public async Task UpdateTextNote_UpdatesDocumentInRepository()
        {
            // arrange
            var fakeTextRepository = new FakeTextRepository();
            fakeTextRepository.TextDocuments.Add(new TextDocument { Id = "fakeid", Text = "oldtext", CategoryId = "categoryid" , UserId = "fakeuserid" });
            var service = new TextService(fakeTextRepository, new Mock<IEventGridPublisherService>().Object);
            
            // act
            await service.UpdateTextNoteAsync("fakeid", "fakeuserid", "newtext");

            // assert
            Assert.Equal("newtext", fakeTextRepository.TextDocuments.Single().Text);
        }

        [Fact]
        public async Task UpdateTextNote_PublishesTextUpdatedEventToEventGrid()
        {
            // arrange
            var fakeTextRepository = new FakeTextRepository();
            fakeTextRepository.TextDocuments.Add(new TextDocument { Id = "fakeid", UserId = "fakeuserid" });
            var mockEventGridPublisherService = new Mock<IEventGridPublisherService>();
            var service = new TextService(fakeTextRepository, mockEventGridPublisherService.Object);

            // act
            await service.UpdateTextNoteAsync("fakeid", "fakeuserid", "newtext");

            // assert
            mockEventGridPublisherService.Verify(m => 
                    m.PostEventGridEventAsync(EventTypes.Text.TextUpdated, "fakeuserid/fakeid", It.Is<TextUpdatedEventData>(d => d.Preview == "newtext")),
                Times.Once);
        }

        [Fact]
        public async Task UpdateTextNote_InvalidTextId_ReturnsNotFound()
        {
            // arrange
            var service = new TextService(new FakeTextRepository(), new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateTextNoteAsync("fakeid", "fakeuserid", "newtext");

            // assert
            Assert.Equal(UpdateTextNoteResult.NotFound, result);
        }

        [Fact]
        public async Task UpdateTextNote_IncorrectUserId_ReturnsSuccess()
        {
            // arrange
            var fakeTextRepository = new FakeTextRepository();
            fakeTextRepository.TextDocuments.Add(new TextDocument { Id = "fakeid", UserId = "fakeuserid2" });
            var service = new TextService(fakeTextRepository, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.UpdateTextNoteAsync("fakeid", "fakeuserid", "newtext");

            // assert
            Assert.Equal(UpdateTextNoteResult.NotFound, result);
        }
        #endregion

        #region GetTextNote Tests
        [Fact]
        public async Task GetTextNote_ReturnsCorrectText()
        {
            // arrange
            var fakeTextRepository = new FakeTextRepository();
            fakeTextRepository.TextDocuments.Add(new TextDocument { Id = "fakeid", Text = "faketext", CategoryId = "fakecategoryid", UserId = "fakeuserid" });
            var service = new TextService(fakeTextRepository, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.GetTextNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.NotNull(result);
            Assert.Equal("fakeid", result.Id);
            Assert.Equal("faketext", result.Text);
        }

        [Fact]
        public async Task GetTextNote_InvalidTextId_ReturnsNull()
        {
            // arrange
            var service = new TextService(new FakeTextRepository(), new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.GetTextNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetTextNote_IncorrectUserId_ReturnsNull()
        {
            // arrange
            var fakeTextRepository = new FakeTextRepository();
            fakeTextRepository.TextDocuments.Add(new TextDocument { Id = "fakeid", Text = "faketext", CategoryId = "fakecategoryid", UserId = "fakeuserid2" });
            var service = new TextService(fakeTextRepository, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.GetTextNoteAsync("fakeid", "fakeuserid");

            // assert
            Assert.Null(result);
        }
        #endregion
        
        #region ListTextNotes Tests
        [Fact]
        public async Task ListTextNotes_ReturnsIds()
        {
            // arrange
            var fakeTextRepository = new FakeTextRepository();
            fakeTextRepository.TextDocuments.Add(new TextDocument { Id = "fakeid1", UserId = "fakeuserid", Text = "faketext1" });
            fakeTextRepository.TextDocuments.Add(new TextDocument { Id = "fakeid2", UserId = "fakeuserid", Text = "faketext2" });
            var service = new TextService(fakeTextRepository, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.ListTextNotesAsync("fakeuserid");

            // assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, s => s.Id == "fakeid1" && s.Preview == "faketext1");
            Assert.Contains(result, s => s.Id == "fakeid2" && s.Preview == "faketext2");
        }

        [Fact]
        public async Task ListTextNotes_DoesNotReturnsIdsForAnotherUser()
        {
            // arrange
            var fakeTextRepository = new FakeTextRepository();
            fakeTextRepository.TextDocuments.Add(new TextDocument { Id = "fakeid1", UserId = "fakeuserid" });
            fakeTextRepository.TextDocuments.Add(new TextDocument { Id = "fakeid2", UserId = "fakeuserid2" });
            var service = new TextService(fakeTextRepository, new Mock<IEventGridPublisherService>().Object);

            // act
            var result = await service.ListTextNotesAsync("fakeuserid");

            // assert
            Assert.Single(result);
            Assert.Equal("fakeid1", result.Single().Id);
        }
        #endregion
    }
}
