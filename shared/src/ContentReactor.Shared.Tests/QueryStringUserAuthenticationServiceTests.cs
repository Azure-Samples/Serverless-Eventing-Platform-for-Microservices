using System.Threading.Tasks;
using ContentReactor.Shared.UserAuthentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ContentReactor.Shared.Tests
{
    public class QueryStringUserAuthenticationServiceTests
    {
        [Fact]
        public async Task GetUserId_ValidUserId()
        {
            // arrange
            var req = new DefaultHttpRequest(new DefaultHttpContext())
            {
                QueryString = new QueryString("?userId=fakeuserid")
            };
            var service = new QueryStringUserAuthenticationService();

            // act
            var result = await service.GetUserIdAsync(req, out var userId, out var responseResult);

            // assert
            Assert.True(result);
            Assert.Equal("fakeuserid", userId);
            Assert.Null(responseResult);
        }
        
        [Fact]
        public async Task GetUserId_DuplicateUserIds()
        {
            // arrange
            var req = new DefaultHttpRequest(new DefaultHttpContext())
            {
                QueryString = new QueryString("?userId=fakeuserid1&userId=fakeuserid2")
            };
            var service = new QueryStringUserAuthenticationService();

            // act
            var result = await service.GetUserIdAsync(req, out var userId, out var responseResult);

            // assert
            Assert.IsType<BadRequestObjectResult>(responseResult);
            Assert.False(result);
            Assert.Null(userId);
        }

        [Fact]
        public async Task GetUserId_NoUserId()
        {
            // arrange
            var req = new DefaultHttpRequest(new DefaultHttpContext())
            {
                QueryString = new QueryString("?otherParameter=xyz")
            };
            var service = new QueryStringUserAuthenticationService();

            // act
            var result = await service.GetUserIdAsync(req, out var userId, out var responseResult);

            // assert
            Assert.IsType<BadRequestObjectResult>(responseResult);
            Assert.False(result);
            Assert.Null(userId);
        }
    }
}
