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
    public class AudioController : Controller
    {
        private readonly ILogger _logger;
        private readonly ICRUDService _crudService;

        public AudioController(ILogger<AudioController> logger,ICRUDService crudService)
        {
            _logger = logger;
            _crudService = crudService;
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
                return await _crudService.CreateAudioUrlAsync(userId, requestBody);
            }
            catch (Exception e)
            {
                _logger.LogError("Create Audio Url Failed " + e);
                return BadRequest(e.Message);
            }
        }

        [HttpPost("{audioId}")]
        [Route("")]
        public async Task<dynamic> Post(string audioId)
        {
            string userId = Request.Query
             .SingleOrDefault(q => string.Compare(q.Key, "userId", StringComparison.OrdinalIgnoreCase) == 0)
             .Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(audioId))
            {
                return BadRequest("Missing parameters in request, userId / audioId");
            }

            var requestBody = new StreamReader(Request.Body).ReadToEnd();

            if (string.IsNullOrEmpty(requestBody))
            {
                return BadRequest("Missing request body");
            }

            try
            {
                return await _crudService.CreateAudioAsync(userId, audioId, requestBody);
            }
            catch (Exception e)
            {
                _logger.LogError("Create Audio Failed " + e);
                return BadRequest(e.Message);
            }
        }

        [HttpGet("{audioId}")]
        [Route("")]
        public async Task<dynamic> GetAudio(string audioId)
        {
            string userId = Request.Query
                .SingleOrDefault(q => string.Compare(q.Key, "userId", StringComparison.OrdinalIgnoreCase) == 0)
                .Value;

            try
            {
                return await _crudService.GetAudioAsync(userId, audioId);
            }
            catch (Exception e)
            {
                _logger.LogError("Get Audio Failed " + e);
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        public async Task<dynamic> ListAudio(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("Provide a user id");
            }

            try
            {
                return await _crudService.GetAudiosAsync(userId);
            }
            catch (Exception e)
            {
                _logger.LogError("List Audios Failed " + e);
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("{audioId}")]
        [Route("")]
        public async Task<dynamic> DeleteAudio(string audioId)
        {
            string userId = Request.Query
               .SingleOrDefault(q => string.Compare(q.Key, "userId", StringComparison.OrdinalIgnoreCase) == 0)
               .Value;
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(audioId))
            {
                return BadRequest("Missing parameters in request, userId/audioId");
            }
            try
            {
                return await _crudService.DeleteAudioAsync(userId, audioId);
            }
            catch (Exception e)
            {
                _logger.LogError("Delete Audio Failed " + e);
                return BadRequest(e.Message);
            }
        }

    }
}