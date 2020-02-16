using System;
using System.Collections.Generic;
using MongoDB.Driver;

namespace Game.Domain
{
    public class MongoUserRepository : IUserRepository
    {
        private readonly IMongoCollection<UserEntity> userCollection;
        public const string CollectionName = "users";

        public MongoUserRepository(IMongoDatabase database)
        {
            userCollection = database.GetCollection<UserEntity>(CollectionName);
            userCollection.Indexes.CreateOne(new CreateIndexModel<UserEntity>(
                Builders<UserEntity>.IndexKeys.Ascending(u => u.Login),
                new CreateIndexOptions { Unique = true }));
        }

        public UserEntity Insert(UserEntity user)
        {
            userCollection.InsertOne(user);
            return user;
        }

        public UserEntity FindById(Guid id)
        {
            return userCollection.Find(u => u.Id == id).SingleOrDefault();
        }

        public UserEntity GetOrCreateByLogin(string login)
        {
            // Возможен data-race двух параллельных запросов GetOrCreate
            // В один запрос c Upsert-ом
            try
            {
                return userCollection.FindOneAndUpdate<UserEntity>(
                    u => u.Login == login,
                    Builders<UserEntity>.Update
                        .SetOnInsert(u => u.Id, Guid.NewGuid()),
                    new FindOneAndUpdateOptions<UserEntity, UserEntity>
                    {
                        IsUpsert = true,
                        ReturnDocument = ReturnDocument.After
                    });

            }
            catch (MongoCommandException e) when (e.Code == 11000)
            {
                return userCollection.FindSync(u => u.Login == login).First();
            }
            
            //А вот без изысков в два запроса. При создании работает медленнее.
            var userEntity = userCollection.FindSync(u => u.Login == login).FirstOrDefault();
            if (userEntity != null) return userEntity;
            var newUser = new UserEntity(Guid.NewGuid()) { Login = login };
            Insert(newUser);
            return newUser;
        }

        public void Update(UserEntity user)
        {
            userCollection.ReplaceOne(u => u.Id == user.Id, user);
        }

        // Для вывода списка всех пользователей (упорядоченных по логину)
        // страницы нумеруются с единицы
        public PageList<UserEntity> GetPage(int pageNumber, int pageSize)
        {
            var totalCount = userCollection.CountDocuments(u => true);
            var users = userCollection.Find(u => true)
                .SortBy(u => u.Login)
                .Skip((pageNumber - 1) * pageSize)
                .Limit(pageSize)
                .ToList();
            return new PageList<UserEntity>(
                users, totalCount, pageNumber, pageSize);
        }

        public void Delete(Guid id)
        {
            userCollection.DeleteOne(u => u.Id == id);
        }

        public void UpdateOrInsert(UserEntity user, out bool isInserted)
        {
            var result = userCollection.ReplaceOne(
                u => u.Id == user.Id,
                user,
                new ReplaceOptions
                {
                    IsUpsert = true,
                });
            isInserted = result.IsAcknowledged && result.ModifiedCount == 0;
        }

        // Пример одного специализированного метода репозитория, вместо серии более стандартных.
        // Пример частичного обновления нескольких сущностей в БД
        // Сейчас вместо этого кода работает program.UpdatePlayersWhenGameFinished
        public void UpdatePlayersWhenGameIsFinished(IEnumerable<Guid> userIds)
        {
            var updateBuilder = Builders<UserEntity>.Update;
            userCollection.UpdateMany(
                Builders<UserEntity>.Filter.In(u => u.Id, userIds),
                updateBuilder.Combine(
                    updateBuilder.Inc(u => u.GamesPlayed, 1),
                    updateBuilder.Set(u => u.CurrentGameId, null)
                ));
        }
    }
}