using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using Newtonsoft.Json;
using NUnit.Framework;
using VersionOne.CommitService.Interfaces;
using VersionOne.CommitService.Types;

namespace TfsTranslator.Tests
{
	[TestFixture]
	public class TfsTranslatorTests
	{
		private readonly ITranslateInboundMessageToCommits _translator = new TfsTranslator();
		private InboundMessage _validMessage;
		private InboundMessage _invalidMessage;
		private Dictionary<string, string> _headers;


		[SetUp]
		public void SetUp()
		{

			_headers = new Dictionary<string, string>()
			{
				{"User-Agent", "Team Foundation (TfsJobAgent.exe, 10.0.40219.1)"}
			};

			var validSampleData = File.ReadAllText(@".\TestData\ValidMessage.xml");
			_validMessage = new InboundMessage(validSampleData, _headers);


			var inValidSampleData = File.ReadAllText(@".\TestData\InValidMessage.xml");
			_invalidMessage = new InboundMessage(inValidSampleData, _headers);
		}

		[Test]
		public void CanProcess_is_true_for_valid_message_and_useragent()
		{
			bool canProcess = _translator.CanProcess(_validMessage);
			Assert.IsTrue(canProcess);
		}

		[Test]
		public void CanProcess_is_false_for_invalid_useragent()
		{
			_validMessage.Headers["User-Agent"] = "nonsense";
			bool canProcess = _translator.CanProcess(_validMessage);
			Assert.IsFalse(canProcess);
		}

		[Test]
		public void CanProcess_is_false_for_invalid_body_message()
		{
			bool canProcess = _translator.CanProcess(_invalidMessage);
			Assert.IsFalse(canProcess);
		}

		[Test]
		public void Execute_succeeds_for_valid_message()
		{
			Translation.Result result = _translator.Execute(_validMessage);
			var successfulResult = result as Translation.Result.SuccessWithResponse;

			Assert.AreEqual(1, successfulResult.Commits.Count());
			Assert.IsTrue(result.IsSuccessWithResponse);
		}

		[Test]
		[UseReporter(typeof(DiffReporter))]
		public void Execute_fails_on_non_parsable_input_and_response_matches_expectation()
		{
			Translation.Result result = _translator.Execute(_invalidMessage);

			Assert.IsTrue(result.IsFailureWithResponse);
			var failedResult = result as Translation.Result.FailureWithResponse;
			Approvals.Verify(failedResult.Response.Body);
		}


		[Test]
		[UseReporter(typeof(DiffReporter))]
		public void Execute_matches_expectations()
		{
			Translation.Result result = _translator.Execute(_validMessage);

			Assert.IsTrue(result.IsSuccessWithResponse);
			var temp = result as Translation.Result.SuccessWithResponse;
			CommitMessage cm = temp.Commits.FirstOrDefault();
			string json = JsonConvert.SerializeObject(cm, Formatting.Indented);
			Approvals.Verify(json);
		}
	}
}
