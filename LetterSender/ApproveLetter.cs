using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
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
			var urlDecoded = WebUtility.UrlDecode(requestBodyString[8..]);
			var submission = Utils.ExtractJsonPayload<SlackSubmission>(urlDecoded);

			var token = Utils.GetEnvironmentVariable("SLACK_TOKEN");
			if (!token.Equals(submission.Token))
			{
				return new HttpResponseMessage(HttpStatusCode.Accepted);
			}

			log.LogInformation("{ResponseUrl}", submission.ResponseUrl);

			var sender = "brad.cavanagh@gmail.com";
			var receiver = "brad.cavanagh@gmail.com";
			var subject = "test from azure function";
			var body = "this is a test";

			using var client = new AmazonSimpleEmailServiceV2Client(RegionEndpoint.CACentral1);
			var sendRequest = new SendEmailRequest
			{
				FromEmailAddress = sender,
				Destination = new Destination
				{
					ToAddresses = new List<string> { receiver }
				},
				Content = new EmailContent
				{
					Simple = new Message
					{
						Body = new Body { Text = new Content { Data = body } },
						Subject = new Content { Data = subject },
					}
				}
			};
			var response = await client.SendEmailAsync(sendRequest);

			return new HttpResponseMessage(HttpStatusCode.Accepted);
		}
	}
}