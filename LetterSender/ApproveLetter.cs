using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace LetterSender
{
	public static class ApproveLetter
	{
		[FunctionName("ApproveLetter")]
		public static async Task<HttpResponseMessage> RunAsync(
			[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
			HttpRequest req, ILogger log)
		{
			log.LogInformation("C# HTTP trigger function processed a request");

			// The request body from Slack is a string that starts with "payload=",
			// and the remainder is URL-encoded JSON (why). Strip off the beginning
			// and decode the remainder.
			using var streamReader = new StreamReader(req.Body);
			var requestBodyString = await streamReader.ReadToEndAsync();
			var urlDecoded = WebUtility.HtmlDecode(requestBodyString[8..]);

			var submission = Utils.ExtractJsonPayload<SlackSubmission>(urlDecoded);
			log.LogInformation("Body: {RequestBodyString}", requestBodyString);

			var token = Utils.GetEnvironmentVariable("SLACK_TOKEN");
			if (!token.Equals(submission.Token))
			{
				return new HttpResponseMessage(HttpStatusCode.Accepted);
			}

			log.LogInformation("{ResponseUrl}", submission.ResponseUrl);

			return new HttpResponseMessage(HttpStatusCode.Accepted);
		}
	}
}