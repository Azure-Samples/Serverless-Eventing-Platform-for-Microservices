using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignalRMiddleware.Services;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SignalRMiddleware.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class CategoryController : Controller
    {
        private readonly ILogger _logger;
        private readonly ICRUDService _crudService;

        public CategoryController(ILogger<CategoryController> logger, ICRUDService service)
        {
            _logger = logger;
            _crudService = service;
        }
        [HttpGet]
        public async Task<dynamic> GetCategories(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("Provide a user id");
            }

            try
            {
                return await _crudService.GetCategoriesAsync(userId);
            }
            catch (Exception e)
            {
                _logger.LogError("List Categories Failed " + e);
                return BadRequest(e.Message);
            }
        }


        [HttpGet("{categoryId}")]
        [Route("")]
        public async Task<dynamic> GetCategory(string categoryId)
        {
            string userId = Request.Query
               .SingleOrDefault(q => string.Compare(q.Key, "userId", StringComparison.OrdinalIgnoreCase) == 0)
               .Value;

            try
            {

                return await _crudService.GetCategoryAsync(userId, categoryId);
               
            }
            catch (Exception e)
            {
                _logger.LogError("Get Category Failed " + e);
                return BadRequest(e.Message);
            }
        }



        [HttpDelete("{categoryId}")]
        [Route("")]
        public async Task<dynamic> DeleteCategory(string categoryId)
        {
            string userId = Request.Query
               .SingleOrDefault(q => string.Compare(q.Key, "userId", StringComparison.OrdinalIgnoreCase) == 0)
               .Value;
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(categoryId))
            {
                return BadRequest("Missing parameters in request, userId/categoryId");
            }

            try
            {
                return await _crudService.DeleteCategoryAsync(userId,categoryId);
            }
            catch (Exception e)
            {
                _logger.LogError("Delete Category Failed " + e);
                return BadRequest(e.Message);
            }
        }

        [HttpPatch("{categoryId}")]
        public async Task<dynamic> UpdateCategories(string categoryId)
        {
            
            string userId = Request.Query
               .SingleOrDefault(q => string.Compare(q.Key, "userId", StringComparison.OrdinalIgnoreCase) == 0)
               .Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(categoryId))
            {
                return BadRequest("Missing parameters in request, userId/categoryId");
            }

            var requestBody = new StreamReader(Request.Body).ReadToEnd();

            if (string.IsNullOrEmpty(requestBody))
            {
                return BadRequest("Missing request body");
            }

            try
            {
                return await _crudService.UpdateCategoriesAsync(userId, categoryId, requestBody);
            }
            catch (Exception e)
            {
                _logger.LogError("Update Category Failed " + e);
                return BadRequest(e.Message);
            }

        }

        [HttpPost]
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
                return await _crudService.CreateCategoryAsync(userId, requestBody);
            }
            catch (Exception e)
            {
                _logger.LogError("Create Category Failed " + e);
                return BadRequest(e.Message);
            } 
           
        }
    }
}