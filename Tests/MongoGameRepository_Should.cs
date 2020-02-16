using System;
using System.Linq;
using FluentAssertions;
using Game.Domain;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class MongoGameRepository_Should
    {
        [SetUp]
        public void SetUp()
        {
            var db = TestMongoDatabase.Create();
            db.DropCollection(MongoGameRepository.CollectionName);
            repo = new MongoGameRepository(db);
        }

        private MongoGameRepository repo;

        [Test]
        public void CreateGame()
        {
            var gameEntity = repo.Insert(new GameEntity(10));
            Console.WriteLine(gameEntity.Id);
            gameEntity.Id.Should().NotBe(Guid.Empty);
        }

        [Test]
        public void FindGameById()
        {
            var gameEntity = repo.Insert(new GameEntity(10));
            repo.FindById(gameEntity.Id)
                .Should().NotBe(Guid.Empty);
        }

        [Test]
        public void UpdateGame()
        {
            var createdGame = repo.Insert(new GameEntity(10));
            var login = "someUserName";
            createdGame.AddPlayer(new UserEntity { Login = login });
            repo.Update(createdGame);
            var retrievedGame = repo.FindById(createdGame.Id);
            retrievedGame.Players.Should().HaveCount(1);
            retrievedGame.Players[0].Name.Should().Be(login);
        }

        [Test]
        public void FindWaitingToStart()
        {
            var playingGame = new GameEntity(10);
            playingGame.AddPlayer(new UserEntity());
            playingGame.AddPlayer(new UserEntity());
            repo.Insert(playingGame);

            var waitingToStartGame1 = new GameEntity(10);
            repo.Insert(waitingToStartGame1);
            var waitingToStartGame2 = new GameEntity(10);
            repo.Insert(waitingToStartGame2);

            var games = repo.FindWaitingToStart(1);
            games.Select(game => game.Id).Should().Equal(waitingToStartGame1.Id);
            games.First().Id.Should().Be(waitingToStartGame1.Id);
        }

        [Test]
        public void TryUpdateWaitingToStart()
        {
            var createdGame = repo.Insert(new GameEntity(10));
            createdGame.AddPlayer(new UserEntity());
            createdGame.AddPlayer(new UserEntity());

            repo.TryUpdateWaitingToStart(createdGame).Should().BeTrue();

            var retrievedGame = repo.FindById(createdGame.Id);
            retrievedGame.Status.Should().Be(GameStatus.Playing);
        }

        [Test]
        public void TryUpdateWaitingToStart_Fails_WhenGameIsNotWaitingToStart()
        {
            var createdGame = repo.Insert(new GameEntity(10));
            createdGame.AddPlayer(new UserEntity());
            createdGame.AddPlayer(new UserEntity());
            repo.Update(createdGame);

            createdGame.Cancel();
            repo.TryUpdateWaitingToStart(createdGame).Should().BeFalse();

            var retrievedGame = repo.FindById(createdGame.Id);
            retrievedGame.Status.Should().Be(GameStatus.Playing);
        }

        [Test]
        public void TryUpdateWaitingToStart_Fails_WhenNoGame()
        {
            repo.TryUpdateWaitingToStart(new GameEntity(10)).Should().BeFalse();
        }
    }
}