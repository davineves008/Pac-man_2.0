using Game_X.Models.Entities;

namespace Game_X.Models.Map
{
    public class GameMap
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public Tile[,] Tiles { get; set; }

        public List<Coin> Coins { get; set; } = new();

        public List<PowerPellet> PowerPellets { get; set; } = new();

        public (int X, int Y) PlayerSpawn { get; set; }

        public List<(int X, int Y)> GhostSpawns { get; set; } = new();
    }
}
