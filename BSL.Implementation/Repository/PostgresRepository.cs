using BSL.Models;
using BSL.Models.Interface;
using Dapper;
using Npgsql;
using System.Data;

namespace BSL.Implementation.Repository
{
    public class PostgresRepository : IRepository
    {
        private readonly string _connectionString;
        private static int _concurrentDbRequests = 0;

        public PostgresRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private string GetTableName<T>() => $"{typeof(T).Name}s";

        public async Task<T> GetByName<T>(string name) where T : Edition
        {
            int currentLoad = Interlocked.Increment(ref _concurrentDbRequests);
            try
            {
                await Task.Delay(15 + (currentLoad * 5) + Random.Shared.Next(-5, 5));

                using IDbConnection db = new NpgsqlConnection(_connectionString);
                string sql = $"SELECT * FROM {GetTableName<T>()} WHERE Name = @Name LIMIT 1";
                
                return await db.QueryFirstOrDefaultAsync<T>(sql, new { Name = name });
            }
            finally
            {
                Interlocked.Decrement(ref _concurrentDbRequests);
            }
        }

        public async Task<IEnumerable<T>> GetAll<T>() where T : Edition
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);
            return await db.QueryAsync<T>($"SELECT * FROM {GetTableName<T>()}");
        }

        public async Task Add<T>(IEnumerable<T> editions) where T : Edition
        {
            if (!editions.Any()) return;

            using IDbConnection db = new NpgsqlConnection(_connectionString);
            string tableName = $"{typeof(T).Name}s";

            var existingNames = new HashSet<string>(await db.QueryAsync<string>($"SELECT Name FROM {tableName}"));

            var newEditions = editions.Where(e => !existingNames.Contains(e.Name)).ToList();

            if (!newEditions.Any()) return;
            if (typeof(T) == typeof(Book))
            {
                string sql = @"INSERT INTO Books (Name, YearBook, PublisherBook, Author)
                       VALUES (@Name, @YearBook, @PublisherBook, @Author)";
                await db.ExecuteAsync(sql, newEditions);
            }
            else if (typeof(T) == typeof(Newspaper))
            {
                string sql = @"INSERT INTO Newspapers (Name, PlaceOfPublication, PublishingHouse, NumberOfPages, Notes, IssueNumber, DataPublishing, ISSN) 
                       VALUES (@Name, @PlaceOfPublication, @PublishingHouse, @NumberOfPages, @Notes, @IssueNumber, @DataPublishing, @ISSN)";
                await db.ExecuteAsync(sql, newEditions);
            }
        }

        public async Task Remove<T>(IEnumerable<T> editions) where T : Edition
        {
            if (!editions.Any()) return;

            using IDbConnection db = new NpgsqlConnection(_connectionString);
            string sql = $"DELETE FROM {GetTableName<T>()} WHERE Name = @Name";
            await db.ExecuteAsync(sql, editions);
        }
    }
}