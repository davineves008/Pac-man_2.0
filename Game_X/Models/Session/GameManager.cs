using Game_X.Models.Engine;

namespace Game_X.Models.Session
{
    public static class GameManager
    {
        private static readonly Dictionary<Guid, GameSession> Games = new();

        public static Guid CreateGame()
        {
            var engine = GameFactory.Create();

            var session = new GameSession
            {
                Id = Guid.NewGuid(),
                Engine = engine,
                CreatedAt = DateTime.Now,
                LastUpdate = DateTime.Now
            };

            Games.Add(session.Id, session);

            return session.Id;
        }

        public static GameSession Get(Guid id)
        {
            return Games[id];
        }

        public static bool Exists(Guid id)
        {
            return Games.ContainsKey(id);
        }

        public static void Remove(Guid id)
        {
            Games.Remove(id);
        }
    }
}
