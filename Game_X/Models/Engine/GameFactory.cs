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

            // Lista estendida de perfis para novos fantasmas
            var ghostProfiles = new[]
            {
                new { Name = "Blinky", Color = "Red" },
                new { Name = "Pinky",  Color = "Pink" },
                new { Name = "Inky",   Color = "Cyan" },
                new { Name = "Clyde",  Color = "Orange" },
                new { Name = "Shadow", Color = "Purple" },
                new { Name = "Speedy", Color = "Green" },
                new { Name = "Spike",  Color = "Yellow" },
                new { Name = "Casper", Color = "White" }
            };

            // Loop dinamico: cria um fantasma para cada 'G' do mapa
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
                    Direction = Direction.Left,
                    State = GhostState.Normal
                });
            }

            return new GameEngine(player, ghosts, map);
        }
    }
}