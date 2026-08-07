using Game_X.Models.Enums;

namespace Game_X.Models.Entities
{
    public class Ghost
    {
        public string Name { get; set; } = "";

        public string Color { get; set; } = "";

        public int X { get; set; }

        public int Y { get; set; }

        public int SpawnX { get; set; }

        public int SpawnY { get; set; }

        public Direction Direction { get; set; }

        public GhostState State { get; set; }

        public bool IsFrightened { get; set; } = false; // Novo: Indica se está vulnerável

        public void Reset()
        {
            X = SpawnX;
            Y = SpawnY;
            State = GhostState.Normal;
        }
    }
}
