namespace Fantasy.Nfl.Lifetime.Domain;
public class LeagueData
{

    public int Year { get; set; }
    public SeasonType SeasonType { get; set; }
    public string? LastUpdated { get; set; }
    public List<Owner> Owners { get; set; } = new List<Owner>();

    public LeagueData(int year, SeasonType type)
    {
        LastUpdated = DateTime.UtcNow.ToString("O");
        Year = year;
        SeasonType = type;
        Owners = new List<Owner>();
    }

}


public class Owner
{
    public string? Name { get; set; }

    public decimal Score { get; set; }
    public List<Player> Players { get; set; } = new List<Player>();
}

public class Player
{
    public string? FantasyOwner { get; set; }
    public string? Name { get; set; }

    public PositionType Position { get; set; }
    public string? Link { get; set; }

    public int Age { get; set; }
    public int G { get; set; }
    public int PassYd { get; set; }
    public int PassTd { get; set; }
    public int RushYd { get; set; }
    public int RushTd { get; set; }
    public int Rec { get; set; }
    public int RecYd { get; set; }
    public int RecTd { get; set; }

    public decimal Score { get; set; }
}


public enum PositionType
{
    none = 0,
    QB = 1,
    WR = 2,
    RB = 3,
    TE = 4
}

public enum SeasonType
{
    Regular = 0,
    Post = 1
}

