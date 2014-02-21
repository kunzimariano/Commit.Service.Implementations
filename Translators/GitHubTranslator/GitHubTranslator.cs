using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Newtonsoft.Json.Linq;
using VersionOne.CommitService.Interfaces;
using VersionOne.CommitService.Types;

namespace GitHubTranslator
{
	/// <summary>
	/// Translates incoming post-receive hook POST bodies from GitHub into a CommitMessage. See
	/// https://help.github.com/articles/post-receive-hooks for info on the format. 
	/// </summary>
	[Export(typeof(ITranslateInboundMessageToCommits))]
	public class GitHubTranslator : ITranslateInboundMessageToCommits
	{
		public Translation.Result Execute(InboundMessage attempt)
		{
			try
			{
				var body = GetDecodedBody(attempt.Body);
				dynamic root = JObject.Parse(body);

				var commitMessages = GetCommitMessages(root);

				return Translation.Success(commitMessages);
			}
			catch
			{
				return Translation.Failure("It was not possible to translate the message.");
			}
		}

		public bool CanProcess(InboundMessage message)
		{
			return ContainsGitHubEventHeaderAndIsPush(message);
		}

		private IEnumerable<CommitMessage> GetCommitMessages(dynamic root)
		{
			var commitMessages = new List<CommitMessage>();

			foreach (var commit in root.commits)
			{
				var commitMessage = new CommitMessage(
					new Author() { Email = commit.author.email.ToString(), Name = commit.author.name.ToString() },
					DateTimeOffset.Parse(commit.timestamp.ToString()),
					commit.message.ToString(),
					new Repo() { Name = root.repository.name, Url = root.repository.url },
					"GitHub",
					new CommitId() { Name = commit.id.ToString() },
					null
					);

				commitMessages.Add(commitMessage);
			}

			return commitMessages;
		}

		private bool ContainsGitHubEventHeaderAndIsPush(InboundMessage message)
		{
			return message.Headers.ContainsKey("X-Github-Event") && message.Headers["X-Github-Event"] == "push";
		}

		private string GetDecodedBody(string rawBody)
		{
			if (rawBody.Contains("payload="))
			{
				var items = rawBody.Split(new char[] { '=' });
				rawBody = System.Web.HttpUtility.UrlDecode(items[1]);
			}
			return rawBody;
		}
	}
}