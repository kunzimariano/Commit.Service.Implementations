﻿using System;
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
		private readonly Regex _checkInEventPattern = new Regex("<CheckinEvent xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");

		public bool CanProcess(InboundMessage message)
		{
			return IsUserAgentFromTfs(message.Headers) && IsCheckInEvent(message.Body);
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
					"It was not possible to translate the message.",
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

			return new CommitMessage(
					new Author() { Name = c.Element("Committer").Value },
					ParseDateTimeOffset(c),
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

		private bool IsUserAgentFromTfs(IDictionary<string, string> headers)
		{
			return headers["User-Agent"].StartsWith("Team Foundation");
		}

		private bool IsCheckInEvent(string body)
		{
			if (string.IsNullOrWhiteSpace(body))
				return false;

			string decodedBody = HttpUtility.HtmlDecode(body);
			return _checkInEventPattern.IsMatch(decodedBody);
		}

		private DateTimeOffset ParseDateTimeOffset(XElement e)
		{
			var creationDate = e.Element("CreationDate").Value;
			// ignores the three last characters since .Net does not parse it (-05:00:00)
			var offset = e.Element("TimeZoneOffset").Value.Remove(6);
			string validString = string.Format("{0} {1}", creationDate, offset);
			return DateTimeOffset.Parse(validString);
		}
	}
}
