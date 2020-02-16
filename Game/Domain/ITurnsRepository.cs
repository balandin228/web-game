using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public interface ITurnsRepository
    {
        IReadOnlyList<GameTurnEntity> GetLastTurns(Guid gameId, int maxCount);
        GameTurnEntity Insert(GameTurnEntity entity);
    }
}