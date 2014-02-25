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
		private InboundMessage _validMessageNoHeaders;
		private InboundMessage _inValidMessageNoHeaders;

		[SetUp]
		public void SetUp()
		{
			string validSample = File.ReadAllText(@".\TestData\ValidMessage.json");
			_validMessageNoHeaders = new InboundMessage(validSample, new Dictionary<string, string[]>());

			string invalidSample = File.ReadAllText(@".\TestData\InValidMessage.json");
			_inValidMessageNoHeaders = new InboundMessage(invalidSample, new Dictionary<string, string[]>());
		}

		[Test]
		public void CanProcess_is_true_for_valid_headers()
		{
			_validMessageNoHeaders.Headers.Add("X-Github-Event", new[] { "push" });
			bool canProcess = _translator.CanProcess(_validMessageNoHeaders);

			Assert.IsTrue(canProcess);
		}

		[Test]
		public void CanProcess_is_false_for_not_present_headers()
		{
			bool canProcess = _translator.CanProcess(_validMessageNoHeaders);
			Assert.IsFalse(canProcess);
		}

		[Test]
		public void CanProcess_is_false_for_not_present_push_event()
		{
			_validMessageNoHeaders.Headers.Add("X-Github-Event", new[] { "pull" });
			bool canProcess = _translator.CanProcess(_validMessageNoHeaders);

			Assert.IsFalse(canProcess);
		}

		[Test]
		public void Execute_succeeds_for_valid_message()
		{
			Translation.Result result = _translator.Execute(_validMessageNoHeaders);
			var translationResult = (InboundMessageResponse.TranslationResult.Recognized)result.TranslationResult;
			Assert.AreEqual(1, translationResult.commits.Count());
			Assert.IsTrue(result.TranslationResult.IsRecognized);
		}

		[Test]
		[UseReporter(typeof(DiffReporter))]
		public void Execute_matches_expectation_for_valid_message()
		{
			Translation.Result result = _translator.Execute(_validMessageNoHeaders);
			var translationResult = (InboundMessageResponse.TranslationResult.Recognized)result.TranslationResult;

			Approvals.Verify(JsonConvert.SerializeObject(translationResult.commits, Formatting.Indented));
		}


		[Test]
		public void Execute_fails_for_invalid_message()
		{
			var result = _translator.Execute(_inValidMessageNoHeaders);
			Assert.IsTrue(result.TranslationResult.IsFailure);
		}

		[Test]
		[UseReporter(typeof(DiffReporter))]
		public void Execute_matches_expectations_for_single_message_cointaining_three_commits()
		{
			string sample = File.ReadAllText(@".\TestData\ValidMessageWithThreeCommits.json");
			var message = new InboundMessage(sample, new Dictionary<string, string[]>());

			var result = _translator.Execute(message);

			var translationResult = (InboundMessageResponse.TranslationResult.Recognized)result.TranslationResult;
			Approvals.Verify(JsonConvert.SerializeObject(translationResult.commits, Formatting.Indented));
		}
	}
}