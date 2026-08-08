using Game_X.Models.Entities;
using Game_X.Models.Enums;
using Game_X.Models.Map.PacManMVC.Models.Map;
using System.Collections.Generic;

namespace Game_X.Models.Engine
{
    public static class GameFactory
    {
        public static GameEngine Create()
        {
            var map = MapLoader.Load();

            var player = new Player
            {
                X = map.PlayerSpawn.X,
                Y = map.PlayerSpawn.Y,
                Direction = Direction.Left
            };

            var ghosts = new List<Ghost>();

            // Perfis com cores em Hexadecimal prontas para o Canvas/Front-end
            var ghostProfiles = new[]
            {
                new { Name = "Blinky", Color = "#FF0000" }, // Vermelho
                new { Name = "Pinky",  Color = "#FFB8FF" }, // Rosa
                new { Name = "Inky",   Color = "#00FFFF" }, // Ciano
                new { Name = "Clyde",  Color = "#FFB852" }, // Laranja
                new { Name = "Shadow", Color = "#A020F0" }, // Roxo
                new { Name = "Speedy", Color = "#00FF00" }, // Verde
                new { Name = "Spike",  Color = "#FFFF00" }, // Amarelo
                new { Name = "Casper", Color = "#FFFFFF" }  // Branco
            };

            // Instancia dinamicamente um fantasma para cada spawn no mapa
            for (int i = 0; i < map.GhostSpawns.Count; i++)
            {
                var spawn = map.GhostSpawns[i];
                var profile = ghostProfiles[i % ghostProfiles.Length];

                ghosts.Add(new Ghost
                {
                    Name = profile.Name,
                    Color = profile.Color,
                    X = spawn.X,
                    Y = spawn.Y,
                    SpawnX = spawn.X,
                    SpawnY = spawn.Y,
                    Direction = Direction.Up, // Inicia apontado para cima para facilitar a saída
                    State = GhostState.Normal
                });
            }

            return new GameEngine(player, ghosts, map);
        }
    }
}