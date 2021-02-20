using System.Collections.Generic;

namespace LetterSender
{
	public class EmailSubmission
	{
		public string Name { get; set; }
		public string Email { get; set; }
		public string Subject { get; set; }
		public string Content { get; set; }
		public List<string> Recipients { get; set; }
	}
}