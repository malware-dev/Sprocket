using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Sprocket.Localization.Generator
{
    /// <summary>Reads the texts out of a .resx file, the way the game does: every <c>data</c> under the root, by its name, with its value.</summary>
    static class Resx
    {
        /// <summary>The entries in <paramref name="xml" />, in the order they are written; null where it is not XML, and <paramref name="problem" /> says why.</summary>
        public static List<Entry> Read(string xml, out string problem)
        {
            problem = null;
            XDocument document;

            try
            {
                document = XDocument.Parse(xml, LoadOptions.SetLineInfo);
            }
            catch (XmlException e)
            {
                problem = e.Message;
                return null;
            }

            var entries = new List<Entry>();
            if (document.Root == null)
                return entries;

            foreach (var data in document.Root.Elements("data"))
            {
                var name = data.Attribute("name");
                var value = data.Element("value");
                if (name == null || value == null)
                    continue;

                var comment = data.Element("comment");
                var line = ((IXmlLineInfo)data).LineNumber - 1;
                entries.Add(new Entry(name.Value, value.Value, comment?.Value, line));
            }

            return entries;
        }

        /// <summary>The culture a translation's file is named for, such as <c>no</c> in <c>MyTexts.no.resx</c>; null for the file the texts are written in.</summary>
        public static string Culture(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var dot = name.IndexOf('.');
            return dot < 0 ? null : name.Substring(dot + 1);
        }

        /// <summary>The name a file of texts has before any culture, such as <c>MyTexts</c>.</summary>
        public static string Stem(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var dot = name.IndexOf('.');
            return dot < 0 ? name : name.Substring(0, dot);
        }
    }
}
