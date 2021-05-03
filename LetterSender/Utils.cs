using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

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

		public static async Task SendEmailAsync(SlackSubmission submission)
		{
			// var sender = "yesinnewwest@gmail.com";
			// var receiver = "brad.cavanagh@gmail.com";
			// var subject = "test from azure function";
			// var body = "this is a test";
			//
			// var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
			// var client = new SendGridClient(apiKey);
			// var from = new EmailAddress(sender);
			// var to = new EmailAddress(receiver);
			// var plainTextContent = body;
			// var htmlContent = "<strong>and easy to do anywhere, even with C#</strong>";
			// var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
			// var response = await client.SendEmailAsync(msg);
		}

		public static async Task SendUpdateToSlack(SlackSubmission submission, bool accept)
		{
			var message = "";
			if (accept)
			{
				message = $":white_check_mark: Approved by <@{submission.User.Id}|{submission.User.Name}>";
			}
			else
			{
				message = $":x: Rejected by <@{submission.User.Id}|{submission.User.Name}>";
			}

			var emailAtt = submission.OriginalMessage.Attachments[0];
			var colour = accept ? "good" : "danger";

			var httpClient = new HttpClient();
			await httpClient.PostAsJsonAsync(submission.ResponseUrl, new
			{
				attachments = new object[]
				{
					emailAtt,
					new
					{
						fallback = message,
						color = colour,
						text = message,
						ts = (DateTime.UtcNow - new DateTime(1970,1,1)).TotalSeconds
					}
				},
				replace_original = "true",
				response_type = "in_channel"
			});
		}

		public static string GetEnvironmentVariable(string name)
			=> Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
	}
}