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
    public class TextController : Controller
    {
        private readonly ILogger _logger;
        private readonly ICRUDService _crudService;

        public TextController(ILogger<TextController> logger,ICRUDService service)
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
                return await _crudService.CreateTextAsync(userId, requestBody);
            }
            catch (Exception e)
            {
                _logger.LogError("Create Text Failed " + e);
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        public async Task<dynamic> GetTextNotes(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("Provide a user id");
            }

            try
            {
                return await _crudService.ListTextAsync(userId);
            }
            catch (Exception e)
            {
                _logger.LogError("List Text Failed " + e);
                return BadRequest(e.Message);
            }
        }


        [HttpGet("{textId}")]
        [Route("")]
        public async Task<dynamic> ListTextNotes(string textId)
        {
            string userId = Request.Query
               .SingleOrDefault(q => string.Compare(q.Key, "userId", StringComparison.OrdinalIgnoreCase) == 0)
               .Value;

            try
            {

                return await _crudService.GetTextAsync(userId, textId);

            }
            catch (Exception e)
            {
                _logger.LogError("Get Text Failed " + e);
                return BadRequest(e.Message);
            }
        }

        [HttpPatch("{textId}")]
        public async Task<dynamic> UpdateText(string textId)
        {
            string userId = Request.Query
              .SingleOrDefault(q => string.Compare(q.Key, "userId", StringComparison.OrdinalIgnoreCase) == 0)
              .Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(textId))
            {
                return BadRequest("Missing parameters in request, userId/textId");
            }

            var requestBody = new StreamReader(Request.Body).ReadToEnd();

            if (string.IsNullOrEmpty(requestBody))
            {
                return BadRequest("Missing request body");
            }

            try
            {
                return await _crudService.UpdateTextAsync(userId, textId, requestBody);
            }
            catch (Exception e)
            {
                _logger.LogError("Update Text Failed " + e);
                return BadRequest(e.Message);
            }

        }

        [HttpDelete("{textId}")]
        [Route("")]
        public async Task<dynamic> DeleteText(string textId)
        {
            string userId = Request.Query
                .SingleOrDefault(q => string.Compare(q.Key, "userId", StringComparison.OrdinalIgnoreCase) == 0)
                .Value;
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(textId))
            {
                return BadRequest("Missing parameters in request, userId/textId");
            }

            try
            {
                return await _crudService.DeleteTextAsync(userId, textId);
            }
            catch (Exception e)
            {
                _logger.LogError("Delete Text Failed " + e);
                return BadRequest(e.Message);
            }
        }
    }
}