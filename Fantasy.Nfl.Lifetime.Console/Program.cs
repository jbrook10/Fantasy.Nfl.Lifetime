// See https://aka.ms/new-console-template for more information

using Fantasy.Nfl.Lifetime.Business;

namespace Fantasy.Nfl.Lifetime.Console;

class Program
{
    static async Task Main(string[] args)
    {
        var playerReader = new PlayerReader();
        await playerReader.CreateLeagueData(2021, Domain.SeasonType.Regular);
    }
}
