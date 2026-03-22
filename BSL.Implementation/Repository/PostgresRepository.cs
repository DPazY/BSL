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

        public PostgresRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private string GetTableName<T>() => $"{typeof(T).Name}s";

        public T GetByName<T>(string name) where T : Edition
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);
            string sql = $"SELECT * FROM {GetTableName<T>()} WHERE Name = @Name LIMIT 1";
            return db.QueryFirstOrDefault<T>(sql, new { Name = name });
        }

        public IEnumerable<T> GetAll<T>() where T : Edition
        {
            using IDbConnection db = new NpgsqlConnection(_connectionString);
            return db.Query<T>($"SELECT * FROM {GetTableName<T>()}");
        }

        public void Add<T>(IEnumerable<T> editions) where T : Edition
        {
            if (!editions.Any()) return;

            using IDbConnection db = new NpgsqlConnection(_connectionString);
            string tableName = $"{typeof(T).Name}s";

            var existingNames = new HashSet<string>(db.Query<string>($"SELECT Name FROM {tableName}"));

            var newEditions = editions.Where(e => !existingNames.Contains(e.Name)).ToList();

            if (!newEditions.Any()) return;

            if (typeof(T) == typeof(Book))
            {
                string sql = @"INSERT INTO Books (Name, YearBook, PublisherBook, Author)
                       VALUES (@Name, @YearBook, @PublisherBook, @Author)";
                db.Execute(sql, newEditions);
            }
            else if (typeof(T) == typeof(Newspaper))
            {
                string sql = @"INSERT INTO Newspapers (Name, PlaceOfPublication, PublishingHouse, NumberOfPages, Notes, IssueNumber, DataPublishing, ISSN) 
                       VALUES (@Name, @PlaceOfPublication, @PublishingHouse, @NumberOfPages, @Notes, @IssueNumber, @DataPublishing, @ISSN)";
                db.Execute(sql, newEditions);
            }
        }

        public void Remove<T>(IEnumerable<T> editions) where T : Edition
        {
            if (!editions.Any()) return;

            using IDbConnection db = new NpgsqlConnection(_connectionString);
            string sql = $"DELETE FROM {GetTableName<T>()} WHERE Name = @Name";
            db.Execute(sql, editions);
        }
    }
}