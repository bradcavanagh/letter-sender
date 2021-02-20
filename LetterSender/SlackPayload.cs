using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LetterSender
{
	public class SlackPayload
	{
		[JsonPropertyName("attachments")]
		public List<SlackAttachment> Attachments { get; set; }
	}

	public class SlackAttachment
	{
		[JsonPropertyName("pretext")]
		public string Pretext { get; set; }

		[JsonPropertyName("title")]
		public string Title { get; set; }

		[JsonPropertyName("author_name")]
		public string AuthorName { get; set; }

		[JsonPropertyName("text")]
		public string Text { get; set; }

		[JsonPropertyName("fallback")]
		public string Fallback { get; set; }

		[JsonPropertyName("callback_id")]
		public string CallbackId { get; set; }

		[JsonPropertyName("actions")]
		public List<SlackAction> Actions { get; set; }

		[JsonPropertyName("fields")]
		public List<SlackField> Fields { get; set; }
	}

	public class SlackAction
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("text")]
		public string Text { get; set; }

		[JsonPropertyName("style")]
		public string Style { get; set; }

		[JsonPropertyName("type")]
		public string Type { get; set; }

		[JsonPropertyName("value")]
		public string Value { get; set; }

		[JsonPropertyName("confirm")]
		public SlackConfirm Confirm { get; set; }
	}

	public class SlackConfirm
	{
		[JsonPropertyName("text")]
		public string Text { get; set; }

		[JsonPropertyName("ok_text")]
		public string OkText { get; set; }

		[JsonPropertyName("dismiss_text")]
		public string DismissText { get; set; }
	}

	public class SlackField
	{
		[JsonPropertyName("title")]
		public string Title { get; set; }

		[JsonPropertyName("value")]
		public string Value { get; set; }

		[JsonPropertyName("short")]
		public bool Short { get; set; }
	}
}