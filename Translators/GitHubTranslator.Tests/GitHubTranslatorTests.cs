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
			var headers = new Dictionary<string, string[]>() { { "X-Github-Event", new[] { "push" } } };
			var message = new InboundMessage("", headers);

			bool canProcess = _translator.CanProcess(message);

			Assert.IsTrue(canProcess);
		}

		[Test]
		public void CanProcess_is_false_for_not_present_headers()
		{
			var message = new InboundMessage("", new Dictionary<string, string[]>());

			bool canProcess = _translator.CanProcess(message);

			Assert.IsFalse(canProcess);
		}

		[Test]
		public void CanProcess_is_false_for_not_present_push_event()
		{
			var headers = new Dictionary<string, string[]>() { { "X-Github-Event", new[] { "pull" } } };
			var message = new InboundMessage("", headers);

			bool canProcess = _translator.CanProcess(message);

			Assert.IsFalse(canProcess);
		}


		[Test]
		[UseReporter(typeof(DiffReporter))]
		public void Execute_matches_expectations()
		{
			string sample = File.ReadAllText(@".\TestData\ValidMessage.json");
			var message = new InboundMessage(sample, new Dictionary<string, string[]>());

			Translation.Result result = _translator.Execute(message);

			Assert.IsTrue(result.TranslationResult.IsRecognized);
			var translationResult = (InboundMessageResponse.TranslationResult.Recognized)result.TranslationResult;


			Assert.AreEqual(1, translationResult.commits.Count());
			Approvals.Verify(JsonConvert.SerializeObject(translationResult.commits, Formatting.Indented));
		}

		[Test]
		public void Execute_fails_for_invalid_message()
		{
			string sample = File.ReadAllText(@".\TestData\InValidMessage.json");
			var message = new InboundMessage(sample, new Dictionary<string, string[]>());

			var result = _translator.Execute(message);
			Assert.IsTrue(result.TranslationResult.IsFailure);
		}

		[Test]
		[UseReporter(typeof(DiffReporter))]
		public void Execute_matches_expectations_for_single_message_cointaining_three_commits()
		{
			string sample = File.ReadAllText(@".\TestData\ValidMessageWithThreeCommits.json");
			var message = new InboundMessage(sample, new Dictionary<string, string[]>());

			var result = _translator.Execute(message);

			Assert.IsTrue(result.TranslationResult.IsRecognized);
			var translationResult = (InboundMessageResponse.TranslationResult.Recognized)result.TranslationResult;
			Assert.AreEqual(3, translationResult.commits.Count());
			Approvals.Verify(JsonConvert.SerializeObject(translationResult.commits, Formatting.Indented));
		}
	}
}