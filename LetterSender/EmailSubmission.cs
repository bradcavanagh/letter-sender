using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LetterSender
{
	public class EmailSubmission
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }
		[JsonPropertyName("email")]
		public string Email { get; set; }
		[JsonPropertyName("subject")]
		public string Subject { get; set; }
		[JsonPropertyName("content")]
		public string Content { get; set; }
		[JsonPropertyName("recipients")]
		public List<string> Recipients { get; set; }
	}
}