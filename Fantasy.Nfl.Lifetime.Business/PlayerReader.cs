using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon;
using Amazon.S3;
using Fantasy.Nfl.Lifetime.Domain;
using HtmlAgilityPack;

namespace Fantasy.Nfl.Lifetime.Business;

public class PlayerReader
{
    private AmazonS3Client _s3Client = new AmazonS3Client(RegionEndpoint.USEast1);
    static readonly HttpClient _client = new HttpClient();


    public async Task CreateLeagueData(int year, SeasonType seasonType)
    {
        List<RosterEntry> roster = await LoadRoster(year).ConfigureAwait(false);
        LeagueData leagueData = await BuildLeeagueData(year, seasonType, roster).ConfigureAwait(false);
        await WriteData(leagueData);
    }

    private async Task WriteData(LeagueData leagueData)
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            Converters = {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
        };

        string jsonString = JsonSerializer.Serialize(leagueData, options);

        var putObjectRequest = new Amazon.S3.Model.PutObjectRequest()
        {
            BucketName = "nfl-lifetime",
            Key = $"assets/{leagueData.Year}.{leagueData.SeasonType}.Data.json",
            ContentBody = jsonString,
            ContentType = "application/json"
        };
        await _s3Client.PutObjectAsync(putObjectRequest);

       // File.WriteAllText(@"D:\Users\Jeremy\Documents\code\fantasy\Fantasy.Nfl.Lifetime\NflLifetime\src\assets\2021.Regular.Data.json", jsonString);
    }

    private async Task<List<RosterEntry>> LoadRoster(int year)
    {
        var roster = new List<RosterEntry>();

        var rosterFileResponse = await _s3Client.GetObjectAsync("nfl-lifetime", $"data/{year}.Roster.txt");
        StreamReader rosterReader = new StreamReader(rosterFileResponse.ResponseStream);
        string rosterContent = rosterReader.ReadToEnd();

        // string rosterContent = File.ReadAllText(@"D:\Users\Jeremy\Documents\code\fantasy\Fantasy.Nfl.Lifetime\Data\2021.Roster.txt");

        var splitChar = rosterContent.Contains("\r\n") ? "\r\n" : "\n";

        var contentRows = rosterContent.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
        roster = contentRows.Select(r => new RosterEntry(r)).ToList();

        return roster;
    }

    private async Task<LeagueData> BuildLeeagueData(int year, SeasonType seasonType, List<RosterEntry> roster)
    {
        var leagueData = new LeagueData(year, seasonType);
        foreach (var rosterEntry in roster)
        {
            Player player = await GetPlayerData(rosterEntry, year, seasonType).ConfigureAwait(false);
            Owner? matchingOwner = leagueData.Owners.FirstOrDefault(o => o.Name == rosterEntry.Owner);

            if (matchingOwner == null)
            {
                matchingOwner = new Owner() { Name = rosterEntry.Owner, Players = new List<Player>() };
                leagueData.Owners.Add(matchingOwner);
            }

            matchingOwner.Players.Add(player);
            Console.WriteLine(rosterEntry.RosterText());
        }

        foreach (var owner in leagueData.Owners)
        {
            owner.Score = owner.Players.Sum(p => p.Score);
        }

        return leagueData;
    }

    private async Task<Player> GetPlayerData(RosterEntry rosterEntry, int year, SeasonType seasonType)
    {
        var player = new Player()
        {
            Name = rosterEntry.Name,
            Link = string.IsNullOrEmpty(rosterEntry.Link) ? null : $"https://www.pro-football-reference.com/players/{rosterEntry.RosterLetter()}/{rosterEntry.Link}.htm",
            Position = rosterEntry.Position,
            FantasyOwner = rosterEntry.Owner
        };

        await GetPassingData(player, rosterEntry, year, seasonType).ConfigureAwait(false);
        await GatRushingAndReceivingData(player, rosterEntry, year, seasonType).ConfigureAwait(false);
        CalculateScore(player);

        return player;
    }

    private void CalculateScore(Player player)
    {
        decimal score = ((player.PassTd + player.RushTd + player.RecTd) * 6m) +
                        ((player.RushYd + player.RecYd) / 10m) +
                        (player.Rec * 1m) +
                        (player.PassYd / 25m);

        player.Score = Math.Round(score, 2);
    }

    private async Task GetPassingData(Player player, RosterEntry entry, int year, SeasonType seasonType)
    {
        if (string.IsNullOrEmpty(entry.Link)) { return; }

        var divValue = seasonType == SeasonType.Regular ? "div_passing" : "div_passing_playoffs";
        var url = $@"https://widgets.sports-reference.com/wg.fcgi?site=pfr&url=%2Fplayers%2F{entry.RosterLetter()}%2F{entry.Link}.htm&div={divValue}&cx={DateTime.UtcNow.ToString("o")}";

        var document = await GetHtmlDocument(url);

        if (document == null) { return; }

        var yearRow = document.DocumentNode.SelectSingleNode($"//table/tbody/tr[th/@csk='{year}']");

        if (yearRow == null) { return; }

        player.Age = GetByDataStat(yearRow, "age");
        player.G = GetByDataStat(yearRow, "g");
        player.PassTd = GetByDataStat(yearRow, "pass_td");
        player.PassYd = GetByDataStat(yearRow, "pass_yds");
    }

    private async Task GatRushingAndReceivingData(Player player, RosterEntry entry, int year, SeasonType seasonType)
    {
        if (string.IsNullOrEmpty(entry.Link)) { return; }

        var divValue = seasonType == SeasonType.Regular ? "div_rushing_and_receiving" : "div_rushing_and_receiving_playoffs";
        if (player.Position == PositionType.WR || player.Position == PositionType.TE)
        {
            divValue = seasonType == SeasonType.Regular ? "div_receiving_and_rushing" : "div_receiving_and_rushing_playoffs";
        }

        var url = $"https://widgets.sports-reference.com/wg.fcgi?site=pfr&url=%2Fplayers%2F{entry.RosterLetter()}%2F{entry.Link}.htm&div={divValue}&cx={DateTime.UtcNow.ToString("o")}";

        var document = await GetHtmlDocument(url);

        if (document == null) { return; }

        var yearRow = document.DocumentNode.SelectSingleNode($"//table/tbody/tr[th/@csk='{year}']");

        if (yearRow == null) { return; }

        player.Age = GetByDataStat(yearRow, "age");
        player.G = GetByDataStat(yearRow, "g");
        player.RushTd = GetByDataStat(yearRow, "rush_td");
        player.RushYd = GetByDataStat(yearRow, "rush_yds");
        player.Rec = GetByDataStat(yearRow, "rec");
        player.RecTd = GetByDataStat(yearRow, "rec_td");
        player.RecYd = GetByDataStat(yearRow, "rec_yds");
    }


    private int GetByDataStat(HtmlNode node, string stat)
    {
        var statText = node.SelectSingleNode($"td[@data-stat='{stat}']")?.InnerHtml ?? "";

        if (string.IsNullOrEmpty(statText)) {return 0;}

        statText = statText.Replace("<strong>", "")
                           .Replace("</strong>", "")
                           .Replace("<em>", "")
                           .Replace("</em>", "");

        var preDecimal = statText.Split('.').FirstOrDefault();
        var parsed = int.TryParse(preDecimal, out var parsedValue);
        return parsedValue;
    }

    private async Task<HtmlDocument?> GetHtmlDocument(string url)
    {
        var content = await _client.GetStringAsync(url);
        var startContent = content.IndexOf("<table");
        var endContent = content.IndexOf("</table>") + 8;

        if (startContent > 0)
        {
            var html = $"<html><body>{content.Substring(startContent, endContent - startContent)}</body></html>";
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            return doc;
        }
        return null;
    }
}
