namespace Game_X.Models.Entities
{
    public class PowerPellet
    {
        public int X { get; set; }

        public int Y { get; set; }

        public bool Collected { get; set; }

        public int DurationSeconds { get; set; } = 10;
    }
}
