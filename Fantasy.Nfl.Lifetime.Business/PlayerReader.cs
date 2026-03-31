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
            Console.WriteLine(rosterEntry.RosterText() + " - " + player.Score);
            System.Threading.Thread.Sleep(5000);
        }

        foreach (var owner in leagueData.Owners)
        {
            owner.Score = owner.Players.Sum(p => p.Score);
            var sortedPlayers = owner.Players.OrderByDescending(p => p.Score);
            owner.CountingScore = sortedPlayers.Where(p => p.Position == PositionType.QB).Take(1).Sum(p => p.Score) +
                                   sortedPlayers.Where(p => p.Position == PositionType.RB).Take(2).Sum(p => p.Score) +
                                   sortedPlayers.Where(p => p.Position == PositionType.WR).Take(3).Sum(p => p.Score) +
                                   sortedPlayers.Where(p => p.Position == PositionType.TE).Take(1).Sum(p => p.Score);
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

        var divValue = seasonType == SeasonType.Regular ? "div_passing" : "div_passing_post";
        var idValue = seasonType == SeasonType.Regular ? "passing" : "passing_post";

        //var url = $@"https://widgets.sports-reference.com/wg.fcgi?site=pfr&url=%2Fplayers%2F{entry.RosterLetter()}%2F{entry.Link}.htm&div={divValue}&cx={DateTime.UtcNow.ToString("o")}";
        var url = $@"https://www.pro-football-reference.com/players/{entry.RosterLetter()}/{entry.Link}.htm";
        // var document = await GetHtmlDocument(url);

        var document = await GetStringDocument(url, divValue);

        if (document == null) { return; }

        var yearRow = document.DocumentNode.SelectSingleNode($"//table/tbody/tr[@id='{idValue}.{year}']");

        if (yearRow == null) { return; }

        player.Age = GetByDataStat(yearRow, "age");
        player.G = GetByDataStat(yearRow, "g");
        player.PassTd = GetByDataStat(yearRow, "pass_td");
        player.PassYd = GetByDataStat(yearRow, "pass_yds");
    }

    private async Task GatRushingAndReceivingData(Player player, RosterEntry entry, int year, SeasonType seasonType)
    {
        if (string.IsNullOrEmpty(entry.Link)) { return; }

        var divValue = seasonType == SeasonType.Regular ? "div_rushing_and_receiving" : "div_rushing_and_receiving_post";
        var idValue = seasonType == SeasonType.Regular ? "rushing_and_receiving" : "rushing_and_receiving_post";
        if (player.Position == PositionType.WR || player.Position == PositionType.TE)
        {
            divValue = seasonType == SeasonType.Regular ? "div_receiving_and_rushing" : "div_receiving_and_rushing_post";
            idValue = seasonType == SeasonType.Regular ? "receiving_and_rushing" :"receiving_and_rushing_post";
        }

        // var url = $"https://widgets.sports-reference.com/wg.fcgi?site=pfr&url=%2Fplayers%2F{entry.RosterLetter()}%2F{entry.Link}.htm&div={divValue}&cx={DateTime.UtcNow.ToString("o")}";
        var url = $@"https://www.pro-football-reference.com/players/{entry.RosterLetter()}/{entry.Link}.htm";

        var document = await GetStringDocument(url, divValue);

        if (document == null) { return; }

        var yearRow = document.DocumentNode.SelectSingleNode($"//table/tbody/tr[@id='{idValue}.{year}']");

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

        if (string.IsNullOrEmpty(statText)) { return 0; }

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

    private async Task<HtmlDocument?> GetStringDocument(string url, string divName)
    {
        try
        {
            var content = await _client.GetStringAsync(url);

            var divNameIdx = content.IndexOf(divName);

            if (divNameIdx < 0)
            {
                return null;
            }

            var startTableIndex = content.IndexOf("<table", divNameIdx);
            var endTableIndex = content.IndexOf("</table>", startTableIndex) + 8;

            if (startTableIndex > 0)
            {
                var html = $"<html><body>{content.Substring(startTableIndex, endTableIndex - startTableIndex)}</body></html>";
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                return doc;
            }
            return null;
        }
        catch (System.Exception)
        {
            Console.WriteLine($"Error: {url}");
            return null;
        }
    }
}

public class Program2
    {
        static int[] queens;

        public static void Start()
        {
            // Initialize queens array
            queens = new int[8];

            // Place the first queen on the first row
            PlaceQueen(0);

            // Print the final positions of the queens
             Console.WriteLine("8 queens solution:");
            for (int i = 0; i < 8; i++)
            {
                 Console.WriteLine("Queen " + (i + 1) + ": (" + (i + 1) + ", " + (queens[i] + 1) + ")");
            }
        }

        static bool PlaceQueen(int row)
        {
            // Try placing the queen on each column of the current row
            for (int col = 0; col < 8; col++)
            {
                // Check if the position is safe
                if (IsSafe(row, col))
                {
                    // Place the queen on the current position
                    queens[row] = col;

                    // Move to the next row
                    if (row == 7)
                    {
                        // We've reached the last row, so the solution is complete
                        return true;
                    }
                    else
                    {
                        // Place the queen on the next row
                        if (PlaceQueen(row + 1))
                        {
                            // The queen was successfully placed on the next row, so return true
                            return true;
                        }
                    }
                }
            }

            // We couldn't find a safe position for the queen on the current row, so return false
            return false;
        }

        static bool IsSafe(int row, int col)
        {
            // Check if the queen is attacked by any of the previously placed queens
            for (int i = 0; i < row; i++)
            {
                // Check if the queen is on the same column or diagonal
                if (queens[i] == col || Math.Abs(queens[i] - col) == row - i)
                {
                    // The queen is attacked, so return false
                    return false;
                }
            }

            // The queen is not attacked, so return true
            return true;
        }
    }
