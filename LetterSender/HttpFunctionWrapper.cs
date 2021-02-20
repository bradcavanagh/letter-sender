using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LetterSender
{
	public class HttpFunctionWrapper
	{
		public delegate Task<HttpResponseMessage> Handler();

		public static async Task<HttpResponseMessage> WrapHandlerAsync(Handler handler, ILogger logger)
		{
			try
			{
				return await handler();
			}
			catch (Exception e)
			{
				logger.LogError("{StackTrace}", e.StackTrace);
				throw;
			}
		}
	}
}