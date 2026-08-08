using Game_X.Models.Entities;
using Game_X.Models.Enums;
using Game_X.Models.Map;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game_X.Models.Engine
{
    public class BonusFruit
    {
        public int X { get; set; } = 13;
        public int Y { get; set; } = 17;
        public bool Active { get; set; } = false;
        public int Type { get; set; } = 0;
        public DateTime? ExpiresAt { get; set; } // Adicionado controle de tempo

        public int Points => Type switch
        {
            0 => 100, // Cereja
            1 => 300, // Morango
            2 => 500, // Laranja
            3 => 700, // Maçã
            _ => 1000
        };
    }

    public class GameEngine
    {
        public Player Player { get; set; }
        public List<Ghost> Ghosts { get; set; }
        public GameMap Map { get; set; }
        public GameStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public BonusFruit BonusFruit { get; set; }

        private int _coinsCollectedCount = 0;
        private int _ghostsEatenInPowerMode = 0; // Combo de pontos para fantasmas

        public GameEngine(Player player, List<Ghost> ghosts, GameMap map)
        {
            Player = player;
            Ghosts = ghosts;
            Map = map;
            BonusFruit = new BonusFruit();

            StartedAt = DateTime.Now;
            Status = GameStatus.Playing;
        }

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

            // Desativa a fruta bônus se o tempo expirar (10 segundos)
            if (BonusFruit.Active && BonusFruit.ExpiresAt.HasValue && DateTime.Now >= BonusFruit.ExpiresAt.Value)
            {
                BonusFruit.Active = false;
            }

            // Checa expiração do Power Pellet
            if (Player.Powered && DateTime.Now >= Player.PowerUntil)
            {
                Player.Powered = false;
                _ghostsEatenInPowerMode = 0; // Reseta o combo

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
                ghost.Direction = direction;

                switch (direction)
                {
                    case Direction.Up: ghost.Y--; break;
                    case Direction.Down: ghost.Y++; break;
                    case Direction.Left: ghost.X--; break;
                    case Direction.Right: ghost.X++; break;
                }

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

            if (_coinsCollectedCount == 30 || _coinsCollectedCount == 100)
            {
                SpawnBonusFruit();
            }
        }

        public void SpawnBonusFruit()
        {
            BonusFruit.X = 13;
            BonusFruit.Y = 8;

            if (_coinsCollectedCount >= 100)
                BonusFruit.Type = 3;
            else if (_coinsCollectedCount >= 70)
                BonusFruit.Type = 2;
            else if (_coinsCollectedCount >= 30)
                BonusFruit.Type = 1;
            else
                BonusFruit.Type = 0;

            BonusFruit.Active = true;
            BonusFruit.ExpiresAt = DateTime.Now.AddSeconds(10); // Fruta dura 10 segundos no mapa
        }

        private void CollectFruit()
        {
            if (BonusFruit != null && BonusFruit.Active)
            {
                if (Player.X == BonusFruit.X && Player.Y == BonusFruit.Y)
                {
                    Player.Score += BonusFruit.Points;
                    BonusFruit.Active = false;
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
            _ghostsEatenInPowerMode = 0; // Reseta combo para a nova fruta de poder

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
                    ghost.Reset();

                    // Multiplicador de combo: 200, 400, 800, 1600...
                    _ghostsEatenInPowerMode++;
                    int bonusPoints = (int)Math.Pow(2, _ghostsEatenInPowerMode) * 100;
                    Player.Score += bonusPoints;

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

                        foreach (var g in Ghosts)
                        {
                            g.Reset();
                        }
                    }

                    // Interrompe o loop após atingir o jogador para evitar danos múltiplos
                    break;
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