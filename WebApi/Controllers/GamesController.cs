using System;
using System.Linq;
using AutoMapper;
using Game.Domain;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api")]
    public class GamesController : Controller
    {
        private readonly IGameRepository gameRepository;
        private readonly IUserRepository userRepository;
        private readonly IMapper mapper;

        public GamesController(
            IGameRepository gameRepository,
            IUserRepository userRepository,
            IMapper mapper)
        {
            this.gameRepository = gameRepository;
            this.userRepository = userRepository;
            this.mapper = mapper;
        }

        [HttpGet("games/{gameId}", Name = nameof(GetGame))]
        [SwaggerResponse(200, "OK", typeof(GameDto))]
        [SwaggerResponse(404, "Game is not found")]
        public ActionResult<GameDto> GetGame([FromRoute] Guid gameId)
        {
            var game = gameRepository.FindById(gameId);
            if (game == null)
                return NotFound("Game is not found");

            var gameDto = mapper.Map<GameDto>(game);
            return Ok(gameDto);
        }

        [HttpPost("games")]
        [SwaggerResponse(201, "Game created", typeof(GameDto))]
        [SwaggerResponse(404, "User is not found")]
        [SwaggerResponse(409, "User is in another game")]
        [SwaggerResponse(422, "Incorrect count of turns")]
        public ActionResult<GameDto> StartNewGame([FromBody] GameToCreateDto body)
        {
            var user = userRepository.FindById(body.PlayerId);
            if (user == null)
                return NotFound("User is not found");
            if (user.CurrentGameId.HasValue)
                return Conflict($"User {body.PlayerId} is in another game");

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var game = new GameEntity(body.TurnsCount);
            game.AddPlayer(user);
            var savedGame = gameRepository.Insert(game);

            user.CurrentGameId = savedGame.Id;
            userRepository.Update(user);

            var gameDto = mapper.Map<GameDto>(savedGame);
            return CreatedAtRoute(
                nameof(GetGame),
                new { gameId = savedGame.Id },
                gameDto);
        }

        [HttpPost("games/join")]
        [SwaggerResponse(200, "OK", typeof(GameDto))]
        [SwaggerResponse(404, "User or game is not found")]
        [SwaggerResponse(409, "User is in another game or can't join")]
        public ActionResult<GameDto> JoinGame([FromBody] GameToJoinDto body)
        {
            var user = userRepository.FindById(body.PlayerId);
            if (user == null)
                return NotFound("User is not found");
            if (user.CurrentGameId.HasValue)
                return Conflict($"User {body.PlayerId} is in another game");

            var games = gameRepository.FindWaitingToStart();
            if (!games.Any())
                return NotFound("Can't find any game to join");
            var gameIndex = new Random().Next(games.Count);
            var game = games[gameIndex];

            game.AddPlayer(user);
            if (!gameRepository.TryUpdateWaitingToStart(game))
                return Conflict("Can't join. Try again");

            user.CurrentGameId = game.Id;
            userRepository.Update(user);

            var gameDto = mapper.Map<GameDto>(game);
            return Ok(gameDto);
        }

        [HttpPost("users/{userId}/game/decide")]
        [SwaggerResponse(200, "OK", typeof(GameDto))]
        [SwaggerResponse(404, "User or game is not found")]
        [SwaggerResponse(409, "Decision is not needed")]
        public ActionResult<GameDto> DecideInGame(Guid userId,
            [FromBody] PlayerDecision decision)
        {
            var user = userRepository.FindById(userId);
            if (user == null)
                return NotFound("User is not found");
            if (!user.CurrentGameId.HasValue)
                return NotFound("User is not in any game");

            var gameId = user.CurrentGameId.Value;
            var game = gameRepository.FindById(gameId);
            if (game == null)
                return NotFound("Game is not found");

            if (game.Players.All(p => p.UserId != userId))
                return NotFound("Game doesn't contain the user");

            try
            {
                game.SetPlayerDecision(userId, decision);
            }
            catch (InvalidOperationException)
            {
                return Conflict();
            }
            
            if (game.HaveDecisionOfEveryPlayer)
                game.FinishTurn();
            gameRepository.Update(game);

            var gameDto = mapper.Map<GameDto>(game);
            return Ok(gameDto);
        }

        [HttpPost("users/{userId}/game/exit")]
        [SwaggerResponse(200, "OK", typeof(UserDto))]
        [SwaggerResponse(404, "User is not found or is not in any game")]
        public ActionResult<UserDto> ExitGame([FromRoute] Guid userId)
        {
            var user = userRepository.FindById(userId);
            if (user == null)
                return NotFound("User is not found");
            if (!user.CurrentGameId.HasValue)
                return NotFound("User is not in any game");

            var gameId = user.CurrentGameId.Value;
            var game = gameRepository.FindById(gameId);
            if (game != null)
            {
                game.Cancel();
                gameRepository.Update(game);
            }

            user.ExitGame();
            userRepository.Update(user);

            var userDto = mapper.Map<UserDto>(user);
            return Ok(userDto);
        }
    }
}