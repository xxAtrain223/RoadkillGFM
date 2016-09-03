using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using Roadkill.Core.Configuration;
using Roadkill.Core.Database;
using Roadkill.Core.Converters;

namespace Roadkill.Plugins.GFM
{
    public class TablesPlugin : Roadkill.Core.Plugins.TextPlugin
    {
        public override string Id
        {
            get { return "GfmTablesPlugin"; }
        }

        public override string Name
        {
            get { return "GFM Tables"; }
        }

        public override string Description
        {
            get { return "Adds Github Flavor Markdown tables.\nNote: Currently requires `|` to be the first and last characters of the line."; }
        }

        public override string Version
        {
            get { return "1.0.0"; }
        }

        public class Table
        {
            public int Begin { get; set; }
            public int End { get; set; }
            public List<string> Rows { get; set; }
            public string Html { get; set; }

            public Table()
            {
                Begin = -1;
                End = -1;
                Rows = new List<string>();
                Html = "";
            }
        }

        public override string BeforeParse(string markdown)
        {
            var mdParser = new MarkdownParser();

            List<string> lines = new List<string>(markdown.Split('\n'));

            List<Table> tables = new List<Table>();
            tables.Add(new Table());

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Trim();

                if (line.StartsWith("|") && line.EndsWith("|"))
                {
                    if (tables[tables.Count - 1].Begin == -1)
                        tables[tables.Count - 1].Begin = i;
                }
                else if (tables[tables.Count - 1].End == -1)
                {
                    if (tables[tables.Count - 1].Begin != -1)
                    {
                        tables[tables.Count - 1].End = i - 1;
                        tables.Add(new Table());
                    }
                }
            }

            if (tables[tables.Count - 1].Begin == -1)
                tables.RemoveAt(tables.Count - 1);
            else if (tables[tables.Count - 1].End == -1)
                tables[tables.Count - 1].End = lines.Count - 1;

            for (int i = tables.Count - 1; i >= 0; i--)
            {
                tables[i].Rows = lines.GetRange(tables[i].Begin, tables[i].End - tables[i].Begin + 1);
                lines.RemoveRange(tables[i].Begin, tables[i].End - tables[i].Begin + 1);

                tables[i].Html += "<table>";

                List<string> aligns = new List<string>();
                foreach (string cell in tables[i].Rows[1].Trim().Trim('|').Split('|'))
                {
                    if (cell.StartsWith(":") && !cell.EndsWith(":"))
                        aligns.Add("left");
                    else if (cell.StartsWith(":") && cell.EndsWith(":"))
                        aligns.Add("center");
                    else if (!cell.StartsWith(":") && cell.EndsWith(":"))
                        aligns.Add("right");
                    else
                        aligns.Add("");
                }

                string[] cells;

                tables[i].Html += "<thead>";
                tables[i].Html += "<tr>";
                cells = tables[i].Rows[0].Trim().Trim('|').Split('|');
                for (int j = 0; j < cells.Length; j++)
                    tables[i].Html += (aligns[j].Length != 0 ? "<th style=\"text-align: " + aligns[j] + "\">" : "<th>") + mdParser.Transform(cells[j].Trim()) + "</th>";
                tables[i].Html += "</tr>";
                tables[i].Html += "</thead>";

                tables[i].Html += "<tbody>";
                foreach (string row in tables[i].Rows.GetRange(2, tables[i].Rows.Count - 2))
                {
                    tables[i].Html += "<tr>";
                    cells = row.Trim().Trim('|').Split('|');
                    for (int j = 0; j < cells.Length; j++)
                        tables[i].Html += (aligns[j].Length != 0 ? "<td style=\"text-align: " + aligns[j] + "\">" : "<td>") + mdParser.Transform(cells[j].Trim()) + "</td>";
                    tables[i].Html += "</tr>";
                }
                tables[i].Html += "</tbody>";

                tables[i].Html += "</table>";

                lines.Insert(tables[i].Begin, tables[i].Html);
            }

            return string.Join("\n", lines);
        }
    }
}