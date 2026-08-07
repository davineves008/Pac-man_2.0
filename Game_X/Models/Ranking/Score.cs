namespace Game_X.Models.Ranking
{
    public class Score
    {
        public int Id { get; set; }

        public string PlayerName { get; set; }

        public int Points { get; set; }

        public TimeSpan TimePlayed { get; set; }

        public DateTime Date { get; set; }
    }
}
