using Game_X.Models.Engine;

namespace Game_X.Models.Session
{
    public class GameSession
    {
        public Guid Id { get; set; }

        public GameEngine Engine { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime LastUpdate { get; set; }
    }
}
