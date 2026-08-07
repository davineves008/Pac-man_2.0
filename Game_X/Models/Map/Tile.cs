namespace Game_X.Models.Map
{
    public class Tile
    {
        public int X { get; set; }

        public int Y { get; set; }

        public TileType Type { get; set; }

        public bool Visited { get; set; }
    }
}
