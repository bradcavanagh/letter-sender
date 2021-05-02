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

			using var streamReader = new StreamReader(req.Body);
			var requestBodyString = await streamReader.ReadToEndAsync();
			log.LogInformation("Body: {RequestBodyString}", requestBodyString);

			return new HttpResponseMessage(HttpStatusCode.Accepted);
		}
	}
}