using DurableFunction.Monitoring.Client.Constants;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DurableFunction.Monitoring.Client
{
    public static class Monitoring
    {
        [FunctionName(AppConstants.MonitoringOrchestrator)]
        public static async Task MonitoringOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            DateTime nextCheckpoint = context.CurrentUtcDateTime.AddSeconds(AppConstants.MonitoringInterval);
            while (context.CurrentUtcDateTime < nextCheckpoint)
            {
                await context.CallActivityAsync<string>(AppConstants.MonitoringActivity, "SomeLocation");
            }

            await context.CreateTimer(nextCheckpoint, CancellationToken.None);
        }

        [FunctionName(AppConstants.MonitoringActivity)]
        public static async Task MonitoringActivity([ActivityTrigger] string someLocation, ILogger log)
        {
            log.LogInformation($"Cleanup Process running in {someLocation} @ {DateTime.UtcNow}...");
            await Task.Delay(2000); // To Consider it take 2 Seconds in realtime
            log.LogInformation($"Cleanup Process completed in {someLocation} @ {DateTime.UtcNow}...");
        }

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