using Game_X.Models.DTO;
using Game_X.Models.Enums;
using Game_X.Models.Session;
using Microsoft.AspNetCore.Mvc;

namespace Game_X.Controllers
{
    public class GameController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }



        //cria uma partida
        public IActionResult NewGame()
        {
            Guid gameId = GameManager.CreateGame();

            HttpContext.Session.SetString(
                "GameId",
                gameId.ToString());

            return RedirectToAction("Index");
        }

        //recupera a partida;
        private GameSession CurrentGame()
        {
            var id = HttpContext.Session.GetString("GameId");

            if (string.IsNullOrEmpty(id))
                return null;

            return GameManager.Get(Guid.Parse(id));
        }


        //recebe os comandos do teclado;
        [HttpPost]
        [HttpPost]
        public IActionResult Move([FromBody] MoveRequest request)
        {
            var game = CurrentGame();

            if (game == null || request == null)
                return BadRequest();

            if (Enum.TryParse<Direction>(request.Direction, true, out var dir))
            {
                // Chama a função correta que atualiza X/Y e valida colisão com paredes!
                game.Engine.MovePlayer(dir);
            }

            return Json(new { sucesso = true, player = game.Engine.Player });
        }

        public class MoveRequest
        {
            public string Direction { get; set; }
        }

        //cria um endpoint, desenha o mapa e os personagens;

        [HttpGet]
        public IActionResult State()
        {
            var game = CurrentGame();

            if (game == null)
                return BadRequest();

            // Executa a IA dos fantasmas e atualiza o estado do jogo a cada requisição
            game.Engine.Update();

            var tiles = new List<object>();

            for (int y = 0; y < game.Engine.Map.Height; y++)
            {
                for (int x = 0; x < game.Engine.Map.Width; x++)
                {
                    var tile = game.Engine.Map.Tiles[x, y];

                    tiles.Add(new
                    {
                        x = tile.X,
                        y = tile.Y,
                        type = tile.Type
                    });
                }
            }

            return Json(new
            {
                width = game.Engine.Map.Width,
                height = game.Engine.Map.Height,

                player = game.Engine.Player,
                ghosts = game.Engine.Ghosts,

                tiles = tiles,

                coins = game.Engine.Map.Coins,
                pellets = game.Engine.Map.PowerPellets,

                // ✅ AGORA O FRONT-END RECEBE A FRUTA!
                bonusFruit = game.Engine.BonusFruit,

                status = game.Engine.Status
            });
        }
    }
}

