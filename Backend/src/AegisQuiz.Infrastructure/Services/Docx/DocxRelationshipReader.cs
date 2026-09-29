using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace AegisQuiz.Infrastructure.Services.Docx
{
    /// <summary>
    /// [Docx Relationship & Media Reader]
    /// Chuyên trách đọc bản đồ quan hệ OpenXML (word/_rels/document.xml.rels),
    /// trích xuất nhị phân toàn bộ media trong word/media/ và trích xuất raw paragraphs.
    /// </summary>
    public static class DocxRelationshipReader
    {
        private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        public static Dictionary<string, string> LoadRelationships(ZipArchive archive)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var entry = archive.GetEntry("word/_rels/document.xml.rels");
            if (entry == null) return map;

            using var stream = entry.Open();
            var doc = XDocument.Load(stream);
            foreach (var rel in doc.Descendants(XName.Get("Relationship", "http://schemas.openxmlformats.org/package/2006/relationships")))
            {
                var id = rel.Attribute("Id")?.Value;
                var target = rel.Attribute("Target")?.Value;
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(target))
                {
                    map[id] = target;
                }
            }

            return map;
        }

        public static Dictionary<string, byte[]> LoadMediaFiles(ZipArchive archive)
        {
            var dict = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.StartsWith("word/media/", StringComparison.OrdinalIgnoreCase))
                {
                    using var s = entry.Open();
                    using var ms = new MemoryStream();
                    s.CopyTo(ms);
                    var key = entry.FullName.Substring("word/".Length); // "media/image1.jpg"
                    dict[key] = ms.ToArray();
                }
            }

            return dict;
        }

        public static List<string> ExtractRawParagraphs(Stream docxStream)
        {
            var paragraphs = new List<string>();
            try
            {
                using var archive = new ZipArchive(docxStream, ZipArchiveMode.Read, true);
                var documentEntry = archive.GetEntry("word/document.xml");
                if (documentEntry == null) return paragraphs;

                using var entryStream = documentEntry.Open();
                var doc = XDocument.Load(entryStream);

                foreach (var p in doc.Descendants(W + "p"))
                {
                    var textParts = new List<string>();
                    foreach (var t in p.Descendants(W + "t"))
                    {
                        textParts.Add(t.Value);
                    }
                    var line = string.Concat(textParts).Trim();
                    if (!string.IsNullOrEmpty(line))
                    {
                        paragraphs.Add(line);
                    }
                }
            }
            catch
            {
                // fail-safe ignore
            }
            return paragraphs;
        }
    }
}
