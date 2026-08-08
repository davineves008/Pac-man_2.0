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

        // Cria uma nova partida
        public IActionResult NewGame()
        {
            Guid gameId = GameManager.CreateGame();

            HttpContext.Session.SetString("GameId", gameId.ToString());

            return RedirectToAction("Index");
        }

        // Recupera a partida atual da sessão
        private GameSession CurrentGame()
        {
            var id = HttpContext.Session.GetString("GameId");

            if (string.IsNullOrEmpty(id))
                return null;

            return GameManager.Get(Guid.Parse(id));
        }

        // Recebe os comandos do teclado do jogador
        [HttpPost]
        public IActionResult Move([FromBody] MoveRequest request)
        {
            var game = CurrentGame();

            if (game == null || request == null)
                return BadRequest(new { erro = "Sessão inválida ou request nulo" });

            if (Enum.TryParse<Direction>(request.Direction, true, out var dir))
            {
                // Move o jogador e valida colisões
                game.Engine.MovePlayer(dir);
            }

            return Json(new { sucesso = true, player = game.Engine.Player });
        }

        // Endpoint que fornece o estado completo do mapa, entidades e frutas para a UI
        [HttpGet]
        public IActionResult State()
        {
            var game = CurrentGame();

            // Se a sessão expirou ou não existe, cria um novo jogo na hora
            if (game == null)
            {
                Guid newGameId = GameManager.CreateGame();
                HttpContext.Session.SetString("GameId", newGameId.ToString());
                game = GameManager.Get(newGameId);
            }

            // Executa o loop do jogo (IA dos fantasmas, power-ups, etc.)
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

                // Envia os dados da fruta bônus
                bonusFruit = game.Engine.BonusFruit,

                status = game.Engine.Status
            });
        }
    }
}