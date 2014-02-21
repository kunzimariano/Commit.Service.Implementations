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
		private readonly ITranslateInboundMessageToCommits _translator = new global::TfsTranslator.TfsTranslator();
		private InboundMessage _validAttempt;
		private InboundMessage _invalidAttempt;
		private Dictionary<string, string> _headers;


		[SetUp]
		public void SetUp()
		{

			_headers = new Dictionary<string, string>()
			{
				{"User-Agent", "Team Foundation (TfsJobAgent.exe, 10.0.40219.1)"}
			};

			var validSampleData = File.ReadAllText(@".\TestData\ValidMessage.xml");
			_validAttempt = new InboundMessage(validSampleData, _headers);


			var inValidSampleData = File.ReadAllText(@".\TestData\InValidMessage.xml");
			_invalidAttempt = new InboundMessage(inValidSampleData, _headers);
		}

		[Test]
		public void CanProcess_is_true_for_valid_CommitAttempt_from_TFS_request()
		{
			bool canProcess = _translator.CanProcess(_validAttempt);
			Assert.IsTrue(canProcess);
		}

		[Test]
		public void CanProcess_is_false_for_invalid_UserAgent()
		{
			//TODO: fix this
			_invalidAttempt.Headers["User-Agent"] = "nonsense";
			bool canProcess = _translator.CanProcess(_invalidAttempt);
			Assert.IsFalse(canProcess);
		}

		[Test]
		public void Execute_succeeds_for_valid_CommitAttempt()
		{
			Translation.Result result = _translator.Execute(_validAttempt);
			var successfulResult = result as Translation.Result.SuccessWithResponse;

			Assert.AreEqual(1, successfulResult.Commits.Count());
			Assert.IsTrue(result.IsSuccessWithResponse);
		}

		[Test]
		[UseReporter(typeof(DiffReporter))]
		public void Execute_fails_on_non_parsable_input()
		{
			Translation.Result result = _translator.Execute(_invalidAttempt);
			var failedResult = result as Translation.Result.FailureWithResponse;

			Assert.IsTrue(result.IsFailureWithResponse);
			Approvals.Verify(failedResult.Response.Body);
		}


		[Test]
		[UseReporter(typeof(DiffReporter))]
		public void Execute_Result_Matches_Expectation()
		{
			Translation.Result result = _translator.Execute(_validAttempt);
			var temp = result as Translation.Result.SuccessWithResponse;
			CommitMessage cm = temp.Commits.FirstOrDefault();
			string json = JsonConvert.SerializeObject(cm, Formatting.Indented);
			Approvals.Verify(json);
		}
	}
}
