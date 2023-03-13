using HtmlAgilityPack;
using ScrapySharp.Network;
using System.Globalization;

namespace LibrariesEvents
{
    public class Scrapper
    {
        public async Task<List<LibEvent>> GetCurrentEvents()
        {
            ScrapingBrowser Browser = new();
            WebPage PageResult = await Browser.NavigateToPageAsync(new Uri("https://www.biblioteka.lodz.pl/wydarzenia/"));
            HtmlNode rawHTML = PageResult.Html;
            var nodes2 = PageResult.Html.SelectNodes("//table");
            List<LibEvent> events = new();

            foreach (var item in nodes2)
            {
                foreach (var tabl in item.SelectNodes("tbody"))
                {
                    foreach (var item3 in tabl.SelectNodes("tr"))
                    {
                        var values = item3.SelectNodes("th|td").Select(a => a.InnerText).ToList();
                        var eventProp = new LibEvent
                        {
                            Url = $"https://www.biblioteka.lodz.pl{item3.Attributes["data-url"].Value}",
                            Date = DateTime.ParseExact($"{values[0]} {values[1]}", "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                            Name = values[2],
                            BranchNumber = int.Parse(values[3]),
                            BranchName = values[4],
                            Type = values[5]
                        };
                        events.Add(eventProp);
                    }
                }
            }

            return events;
        }
    }
}