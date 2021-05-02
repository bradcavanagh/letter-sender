using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

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
			var urlDecoded = WebUtility.UrlDecode(requestBodyString[8..]);
			var submission = Utils.ExtractJsonPayload<SlackSubmission>(urlDecoded);

			var token = Utils.GetEnvironmentVariable("SLACK_TOKEN");
			if (!token.Equals(submission.Token))
			{
				return new HttpResponseMessage(HttpStatusCode.Accepted);
			}

			log.LogInformation("{ResponseUrl}", submission.ResponseUrl);

			var sender = "yesinnewwest@gmail.com";
			var receiver = "brad.cavanagh@gmail.com";
			var subject = "test from azure function";
			var body = "this is a test";

			var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
			var client = new SendGridClient(apiKey);
			var from = new EmailAddress(sender);
			var to = new EmailAddress(receiver);
			var plainTextContent = body;
			var htmlContent = "<strong>and easy to do anywhere, even with C#</strong>";
			var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
			var response = await client.SendEmailAsync(msg);

			var httpClient = new HttpClient();
			var postBody = new Dictionary<string, string> {{"text", "Thanks!"}};
			var content = new FormUrlEncodedContent(postBody);
			var response2 = await httpClient.PostAsJsonAsync(submission.ResponseUrl, content);

			return new HttpResponseMessage(HttpStatusCode.Accepted);
		}
	}
}