using System;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MiMFa.Net
{
    /// <summary>Evaluates XPath expressions against XML text or JSON-compatible objects.</summary>
    public sealed class XmlQuery
    {
        public bool Matches(object value, string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("An XPath expression is required.", nameof(expression));

            string xml = ToXml(value);
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            var document = new XmlDocument { XmlResolver = null };
            try
            {
                using (var reader = XmlReader.Create(new StringReader(xml), settings))
                    document.Load(reader);
                object result = document.CreateNavigator().Evaluate(expression);
                var nodes = result as XPathNodeIterator;
                if (nodes != null) return nodes.MoveNext();
                if (result is bool) return (bool)result;
                if (result is double) return (double)result != 0;
                if (result is string) return !string.IsNullOrEmpty((string)result);
                return result != null;
            }
            catch (XPathException exception)
            {
                throw new ArgumentException("The XPath expression is invalid: " + exception.Message, nameof(expression), exception);
            }
            catch (XmlException exception)
            {
                throw new FormatException("The value being searched is not valid XML or a JSON object that can be represented as XML.", exception);
            }
        }

        private static string ToXml(object value)
        {
            if (value == null) return "<root />";
            if (value is string) return (string)value;

            JToken token = value as JToken ?? JToken.FromObject(value);
            if (token.Type == JTokenType.Object)
                return JsonConvert.DeserializeXmlNode(token.ToString(Newtonsoft.Json.Formatting.None), "root")?.OuterXml ?? "<root />";
            if (token.Type == JTokenType.Array)
                return JsonConvert.DeserializeXmlNode("{\"root\":" + token.ToString(Newtonsoft.Json.Formatting.None) + "}")?.OuterXml ?? "<root />";

            return "<root>" + SecurityElementEscape(Convert.ToString(value, CultureInfo.InvariantCulture)) + "</root>";
        }

        private static string SecurityElementEscape(string value) =>
            System.Security.SecurityElement.Escape(value) ?? string.Empty;
    }
}
