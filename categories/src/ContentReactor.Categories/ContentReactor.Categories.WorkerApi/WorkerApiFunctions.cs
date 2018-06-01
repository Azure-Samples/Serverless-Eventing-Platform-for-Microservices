using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using ContentReactor.Categories.Services;
using ContentReactor.Categories.Services.Repositories;
using ContentReactor.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace ContentReactor.Categories.WorkerApi
{
    public static class WorkerApiFunctions
    {
        private static readonly IEventGridSubscriberService EventGridSubscriberService = new EventGridSubscriberService();
        private static readonly ICategoriesService CategoriesService = new CategoriesService(new CategoriesRepository(), new ImageSearchService(new Random(), new HttpClient()), new SynonymService(new HttpClient()), new EventGridPublisherService());

        [FunctionName("UpdateCategorySynonyms")]
        public static async Task<IActionResult> UpdateCategorySynonyms(
            [HttpTrigger(AuthorizationLevel.Function, "post")]HttpRequest req,
            TraceWriter log)
        {
            // authenticate to Event Grid if this is a validation event
            var eventGridValidationOutput = EventGridSubscriberService.HandleSubscriptionValidationEvent(req);
            if (eventGridValidationOutput != null)
            {
                log.Info("Responding to Event Grid subscription verification.");
                return eventGridValidationOutput;
            }

            try
            {
                var (_, userId, categoryId) = EventGridSubscriberService.DeconstructEventGridMessage(req);

                // process the category synonyms
                log.Info($"Updating synonyms for category ID {categoryId}...");
                var updated = await CategoriesService.UpdateCategorySynonymsAsync(categoryId, userId);
                if (!updated)
                {
                    log.Warning("Did not update category synonyms as no synonyms were available.");
                }

                return new OkResult();
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }

        [FunctionName("AddCategoryItem")]
        public static async Task<IActionResult> AddCategoryItem(
            [HttpTrigger(AuthorizationLevel.Function, "post")]HttpRequest req,
            TraceWriter log)
        {
            // authenticate to Event Grid if this is a validation event
            var eventGridValidationOutput = EventGridSubscriberService.HandleSubscriptionValidationEvent(req);
            if (eventGridValidationOutput != null)
            {
                log.Info("Responding to Event Grid subscription verification.");
                return eventGridValidationOutput;
            }
            
            try
            {
                var (eventGridEvent, userId, _) = EventGridSubscriberService.DeconstructEventGridMessage(req);
                
                // process the category item
                await CategoriesService.ProcessAddItemEventAsync(eventGridEvent, userId);

                return new OkResult();
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }

        [FunctionName("UpdateCategoryItem")]
        public static async Task<IActionResult> UpdateCategoryItem(
            [HttpTrigger(AuthorizationLevel.Function, "post")]HttpRequest req,
            TraceWriter log)
        {
            // authenticate to Event Grid if this is a validation event
            var eventGridValidationOutput = EventGridSubscriberService.HandleSubscriptionValidationEvent(req);
            if (eventGridValidationOutput != null)
            {
                log.Info("Responding to Event Grid subscription verification.");
                return eventGridValidationOutput;
            }
            
            try
            {
                var (eventGridEvent, userId, _) = EventGridSubscriberService.DeconstructEventGridMessage(req);
                
                // process the category item
                await CategoriesService.ProcessUpdateItemEventAsync(eventGridEvent, userId);

                return new OkResult();
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }

        [FunctionName("DeleteCategoryItem")]
        public static async Task<IActionResult> DeleteCategoryItem(
            [HttpTrigger(AuthorizationLevel.Function, "post")]HttpRequest req,
            TraceWriter log)
        {
            // authenticate to Event Grid if this is a validation event
            var eventGridValidationOutput = EventGridSubscriberService.HandleSubscriptionValidationEvent(req);
            if (eventGridValidationOutput != null)
            {
                log.Info("Responding to Event Grid subscription verification.");
                return eventGridValidationOutput;
            }
            
            try
            {
                var (eventGridEvent, userId, _) = EventGridSubscriberService.DeconstructEventGridMessage(req);
                
                // process the category item
                await CategoriesService.ProcessDeleteItemEventAsync(eventGridEvent, userId);

                return new OkResult();
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }
        
        [FunctionName("AddCategoryImage")]
        public static async Task<IActionResult> AddCategoryImage(
            [HttpTrigger(AuthorizationLevel.Function, "post")]HttpRequest req,
            TraceWriter log)
        {
            // authenticate to Event Grid if this is a validation event
            var eventGridValidationOutput = EventGridSubscriberService.HandleSubscriptionValidationEvent(req);
            if (eventGridValidationOutput != null)
            {
                log.Info("Responding to Event Grid subscription verification.");
                return eventGridValidationOutput;
            }
            
            try
            {
                var (_, userId, categoryId) = EventGridSubscriberService.DeconstructEventGridMessage(req);

                // process the category image
                log.Info($"Updating image for category ID {categoryId}...");
                var updated = await CategoriesService.UpdateCategoryImageAsync(categoryId, userId);
                if (!updated)
                {
                    log.Warning("Did not update category image as no images were available.");
                }

                return new OkResult();
            }
            catch (Exception ex)
            {
                log.Error("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }
    }
}
