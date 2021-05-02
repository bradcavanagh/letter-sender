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
	public class Utils
	{
		public async static Task<TPayload> ExtractJsonPayload<TPayload>(Stream reqBody)
		{
			using (var streamReader = new StreamReader(reqBody))
			{
				var requestBodyString = await streamReader.ReadToEndAsync();
				return ExtractJsonPayload<TPayload>(requestBodyString);
			}
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
			SlackPayload payload = new SlackPayload()
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

		public static async Task PostMessage(SlackPayload payload, Uri uri)
		{
			string payloadJson = JsonSerializer.Serialize(payload);

			using HttpClient client = new HttpClient();
			await client.PostAsync(uri.ToString(), new StringContent(payloadJson));
		}

		public static async Task SaveMessageToStorageAccount(EmailSubmission submission)
		{
			string connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
			BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
			string containerName = DateTime.Now.ToString("yyyy-MM-dd");
			BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
			if (containerClient is null)
			{
				containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName);
			}

			BlobClient blobClient = containerClient.GetBlobClient(DateTime.Now.ToString("O"));

			JsonSerializerOptions options = new JsonSerializerOptions {WriteIndented = true};
			await using MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(submission, options)));
			await blobClient.UploadAsync(ms);
		}

		public static string GetEnvironmentVariable(string name)
			=> Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
	}
}