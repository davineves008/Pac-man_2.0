using Game_X.Models.Enums;

namespace Game_X.Models.Entities
{
    public class Player
    {
        public int X { get; set; }

        public int Y { get; set; }

        public Direction Direction { get; set; }

        public int Score { get; set; }

        public int Lives { get; set; } = 3;

        public bool IsAlive => Lives > 0;

        public bool Powered { get; set; }

        public DateTime PowerUntil { get; set; }
    }
}
