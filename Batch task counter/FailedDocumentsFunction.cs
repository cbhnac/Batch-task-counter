using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace AzureTaskFunctions
{
	public static class FailedDocumentsFunction
	{

		private static readonly string BatchAccountName = Environment.GetEnvironmentVariable("BatchAccountName");
		private static readonly string BatchAccountKey = Environment.GetEnvironmentVariable("BatchAccountKey");
		private static readonly string BatchAccountUrl = Environment.GetEnvironmentVariable("BatchAccountUrl");
		private static readonly string JobId = Environment.GetEnvironmentVariable("JobId");
		private static readonly string LocalPath = Environment.GetEnvironmentVariable("LocalPath");

		[FunctionName("FailedDocuments")]
		public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
			HttpRequestMessage req, TraceWriter log)
		{
			var sharedKeyCredentials = new BatchSharedKeyCredentials(BatchAccountUrl, BatchAccountName, BatchAccountKey);
			using (var batchClient = BatchClient.Open(sharedKeyCredentials))
			{//files/stdout.txt
				var tasks = batchClient.JobOperations.ListTasks(JobId);
				var failedTaskInformation = tasks.Where(task => task.ExecutionInformation.Result == TaskExecutionResult.Failure).Select(task => new { task.Id, task.CommandLine }).ToList();
				var dict = new Dictionary<string, string>();
				
				foreach (var info in failedTaskInformation)
				{
					var splitString = info.CommandLine.Split(':');
					var documentFileId = splitString[1].Split(',')[0];
					var documentId = splitString[2].Split(',')[0];

					if (!Directory.Exists($@"{LocalPath}\{documentFileId}"))
					{
						Directory.CreateDirectory($@"{LocalPath}\{documentFileId}");
					}

					try
					{
						File.WriteAllText($@"{LocalPath}\{documentFileId}\DocumentInformation.txt", $"DocumentFileId: {documentFileId}\nDocumentId: {documentId}");
						File.WriteAllText($@"{LocalPath}\{documentFileId}\stderr.txt", await batchClient.JobOperations.GetNodeFile(JobId, info.Id, "stderr.txt").ReadAsStringAsync());
						File.WriteAllText($@"{LocalPath}\{documentFileId}\stdout.txt", await batchClient.JobOperations.GetNodeFile(JobId, info.Id, "stdout.txt").ReadAsStringAsync());
					}
					catch (Exception e)
					{
						File.WriteAllText($@"{LocalPath}\{documentFileId}\Error.txt", $"DocumentFileId: {documentFileId}\nDocumentId: {documentId}\nCould not download the files from Azure.\n\n{e.Message}");
					}
				}
				return req.CreateResponse(HttpStatusCode.OK);
			}
		}
	}
}