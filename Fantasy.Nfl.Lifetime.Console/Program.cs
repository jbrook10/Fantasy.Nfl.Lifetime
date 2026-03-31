// See https://aka.ms/new-console-template for more information

using Fantasy.Nfl.Lifetime.Business;
using Fantasy.Nfl.Lifetime.Domain;
using System;

namespace Fantasy.Nfl.Lifetime.Console;

class Program
{
    static async Task Main(string[] args)
    {
        // args = new string[] {"2025", "0"};

        var season = Convert.ToInt32(args[0]);
        var seasonType = (SeasonType)Enum.ToObject(typeof(SeasonType), Convert.ToInt32(args[1]));

        var playerReader = new PlayerReader();
        await playerReader.CreateLeagueData(season, seasonType);

        // Program2.Start();
    }
}
 