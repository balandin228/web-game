using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace Game.Domain
{
    public class GameTurnEntity
    {
        [BsonElement]
        public Guid Id { get; private set; }

        [BsonElement]
        public Guid GameId { get; }


        public GameTurnEntity(Guid gameId, int turnIndex, Dictionary<Guid, PlayerDecision> playerDecisions, Guid winnerId)
            : this(Guid.Empty, gameId, turnIndex, playerDecisions, winnerId)
        {

        }

        [BsonConstructor]
        public GameTurnEntity(Guid id, Guid gameId, int turnIndex, Dictionary<Guid, PlayerDecision> playerDecisions, Guid winnerId)
        {
            Id = id;
            GameId = gameId;
            TurnIndex = turnIndex;
            this.playerDecisions = playerDecisions;
            WinnerId = winnerId;
        }

        [BsonElement]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        private readonly Dictionary<Guid, PlayerDecision> playerDecisions;

        [BsonElement]
        public Guid WinnerId { get; }

        [BsonElement]
        public int TurnIndex { get; }

        public IReadOnlyDictionary<Guid, PlayerDecision> PlayerDecisions => playerDecisions;
    }
}