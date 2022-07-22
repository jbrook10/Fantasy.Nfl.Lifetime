namespace Fantasy.Nfl.Lifetime.Domain
{
    public class RosterEntry
    {
        public string? Owner { get; set; }
        public string? Name { get; set; }
        public PositionType Position { get; set; }
        public string? Link { get; set; }

        public RosterEntry(string rawEntry)
        {
            var parts = rawEntry.Split('|', System.StringSplitOptions.RemoveEmptyEntries);
            Owner = parts[0].Trim();
            Name = parts[1].Trim();

            if (Enum.TryParse(parts[2].Trim(), out PositionType position))
            {
                Position = position;
            }
            else
            {
                Position = PositionType.none;
            }


            Link = parts.Length > 3 ? parts[3].Trim() : string.Empty;
        }

        public string RosterText()
        {
            return $"{Owner} | {Name} | {PostitionText()} | {Link}";
        }

        public string PostitionText()
        {
            return Position.ToString();
        }

        public string? RosterLetter() {
            return string.IsNullOrEmpty(Link) ? null : Link[0].ToString();
        }

    }
}