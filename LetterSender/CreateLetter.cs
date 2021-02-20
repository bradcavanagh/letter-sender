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

			var slackWebhookUrl = "https://hooks.slack.com/services/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";
			//Utils.GetEnvironmentVariable("SLACK_WEBHOOK_URL");

			var submission = await Utils.ExtractJsonPayload<EmailSubmission>(req.Body);

			var submissionId = Guid.NewGuid();

			await Utils.PostMessageToSlack(submission, submissionId, new Uri(slackWebhookUrl));

			return new HttpResponseMessage(HttpStatusCode.Accepted);
		}
	}
}