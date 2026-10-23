using HtmlAgilityPack;
using ScrapySharp.Network;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LibrariesEvents
{
    public class Scrapper
    {

        private const string BaseUrl = "https://www.biblioteka.lodz.pl";

        private static readonly Dictionary<string, int> PolishMonths = new(StringComparer.OrdinalIgnoreCase)
        {
            ["stycznia"] = 1,
            ["lutego"] = 2,
            ["marca"] = 3,
            ["kwietnia"] = 4,
            ["maja"] = 5,
            ["czerwca"] = 6,
            ["lipca"] = 7,
            ["sierpnia"] = 8,
            ["września"] = 9,
            ["października"] = 10,
            ["listopada"] = 11,
            ["grudnia"] = 12
        };

        private static readonly Regex DateHeaderRegex = new(@"^(\d{1,2})\s+(\p{L}+)\s+(\d{4})\b", RegexOptions.Compiled);
        private static readonly Regex TimeRegex = new(@"^godz\.?\s*(\d{1,2}):(\d{2})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        public async Task<List<LibEvent>> GetCurrentEvents()
        {
            var today = DateTime.Today;
            var firstOfMonth = new DateTime(today.Year, today.Month, 1);

            var tasks = Enumerable.Range(0, 3)
                .Select(i => GetMonthEvents(firstOfMonth.AddMonths(i)))
                .ToList();

            List<LibEvent>[] results = await Task.WhenAll(tasks);

            return results
                .SelectMany(r => r)
                .Where(e => e.Date >= today)
                .GroupBy(e => (e.Url, e.Date))
                .Select(g => g.First())
                .ToList();
        }

        private static async Task<List<LibEvent>> GetMonthEvents(DateTime month)
        {
            ScrapingBrowser browser = new();
            var uri = new Uri($"{BaseUrl}/wydarzenia/{month.Year}/{month.Month}/");

            WebPage page = await browser.NavigateToPageAsync(uri);
            return ParseMonthPage(page.Html);
        }



        private static List<LibEvent> ParseMonthPage(HtmlNode root)
        {
            foreach (var table in root.SelectNodes("//table")?.ToList() ?? new List<HtmlNode>())
                table.Remove();

            var result = new List<LibEvent>();
            DateTime? currentDay = null;
            LibEvent? current = null;
            bool addressTaken = false;

            foreach (var node in root.Descendants().ToList())
            {
                if (node.NodeType == HtmlNodeType.Element && node.Name == "a")
                {
                    var href = node.GetAttributeValue("href", "");
                    if (currentDay != null && href.Contains("/aktualnosc/"))
                    {
                        current = new LibEvent
                        {
                            Url = new Uri(new Uri(BaseUrl), href).ToString(),
                            Date = currentDay,
                            Name = Clean(node.InnerText)
                        };
                        result.Add(current);
                        addressTaken = false;
                    }
                    continue;
                }

                if (node.NodeType != HtmlNodeType.Text) continue;
                if (node.ParentNode.Name is "script" or "style") continue;
                if (node.Ancestors("a").Any()) continue;

                var text = Clean(node.InnerText);
                if (text.Length == 0) continue;

                if (text.Equals("Filtruj", StringComparison.OrdinalIgnoreCase)) break;

                var dm = DateHeaderRegex.Match(text);
                if (dm.Success && PolishMonths.TryGetValue(dm.Groups[2].Value, out int monthNo))
                {
                    currentDay = new DateTime(int.Parse(dm.Groups[3].Value), monthNo, int.Parse(dm.Groups[1].Value));
                    current = null;
                    continue;
                }

                if (current == null) continue;

                var tm = TimeRegex.Match(text);
                if (tm.Success)
                {
                    current.Date = currentDay!.Value
                        .AddHours(int.Parse(tm.Groups[1].Value))
                        .AddMinutes(int.Parse(tm.Groups[2].Value));
                    continue;
                }

                if (!addressTaken && text.Length <= 60)
                {
                    addressTaken = true;
                }
            }

            return result;
        }

        private static string Clean(string s) =>
            Regex.Replace(HtmlEntity.DeEntitize(s), @"\s+", " ").Trim();
    }
}