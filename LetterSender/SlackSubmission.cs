using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LetterSender
{
	public class SlackSubmission
	{
		public List<SlackSubmissionAction> Actions { get; set; }
		public string Token { get; set; }
		public SlackSubmissionUser User { get; set; }
		[JsonPropertyName("original_message")]
		public SlackSubmissionOriginalMessage OriginalMessage { get; set; }
		[JsonPropertyName("response_url")]
		public string ResponseUrl { get; set; }
	}

	public class SlackSubmissionUser
	{
		public string Id { get; set; }
		public string Name { get; set; }
	}

	public class SlackSubmissionAction
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public string Value { get; set; }
	}

	public class SlackSubmissionOriginalMessage
	{
		public string Type { get; set; }
		public string Subtype { get; set; }
		public string Text { get; set; }
		public string Ts { get; set; }
		public List<SlackSubmissionAttachment> Attachments { get; set; }
	}

	public class SlackSubmissionAttachment
	{
		[JsonPropertyName("author_name")]
		public string AuthorName { get; set; }
		public string Text { get; set; }
		public string Title { get; set; }
		public List<SlackSubmissionField> Fields { get; set; }
	}

	public class SlackSubmissionField
	{
		public string Title { get; set; }
		public string Value { get; set; }
	}
}