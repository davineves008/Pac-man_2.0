using System.Collections.Generic;
using Game_X.Models.Entities;
using Game_X.Models.Map;

namespace Game_X.Models.Map.PacManMVC.Models.Map
{
    public static class MapLoader
    {
        public static GameMap Load()
        {
            // Legenda do Mapa:
            // 0 = Espaço Vazio, 1 = Parede, 2 = Moeda, 3 = Power Pellet
            // P = Spawn do Jogador, G = Spawn de Fantasma
            // Repare nas últimas posições de cada linha (canto inferior direito):
            string[] mapData = new string[]
        {
    "111111111111111111111111111", // Linha 0
    "122222222212222212222222221", // Linha 1
    "131111211212111212112111131", // Linha 2
    "121111211212111212112111121", // Linha 3
    "122222222222222222222222221", // Linha 4
    "121111211112222211121111221", // Linha 5
    "022222222100000021122222220", // Linha 6 - TÚNEL NAS PONTAS ('0' na borda)
    "1111112121G11111G1112212111", // Linha 7
    "1111112121G00000G1112212111", // Linha 8
    "122222212111111111111222221", // Linha 9
    "1211112222222P2222222211121", // Linha 10
    "032222222222211222221222230",   // Linha 11 - Simétrico e ajustado!
    "111111111111111111111111111"  // Linha 12
        };

            int height = mapData.Length;
            int width = mapData[0].Length;

            var map = new GameMap
            {
                Width = width,
                Height = height,
                Tiles = new Tile[width, height],
                Coins = new List<Coin>(),
                PowerPellets = new List<PowerPellet>(),
                GhostSpawns = new List<(int X, int Y)>()
            };

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Trata caracteres extras/faltantes com segurança
                    char tileChar = (x < mapData[y].Length) ? mapData[y][x] : '0';

                    TileType type = tileChar switch
                    {
                        '1' => TileType.Wall,
                        '2' => TileType.Coin,
                        '3' => TileType.PowerPellet,
                        'P' => TileType.PlayerSpawn,
                        'G' => TileType.GhostSpawn,
                        _ => TileType.Empty
                    };

                    map.Tiles[x, y] = new Tile
                    {
                        X = x,
                        Y = y,
                        Type = type,
                        Visited = false
                    };

                    switch (tileChar)
                    {
                        case '2':
                            map.Coins.Add(new Coin { X = x, Y = y, Collected = false });
                            break;
                        case '3':
                            map.PowerPellets.Add(new PowerPellet { X = x, Y = y, Collected = false });
                            break;
                        case 'P':
                            map.PlayerSpawn = (x, y);
                            break;
                        case 'G':
                            map.GhostSpawns.Add((x, y));
                            break;
                    }
                }
            }

            return map;
        }
    }
}