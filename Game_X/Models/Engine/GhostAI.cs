using Game_X.Models.Entities;
using Game_X.Models.Enums;
using Game_X.Models.Map;

namespace Game_X.Models.Engine
{
    public static class GhostAI
    {
        private static readonly Random Rand = new();

        public static Direction GetNextDirection(
            Ghost ghost,
            Player player,
            GameMap map)
        {
            var possibleMoves = new List<(Direction Dir, int NextX, int NextY)>
            {
                (Direction.Up,    ghost.X,     ghost.Y - 1),
                (Direction.Down,  ghost.X,     ghost.Y + 1),
                (Direction.Left,  ghost.X - 1, ghost.Y),
                (Direction.Right, ghost.X + 1, ghost.Y)
            };

            // 1. Filtra movimentos válidos sem parede
            var validMoves = possibleMoves
                .Where(m => CanMove(m.NextX, m.NextY, map))
                .ToList();

            if (!validMoves.Any())
                return Direction.None;

            int houseCenterX = map.Width / 2;
            int houseDoorY = (map.Height / 2) - 2;

            // Se ainda está na casinha, prioridade é sair
            bool isInsideHouse = ghost.Y >= (map.Height / 2) - 1 && Math.Abs(ghost.X - houseCenterX) <= 3;

            (int targetX, int targetY) target;

            if (isInsideHouse)
            {
                target = (houseCenterX, houseDoorY);
            }
            else
            {
                // Já saiu: Bloqueia voltar para a casinha
                validMoves = validMoves
                    .Where(m => !(m.NextX == houseCenterX && m.NextY == (map.Height / 2)))
                    .ToList();

                if (!validMoves.Any())
                    validMoves = possibleMoves.Where(m => CanMove(m.NextX, m.NextY, map)).ToList();

                target = GetTargetTile(ghost, player, map);
            }

            // Se está na mesma posição do Pac-Man, continua avançando
            if (ghost.X == player.X && ghost.Y == player.Y)
            {
                return validMoves[Rand.Next(validMoves.Count)].Dir;
            }

            // 2. Encontra a menor distância até o alvo
            double shortestDistance = double.MaxValue;
            var bestMoves = new List<Direction>();

            foreach (var move in validMoves)
            {
                double distance = Math.Sqrt(Math.Pow(move.NextX - target.targetX, 2) + Math.Pow(move.NextY - target.targetY, 2));

                // Se achar um caminho significativamente mais curto, atualiza o melhor
                if (distance < shortestDistance - 0.001)
                {
                    shortestDistance = distance;
                    bestMoves.Clear();
                    bestMoves.Add(move.Dir);
                }
                // Se a distância for praticamente idêntica (empate), adiciona como opção
                else if (Math.Abs(distance - shortestDistance) < 0.001)
                {
                    bestMoves.Add(move.Dir);
                }
            }

            // 3. SE HOUVER EMPATE nas distâncias, sorteia um dos caminhos!
            // Isso evita que fantasmas no mesmo ponto tomem exatamente a mesma decisão.
            return bestMoves[Rand.Next(bestMoves.Count)];
        }

        private static (int TargetX, int TargetY) GetTargetTile(Ghost ghost, Player player, GameMap map)
        {
            string name = ghost.Name?.ToLower() ?? "";

            // 🔴 Blinky (Vermelho): Mira exatamente NO Pac-Man
            if (name.Contains("red") || name.Contains("blinky"))
            {
                return (player.X, player.Y);
            }

            // 🔵 Inky (Ciano): Mira 2 blocos ATRÁS do Pac-Man (ou em um ângulo oposto ao Vermelho)
            // Isso faz ele flanquear em vez de ir junto com o Vermelho!
            if (name.Contains("cyan") || name.Contains("blue") || name.Contains("inky"))
            {
                return (player.X - 2, player.Y - 2);
            }

            // 🩷 Pinky (Rosa): Mira 2 blocos À FRENTE do Pac-Man
            if (name.Contains("pink") || name.Contains("pinky"))
            {
                return (player.X + 2, player.Y + 2);
            }

            // 🟠 Clyde (Laranja): Persegue de longe, foge para o canto de perto
            if (name.Contains("orange") || name.Contains("clyde"))
            {
                double dist = Math.Sqrt(Math.Pow(ghost.X - player.X, 2) + Math.Pow(ghost.Y - player.Y, 2));
                return dist > 5 ? (player.X, player.Y) : (0, map.Height);
            }

            // Padrão de segurança: Deslocamento leve para evitar sobreposição
            return (player.X, player.Y);
        }

        private static bool CanMove(int x, int y, GameMap map)
        {
            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                return false;

            return map.Tiles[x, y].Type != TileType.Wall;
        }
    }
}