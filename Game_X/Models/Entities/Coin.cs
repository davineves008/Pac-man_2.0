namespace Game_X.Models.Entities
{
    public class Coin
    {
        public int X { get; set; }

        public int Y { get; set; }

        public bool Collected { get; set; }

        public int Points { get; set; } = 10;
    }
}
