using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Azure.Storage.Blobs;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using SendGrid.Helpers.Mail;

namespace LetterSender
{
	public static class Utils
	{
		public static async Task<TPayload> ExtractJsonPayload<TPayload>(Stream reqBody)
		{
			using var streamReader = new StreamReader(reqBody);
			var requestBodyString = await streamReader.ReadToEndAsync();
			return ExtractJsonPayload<TPayload>(requestBodyString);
		}

		public static TPayload ExtractJsonPayload<TPayload>(string reqBody)
		{
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,
			};
			return JsonSerializer.Deserialize<TPayload>(reqBody, options);
		}

		public static async Task PostMessageToSlack(EmailSubmission submission, Guid id, Uri uri)
		{
			var payload = new SlackPayload()
			{
				Attachments = new List<SlackAttachment>()
				{
					new SlackAttachment()
					{
						Pretext = "New submission",
						Title = submission.Subject,
						AuthorName = $"{submission.Name} <{submission.Email}>",
						Text = submission.Content,
					},
					new SlackAttachment()
					{
						Fallback = "Your client does not support approving/rejecting messages",
						CallbackId = "submit",
						Actions = new List<SlackAction>()
						{
							new SlackAction()
							{
								Name = "approve",
								Text = "Approve",
								Style = "primary",
								Type = "button",
								Value = id.ToString(),
								Confirm = new SlackConfirm()
								{
									Text = "Are you sure you want to approve this message?",
									OkText = "Yes",
									DismissText = "Not right now"
								}
							},
							new SlackAction()
							{
								Name = "reject",
								Text = "Reject",
								Type = "button",
								Confirm = new SlackConfirm()
								{
									Text = "Are you sure you want to reject this message?",
									OkText = "Yes",
									DismissText = "Not right now"
								}
							}
						}
					}
				}
			};

			if (submission.Recipients?.Count > 0)
			{
				payload.Attachments[0].Fields = new List<SlackField>()
				{
					new SlackField()
					{
						Title = "Recipients",
						Value = string.Join(", ", submission.Recipients),
						Short = false,
					}
				};
			}
			await PostMessage(payload, uri);
		}

		private static async Task PostMessage(SlackPayload payload, Uri uri)
		{
			var payloadJson = JsonSerializer.Serialize(payload);

			using var client = new HttpClient();
			await client.PostAsync(uri.ToString(), new StringContent(payloadJson));
		}

		public static async Task SaveMessageToStorageAccount(EmailSubmission submission)
		{
			var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
			var blobServiceClient = new BlobServiceClient(connectionString);
			var containerName = DateTime.Now.ToString("yyyy-MM-dd");
			var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
			await containerClient.CreateIfNotExistsAsync();

			var blobClient = containerClient.GetBlobClient(DateTime.Now.ToString("O"));

			var options = new JsonSerializerOptions {WriteIndented = true};
			await using var ms = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(submission, options)));
			await blobClient.UploadAsync(ms);
		}

		public static async Task SendEmailAsync(SlackSubmission submission, ILogger log = null)
		{
			const string sender = "yesinnewwest@gmail.com";

			var authorName = submission.OriginalMessage.Attachments[0].AuthorName;
			authorName = HttpUtility.HtmlDecode(authorName);
			log?.LogInformation("Author: {Author}", authorName);

			// authorName is of the form "Name <email>" so pull out the relevant information.
			const string pattern = @"(?<name>.+) \<(?<email>.+)\>";
			var m = Regex.Match(authorName, pattern);
			string emailAuthorName;
			string emailAuthorEmail;
			if (m.Success)
			{
				emailAuthorName = m.Groups["name"].Value;
				emailAuthorEmail = m.Groups["email"].Value;
			}
			else
			{
				return;
			}

			// Pull out the recipients.
			var recipients = submission.OriginalMessage.Attachments[0].Fields[0].Value;
			recipients = HttpUtility.HtmlDecode(recipients);
			log?.LogInformation("Recipients: {Recipients}", recipients);
			var emailRecipients = recipients
				.Split(',', StringSplitOptions.RemoveEmptyEntries)
				.Select(r => r.Split('|')[1].Replace(">", string.Empty))
				.Select(e => new EmailAddress(e))
				.ToList();

			var message = new MimeMessage();
			message.From.Add(new MailboxAddress($"{emailAuthorName} via Yes In New West", sender));
			foreach (var emailRecipient in emailRecipients)
			{
				message.To.Add(new MailboxAddress(emailRecipient.Name, emailRecipient.Email));
			}
			message.Cc.Add(new MailboxAddress(emailAuthorName, emailAuthorEmail));
			message.ReplyTo.Add(new MailboxAddress(emailAuthorName, emailAuthorEmail));

			message.Subject = submission.OriginalMessage.Attachments[0].Title;
			message.Body = new TextPart("plain")
			{
				Text = $"{submission.OriginalMessage.Attachments[0].Text}\n\nThis email was sent by {emailAuthorName} ({emailAuthorEmail}) through the Yes In New West letter sender."
			};

			using var client = new SmtpClient();
			await client.ConnectAsync("smtp.sendgrid.net", 465, true);
			await client.AuthenticateAsync("apikey", Environment.GetEnvironmentVariable("SENDGRID_API_KEY"));
			await client.SendAsync(message);
			await client.DisconnectAsync(true);
	}

		public static async Task SendUpdateToSlack(SlackSubmission submission, bool accept, ILogger log = null)
		{
			var message = accept ? $":white_check_mark: Approved by <@{submission.User.Id}|{submission.User.Name}>" : $":x: Rejected by <@{submission.User.Id}|{submission.User.Name}>";

			var emailAtt = submission.OriginalMessage.Attachments[0];
			var colour = accept ? "good" : "danger";

			var jsonData = JsonSerializer.Serialize(new
			{
				attachments = new object[]
				{
					emailAtt,
					new
					{
						fallback = message,
						color = colour,
						text = message,
						ts = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds
					}
				},
				replace_original = "true",
				response_type = "in_channel"
			});

			var httpClient = new HttpClient();
			await httpClient.PostAsync(submission.ResponseUrl, new StringContent(jsonData));
		}

		public static string GetEnvironmentVariable(string name)
			=> Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
	}
}