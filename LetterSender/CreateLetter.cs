using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace LetterSender
{
	public static class CreateLetter
	{
		[FunctionName("CreateLetter")]
		public static async Task<HttpResponseMessage> RunAsync(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
			HttpRequest req, ILogger log)
		{
			log.LogInformation("C# HTTP trigger function processed a request.");

			var slackWebhookUrl = Utils.GetEnvironmentVariable("SLACK_WEBHOOK_URL");

			var submission = await Utils.ExtractJsonPayload<EmailSubmission>(req.Body);

			var submissionId = Guid.NewGuid();

			await Utils.PostMessageToSlack(submission, submissionId, new Uri(slackWebhookUrl));

			await Utils.SaveMessageToStorageAccount(submission);

			return new HttpResponseMessage(HttpStatusCode.Accepted);
		}
	}
}