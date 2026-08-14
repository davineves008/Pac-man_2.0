using System.Collections.Generic;
using Game_X.Models.Entities;
using Game_X.Models.Map;

namespace Game_X.Models.Map.PacManMVC.Models.Map
{
    public static class MapLoader
    {
        /// <summary>
        /// Carrega um mapa com base no ID/número informado.
        /// </summary>
        /// <param name="mapId">Número do mapa desejado (padrão: 1)</param>
        public static GameMap Load(int mapId = 1)
        {
            string[] mapData = GetMapData(mapId);

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

        /// <summary>
        /// Seleciona o layout das strings do mapa conforme o ID informado.
        /// </summary>
        private static string[] GetMapData(int mapId)
        {
            return mapId switch
            {
                // Mapa 1: Clássico (o seu mapa original)
                1 => new string[]
                {
                    "111111111111111111111111111",
                    "122222222212222212222222221",
                    "131111211212111212112111131",
                    "121111211212111212112111121",
                    "122222222222222222222222221",
                    "121111211112222211121111221",
                    "022222222100000021122222220",
                    "1111112121G11111G1112212111",
                    "1111112121G00000G1112212111",
                    "122222212111111111111222221",
                    "1211112222222P2222222211121",
                    "032222222222211222221222230",
                    "111111111111111111111111111"
                },

                // Mapa 2: Compacto / Rápido
                2 => new string[]
                {
                    "111111111111111111111111111",
                    "13222222221111122222222231",
                    "1211112112222222112111121",
                    "1211112111121211112111121",
                    "02222222221G0G12222222220",
                    "1211112112111112112111121",
                    "122222211222P222112222221",
                    "111111111111111111111111111"
                },

                // Mapa 3: Desafio / Arena Aberta
                3 => new string[]
                {
                    "111111111111111111111111111",
                    "13222222222111222222222231",
                    "1211111111211121111111121",
                    "1211222211222221122221121",
                    "02222112222G1G22221122220",
                    "1211211211110111121121121",
                    "121122221122P221122221121",
                    "1211111111211121111111121",
                    "13222222222222222222222231",
                    "111111111111111111111111111"
                },

                // Caso passe um ID inexistente, carrega o Mapa 1 como padrão
                _ => GetMapData(1)
            };
        }
    }
}