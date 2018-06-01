using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignalRMiddleware.Services;

namespace SignalRMiddleware.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class ImageController : Controller
    {
        private readonly ILogger _logger;
        private readonly ICRUDService _crudService;

        public ImageController(ILogger<ImageController> logger,ICRUDService service)
        {
            _logger = logger;
            _crudService = service;
        }

        [HttpPost()]
        public async Task<dynamic> Post()
        {
            string userId = Request.Query
              .SingleOrDefault(q => string.Compare(q.Key, "userId", StringComparison.OrdinalIgnoreCase) == 0)
              .Value;

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("Provide a user id");
            }

            var requestBody = new StreamReader(Request.Body).ReadToEnd();

            if (string.IsNullOrEmpty(requestBody))
            {
                return BadRequest("Missing request body");
            }

            try
            {
                return await _crudService.CreateImageUrlAsync(userId, requestBody);
            }
            catch (Exception e)
            {
                _logger.LogError("Create Image Url Failed " + e);
                return BadRequest(e.Message);
            }
        }

        [HttpPost("{imageId}")]
        [Route("")]
        public async Task<dynamic> Post(string imageId)
        {
            string userId = Request.Query
             .SingleOrDefault(q => string.Compare(q.Key, "userId", StringComparison.OrdinalIgnoreCase) == 0)
             .Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(imageId))
            {
                return BadRequest("Missing parameters in request, userId / imageId");
            }

            var requestBody = new StreamReader(Request.Body).ReadToEnd();

            if (string.IsNullOrEmpty(requestBody))
            {
                return BadRequest("Missing request body");
            }

            try
            {
                return await _crudService.CreateImageAsync(userId, imageId,requestBody);
            }
            catch (Exception e)
            {
                _logger.LogError("Create Image Failed " + e);
                return BadRequest(e.Message);
            }
        }

        [HttpGet("{imageId}")]
        [Route("")]
        public async Task<dynamic> GetImage(string imageId)
        {
            string userId = Request.Query
               .SingleOrDefault(q => string.Compare(q.Key, "userId", StringComparison.OrdinalIgnoreCase) == 0)
               .Value;

            try
            {

                return await _crudService.GetImageAsync(userId, imageId);

            }
            catch (Exception e)
            {
                _logger.LogError("Get Image Failed " + e);
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        public async Task<dynamic> ListImages(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("Provide a user id");
            }

            try
            {
                return await _crudService.GetImagesAsync(userId);
            }
            catch (Exception e)
            {
                _logger.LogError("List Images Failed " + e);
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("{imageId}")]
        [Route("")]
        public async Task<dynamic> DeleteImage(string imageId)
        {
            string userId = Request.Query
               .SingleOrDefault(q => string.Compare(q.Key, "userId", StringComparison.OrdinalIgnoreCase) == 0)
               .Value;
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(imageId))
            {
                return BadRequest("Missing parameters in request, userId/audioId");
            }

            try
            {
                return await _crudService.DeleteImageAsync(userId, imageId);
            }
            catch (Exception e)
            {
                _logger.LogError("Delete Image Failed " + e);
                return BadRequest(e.Message);
            }
        }
    }
}