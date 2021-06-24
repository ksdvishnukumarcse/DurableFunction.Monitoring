using DurableFunction.Monitoring.Client.Constants;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DurableFunction.Monitoring.Client
{
    /// <summary>
    /// Monitoring
    /// </summary>
    public static class Monitoring
    {
        /// <summary>
        /// Monitorings the orchestrator.
        /// </summary>
        /// <param name="context">The context.</param>
        [FunctionName(AppConstants.MonitoringOrchestrator)]
        public static async Task MonitoringOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            await context.CallActivityAsync<string>(AppConstants.MonitoringActivity, "SomeLocation");

            // sleep for one hour between cleanups
            DateTime nextCleanup = context.CurrentUtcDateTime.AddSeconds(AppConstants.MonitoringInterval);
            await context.CreateTimer(nextCleanup, CancellationToken.None);

            context.ContinueAsNew(null);
        }

        /// <summary>
        /// Monitorings the activity.
        /// </summary>
        /// <param name="someLocation">Some location.</param>
        /// <param name="log">The log.</param>
        [FunctionName(AppConstants.MonitoringActivity)]
        public static async Task MonitoringActivity([ActivityTrigger] string someLocation, ILogger log)
        {
            log.LogInformation($"Cleanup Process running in {someLocation} @ {DateTime.UtcNow}...");
            await Task.Delay(2000); // To Consider it take 2 Seconds in realtime
            log.LogInformation($"Cleanup Process completed in {someLocation} @ {DateTime.UtcNow}...");
        }

        /// <summary>
        /// HTTPs the start.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="starter">The starter.</param>
        /// <param name="log">The log.</param>
        /// <returns>HttpResponseMessage</returns>
        [FunctionName(AppConstants.MonitoringClient)]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync(AppConstants.MonitoringOrchestrator, null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}