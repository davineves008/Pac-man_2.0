using Game_X.Models.Entities;
using Game_X.Models.Enums;
using Game_X.Models.Map;

namespace Game_X.Models.Engine
{
    // Classe modelo da Fruta Bônus
    public class BonusFruit
    {
        public int X { get; set; } = 13; // Posição X central do labirinto
        public int Y { get; set; } = 17; // Posição Y (abaixo da casinha dos fantasmas)
        public bool Active { get; set; } = false;
        public int Type { get; set; } = 0; // 0: Cereja (100pt), 1: Morango (300pt), 2: Laranja (500pt)...
        public int Points => (Type + 1) * 200; // Exemplo de pontuação dinâmica por tipo
    }

    public class GameEngine
    {
        public Player Player { get; set; }
        public List<Ghost> Ghosts { get; set; }
        public GameMap Map { get; set; }
        public GameStatus Status { get; set; }
        public DateTime StartedAt { get; set; }

        // Propriedade para controlar a Fruta Bônus na sessão
        public BonusFruit BonusFruit { get; set; }

        private int _coinsCollectedCount = 0;

        public GameEngine(Player player, List<Ghost> ghosts, GameMap map)
        {
            Player = player;
            Ghosts = ghosts;
            Map = map;
            BonusFruit = new BonusFruit();

            StartedAt = DateTime.Now;
            Status = GameStatus.Playing;
        }

        // --- MÉTODOS PÚBLICOS ---

        public void MovePlayer(Direction direction)
        {
            if (Status != GameStatus.Playing)
                return;

            int newX = Player.X;
            int newY = Player.Y;

            switch (direction)
            {
                case Direction.Up: newY--; break;
                case Direction.Down: newY++; break;
                case Direction.Left: newX--; break;
                case Direction.Right: newX++; break;
            }

            if (!CanMove(newX, newY))
                return;

            // Lógica do Túnel do Jogador
            if (newX < 0)
            {
                newX = Map.Width - 1;
            }
            else if (newX >= Map.Width)
            {
                newX = 0;
            }

            Player.X = newX;
            Player.Y = newY;
            Player.Direction = direction;

            // Checagens de Coleta e Colisão
            CollectCoin();
            CollectPowerPellet();
            CollectFruit(); // <-- Nova checagem da fruta
            CheckGhostCollision();
            CheckVictory();
        }

        public void Update()
        {
            // Checa se o tempo do Modo Poderoso expirou
            if (Player.Powered && DateTime.Now >= Player.PowerUntil)
            {
                Player.Powered = false;

                foreach (var ghost in Ghosts)
                {
                    if (ghost.State == GhostState.Frightened)
                        ghost.State = GhostState.Normal;
                }
            }

            MoveGhosts();
        }

        public void MoveGhosts()
        {
            foreach (var ghost in Ghosts)
            {
                if (ghost.State == GhostState.Dead)
                {
                    ghost.Reset();
                    continue;
                }

                var direction = GhostAI.GetNextDirection(ghost, Player, Map);

                switch (direction)
                {
                    case Direction.Up: ghost.Y--; break;
                    case Direction.Down: ghost.Y++; break;
                    case Direction.Left: ghost.X--; break;
                    case Direction.Right: ghost.X++; break;
                }

                // Lógica do Túnel dos Fantasmas
                if (ghost.X < 0)
                {
                    ghost.X = Map.Width - 1;
                }
                else if (ghost.X >= Map.Width)
                {
                    ghost.X = 0;
                }
            }

            CheckGhostCollision();
        }

        // --- MÉTODOS PRIVADOS ---

        private bool CanMove(int x, int y)
        {
            if (x < 0 || x >= Map.Width)
            {
                return y >= 0 && y < Map.Height;
            }

            if (y < 0 || y >= Map.Height) return false;

            return Map.Tiles[x, y].Type != TileType.Wall;
        }

        private void CollectCoin()
        {
            var coin = Map.Coins.FirstOrDefault(c =>
                !c.Collected &&
                c.X == Player.X &&
                c.Y == Player.Y);

            if (coin == null)
                return;

            coin.Collected = true;
            Player.Score += coin.Points;

            _coinsCollectedCount++;

            // Faz surgir a fruta bônus ao comer 30 ou 100 moedas
            if (_coinsCollectedCount == 30 || _coinsCollectedCount == 100)
            {
                SpawnBonusFruit();
            }
        }

        private void SpawnBonusFruit()
        {
            // 1. Define as coordenadas para o Spawn do Jogador
            BonusFruit.X = Map.PlayerSpawn.X;
            BonusFruit.Y = Map.PlayerSpawn.Y;

            // 2. Define o tipo (Cereja = 0, Morango = 1, etc.)
            BonusFruit.Type = (_coinsCollectedCount >= 100) ? 1 : 0;

            // 3. Ativa a fruta
            BonusFruit.Active = true;
        }
        private void CollectFruit()
        {
            if (BonusFruit.Active && Player.X == BonusFruit.X && Player.Y == BonusFruit.Y)
            {
                Player.Score += BonusFruit.Points;
                BonusFruit.Active = false; // Desativa a fruta após ser comida
            }
        }

        private void CollectPowerPellet()
        {
            var pellet = Map.PowerPellets.FirstOrDefault(p =>
                !p.Collected &&
                p.X == Player.X &&
                p.Y == Player.Y);

            if (pellet == null)
                return;

            pellet.Collected = true;
            Player.Powered = true;
            Player.PowerUntil = DateTime.Now.AddSeconds(pellet.DurationSeconds);

            foreach (var ghost in Ghosts)
            {
                if (ghost.State != GhostState.Dead)
                    ghost.State = GhostState.Frightened;
            }
        }

        private void CheckGhostCollision()
        {
            foreach (var ghost in Ghosts)
            {
                if (ghost.X != Player.X || ghost.Y != Player.Y)
                    continue;

                if (ghost.State == GhostState.Frightened)
                {
                    ghost.State = GhostState.Dead;
                    Player.Score += 200;
                    continue;
                }

                if (ghost.State == GhostState.Normal)
                {
                    Player.Lives--;

                    if (!Player.IsAlive)
                    {
                        Status = GameStatus.GameOver;
                    }
                    else
                    {
                        Player.X = Map.PlayerSpawn.X;
                        Player.Y = Map.PlayerSpawn.Y;
                    }
                }
            }
        }

        private void CheckVictory()
        {
            if (Map.Coins.All(c => c.Collected))
            {
                Status = GameStatus.Victory;
            }
        }
    }
}