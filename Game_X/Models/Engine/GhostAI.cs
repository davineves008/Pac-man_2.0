using Game_X.Models.Entities;
using Game_X.Models.Enums;
using Game_X.Models.Map;

namespace Game_X.Models.Engine
{
    public static class GhostAI
    {
        public static Direction GetNextDirection(Ghost ghost, Player player, GameMap map)
        {
            // A verdadeira abertura sem parede em cima fica no meio da casinha: X = 13!
            int doorX = 13;
            int doorY = 11;

            // 1. SISTEMA DE SAÍDA OBRIGATÓRIA DA CASINHA
            if (ghost.Y >= 12 || (ghost.Y >= doorY && !CanGhostMove(ghost.X, ghost.Y - 1, map)))
            {
                if (ghost.Y > 12 && CanGhostMove(ghost.X, ghost.Y - 1, map))
                {
                    return Direction.Up;
                }

                if (ghost.X < doorX && CanGhostMove(ghost.X + 1, ghost.Y, map))
                {
                    return Direction.Right;
                }

                if (ghost.X > doorX && CanGhostMove(ghost.X - 1, ghost.Y, map))
                {
                    return Direction.Left;
                }

                if (CanGhostMove(ghost.X, ghost.Y - 1, map))
                {
                    return Direction.Up;
                }
            }

            // 2. BUSCA DE CAMINHOS VÁLIDOS FORA DA CASINHA
            var possibleDirections = new List<Direction>();

            if (CanGhostMove(ghost.X, ghost.Y - 1, map) && ghost.Direction != Direction.Down)
                possibleDirections.Add(Direction.Up);

            if (CanGhostMove(ghost.X, ghost.Y + 1, map) && ghost.Direction != Direction.Up)
                possibleDirections.Add(Direction.Down);

            if (CanGhostMove(ghost.X - 1, ghost.Y, map) && ghost.Direction != Direction.Right)
                possibleDirections.Add(Direction.Left);

            if (CanGhostMove(ghost.X + 1, ghost.Y, map) && ghost.Direction != Direction.Left)
                possibleDirections.Add(Direction.Right);

            if (possibleDirections.Count == 0)
            {
                return GetOppositeDirection(ghost.Direction);
            }

            // 3. MODO ASSUSTADO (Aleatório)
            if (ghost.State == GhostState.Frightened)
            {
                int index = Random.Shared.Next(possibleDirections.Count);
                return possibleDirections[index];
            }

            // 4. MODO PERSEGUIÇÃO VS MODO DISPERSÃO (SCATTER)
            // Ciclo de 12 segundos: 7s perseguindo e 5s dispersando para o canto do mapa
            int currentSecond = DateTime.Now.Second % 12;
            bool isScatterTime = currentSecond >= 7;

            int targetX;
            int targetY;

            if (isScatterTime)
            {
                // No modo dispersão, o fantasma foca no canto superior esquerdo (0,0)
                // Se quiser alterar o tempo de dispersão, basta mudar a condição 'currentSecond >= 7'
                targetX = 0;
                targetY = 0;
            }
            else
            {
                // No modo perseguição, foca na posição atual do jogador
                targetX = player.X;
                targetY = player.Y;
            }

            return possibleDirections
                .OrderBy(dir => GetDistanceAfterMove(ghost.X, ghost.Y, dir, targetX, targetY))
                .First();
        }

        private static bool CanGhostMove(int x, int y, GameMap map)
        {
            if (x < 0 || x >= map.Width || y < 0 || y >= map.Height)
                return true;

            return map.Tiles[x, y].Type != TileType.Wall;
        }

        private static double GetDistanceAfterMove(int x, int y, Direction dir, int targetX, int targetY)
        {
            switch (dir)
            {
                case Direction.Up: y--; break;
                case Direction.Down: y++; break;
                case Direction.Left: x--; break;
                case Direction.Right: x++; break;
            }

            return Math.Pow(x - targetX, 2) + Math.Pow(y - targetY, 2);
        }

        private static Direction GetOppositeDirection(Direction dir)
        {
            return dir switch
            {
                Direction.Up => Direction.Down,
                Direction.Down => Direction.Up,
                Direction.Left => Direction.Right,
                Direction.Right => Direction.Left,
                _ => Direction.Up
            };
        }
    }
}