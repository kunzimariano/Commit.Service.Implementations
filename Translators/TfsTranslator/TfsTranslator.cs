using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;
using VersionOne.CommitService.Interfaces;
using VersionOne.CommitService.Types;

namespace TfsTranslator
{
	[Export(typeof(ITranslateInboundMessageToCommits))]
	public class TfsTranslator : ITranslateInboundMessageToCommits
	{
		private readonly Regex _eventPattern = new Regex(@"(?<=<eventXml>).*?(?=<\/eventXml>)");

		public bool CanProcess(InboundMessage message)
		{
			if (string.IsNullOrEmpty(message.Body))
				return false;

			return IsUserAgentFromTfs(message);
		}

		public Translation.Result Execute(InboundMessage message)
		{
			try
			{
				XDocument document = DecodeContentAndCreateDocument(message);
				CommitMessage checkIn = ParseXmlDocument(document);

				return Translation.SuccessWithResponse(
					new List<CommitMessage> { checkIn },
					new Response() { Body = GetResponseBody(), Headers = GetResponseHeaders() }
					);
			}
			catch
			{
				return Translation.FailureWithResponse(
					"It was not possible to parse the message.",
					new Response() { Body = GetResponseBody(), Headers = GetResponseHeaders() }
					);
			}
		}

		private XDocument DecodeContentAndCreateDocument(InboundMessage attempt)
		{
			string content = HttpUtility.HtmlDecode(attempt.Body);
			Match match = _eventPattern.Match(content);
			var document = XDocument.Parse(match.Value);
			return document;
		}

		private CommitMessage ParseXmlDocument(XDocument document)
		{
			var c = document.Descendants("CheckinEvent").FirstOrDefault();

			//TODO: puke, fix this
			//var creationDate = c.Element("CreationDate").Value;
			//creationDate += " " + c.Element("TimeZoneOffset").Value;
			//var timeOffset = DateTimeOffset.Parse(creationDate);

			var timeOffset = new DateTimeOffset();


			return new CommitMessage(
					new Author() { Name = c.Element("Committer").Value },
					timeOffset,
					c.Element("Comment").Value,
					new Repo() { Name = c.Element("TeamProject").Value },
					"TFS",
					new CommitId() { Name = c.Element("Number").Value },
					null);
		}


		private IDictionary<string, string> GetResponseHeaders()
		{
			return new Dictionary<string, string>()
			{
				{"Content-Type", "application/soap+xml; charset=utf-8"}
			};
		}

		private string GetResponseBody()
		{
			return
				"<?xml version=\"1.0\"?><s:Envelope xmlns:a=\"http://www.w3.org/2005/08/addressing\" xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Header><a:Action s:mustUnderstand=\"1\">http://schemas.microsoft.com/TeamFoundation/2005/06/Services/Notification/03/IService/NotifyResponse</a:Action></s:Header><s:Body xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><NotifyResponse xmlns=\"http://schemas.microsoft.com/TeamFoundation/2005/06/Services/Notification/03\"/></s:Body></s:Envelope>";
		}

		private bool IsUserAgentFromTfs(InboundMessage message)
		{
			return message.Headers["User-Agent"].StartsWith("Team Foundation");
		}
	}
}
