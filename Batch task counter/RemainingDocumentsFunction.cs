using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace AzureTaskFunctions
{
	public static class RemainingDocumentsFunction
	{
		private static readonly string BatchAccountName = Environment.GetEnvironmentVariable("BatchAccountName");
		private static readonly string BatchAccountKey = Environment.GetEnvironmentVariable("BatchAccountKey");
		private static readonly string BatchAccountUrl = Environment.GetEnvironmentVariable("BatchAccountUrl");
		private static readonly string JobId = Environment.GetEnvironmentVariable("JobId");

		[FunctionName("RemainingDocuments")]
		public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
			HttpRequestMessage req, TraceWriter log)
		{
			var sharedKeyCredentials = new BatchSharedKeyCredentials(BatchAccountUrl, BatchAccountName, BatchAccountKey);
			using (var batchClient = BatchClient.Open(sharedKeyCredentials))
			{
				var taskCounts = await batchClient.JobOperations.GetJobTaskCountsAsync(JobId);
				return req.CreateResponse(taskCounts.Active);
			}
		}
	}
}