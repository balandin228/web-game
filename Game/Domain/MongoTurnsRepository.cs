using System;
using System.Collections.Generic;
using MongoDB.Driver;

namespace Game.Domain
{
    public class MongoTurnsRepository : ITurnsRepository
    {
        public const string CollectionName = "turns";
        private readonly IMongoCollection<GameTurnEntity> collection;

        public MongoTurnsRepository(IMongoDatabase database)
        {
            collection = database.GetCollection<GameTurnEntity>(CollectionName);
            collection.Indexes.CreateOne(
                new CreateIndexModel<GameTurnEntity>(
                    Builders<GameTurnEntity>.IndexKeys
                        .Ascending(t => t.GameId).Ascending(t => t.TurnIndex))
            );
        }

        public IReadOnlyList<GameTurnEntity> GetLastTurns(Guid gameId, int maxCount)
        {
            var result = collection.Find(t => t.GameId == gameId)
                .SortByDescending(t => t.TurnIndex)
                .Limit(maxCount)
                .ToList();
            result.Reverse();
            return result;
        }

        public GameTurnEntity Insert(GameTurnEntity entity)
        {
            collection.InsertOne(entity);
            return entity;
        }
    }
}