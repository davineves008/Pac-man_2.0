using Game_X.Models.Entities;
using Game_X.Models.Enums;
using Game_X.Models.Map;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game_X.Models.Engine
{
    // Classe modelo da Fruta Bônus
    public class BonusFruit
    {
        public int X { get; set; } = 13; // Posição X central do labirinto
        public int Y { get; set; } = 17; // Posição Y (abaixo da casinha dos fantasmas)
        public bool Active { get; set; } = false;
        public int Type { get; set; } = 0; // 0: Cereja (100pt), 1: Morango (300pt), 2: Laranja (500pt)...
        public int Points => (Type + 1) * 200; // Pontuação dinâmica por tipo
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
        private bool _justSpawnedFruit = false; // Flag para ignorar a coleta no turno de nascimento

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
            CollectFruit();
            CheckGhostCollision();
            CheckVictory();
        }

        public void Update()
        {
            if (Status != GameStatus.Playing)
                return;

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
                ghost.Direction = direction; // Atualiza a direção para sincronizar com o visual do Canvas

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
            // Posição central abaixo do centro/casa dos fantasmas
            BonusFruit.X = 13;
            BonusFruit.Y = 17;

            // A cada gatilho de moedas, nasce uma fruta mais valiosa!
            if (_coinsCollectedCount >= 100)
                BonusFruit.Type = 3; // Maçã
            else if (_coinsCollectedCount >= 70)
                BonusFruit.Type = 2; // Laranja
            else if (_coinsCollectedCount >= 30)
                BonusFruit.Type = 1; // Morango
            else
                BonusFruit.Type = 0; // Cereja

            BonusFruit.Active = true;
            _justSpawnedFruit = true;
        }

        private void CollectFruit()
        {
            if (BonusFruit != null && BonusFruit.Active)
            {
                if (_justSpawnedFruit)
                {
                    _justSpawnedFruit = false;
                    return;
                }

                if (Player.X == BonusFruit.X && Player.Y == BonusFruit.Y)
                {
                    Player.Score += BonusFruit.Points;
                    BonusFruit.Active = false; // Desativa após comer
                }
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
                    ghost.Reset(); // Reposiciona imediatamente no spawn do fantasma
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
                        // Reseta o jogador e todos os fantasmas para os spawns iniciais
                        Player.X = Map.PlayerSpawn.X;
                        Player.Y = Map.PlayerSpawn.Y;

                        foreach (var g in Ghosts)
                        {
                            g.Reset();
                        }
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