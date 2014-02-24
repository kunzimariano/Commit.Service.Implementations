using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using Newtonsoft.Json;
using NUnit.Framework;
using VersionOne.CommitService.Types;

namespace GitHubTranslator.Tests
{
	[TestFixture]
	public class GitHubCommitAttemptTranslatorTests
	{
		private readonly GitHubTranslator _translator = new GitHubTranslator();

		[Test]
		public void CanProcess_is_true_for_valid_headers()
		{
			var message = new InboundMessage("", new Dictionary<string, string>());
			message.Headers.Add("X-Github-Event", "push");

			bool canProcess = _translator.CanProcess(message);

			Assert.IsTrue(canProcess);
		}

		[Test]
		public void CanProcess_is_false_for_not_present_headers()
		{
			var message = new InboundMessage("", new Dictionary<string, string>());

			bool canProcess = _translator.CanProcess(message);

			Assert.IsFalse(canProcess);
		}

		[Test]
		public void CanProcess_is_false_for_not_present_push_event()
		{
			var message = new InboundMessage("", new Dictionary<string, string>());
			message.Headers.Add("X-Github-Event", "pull");

			bool canProcess = _translator.CanProcess(message);

			Assert.IsFalse(canProcess);
		}


		[Test]
		[UseReporter(typeof(DiffReporter))]
		public void Execute_matches_expectations()
		{
			string sample = File.ReadAllText(@".\TestData\ValidMessage.json");
			var commitAttempt = new InboundMessage(sample, new Dictionary<string, string>());

			var result = (Translation.Result.Success)_translator.Execute(commitAttempt);

			Assert.IsTrue(result.IsSuccess);
			Assert.AreEqual(1, result.Commits.Count());
			Approvals.Verify(JsonConvert.SerializeObject(result.Commits, Formatting.Indented));
		}

		[Test]
		public void Execute_fails_for_invalid_message()
		{
			string sample = File.ReadAllText(@".\TestData\InValidMessage.json");
			var commitAttempt = new InboundMessage(sample, new Dictionary<string, string>());

			var result = (Translation.Result.Failure)_translator.Execute(commitAttempt);

			Assert.IsTrue(result.IsFailure);
		}

		[Test]
		[UseReporter(typeof(DiffReporter))]
		public void Execute_matches_expectations_for_single_message_cointaining_three_commits()
		{
			string sample = File.ReadAllText(@".\TestData\ValidMessageWithThreeCommits.json");
			var message = new InboundMessage(sample, new Dictionary<string, string>());

			var result = (Translation.Result.Success)_translator.Execute(message);

			Assert.IsTrue(result.IsSuccess);
			Assert.AreEqual(3, result.Commits.Count());
			Approvals.Verify(JsonConvert.SerializeObject(result.Commits, Formatting.Indented));
		}
	}
}