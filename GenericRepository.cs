using System.Collections;
using System.ComponentModel;
using Dapper;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using GenericRepositoryManager.Model;

namespace GenericRepositoryManager
{
    public abstract class GenericRepository<T> : IRepository<T> where T : class
    {
        private readonly string _connectionString;
        private readonly string _tableName;
        private IDbConnection Connection => new SqlConnection(_connectionString);

        protected GenericRepository(string connectionString)
        {
            _connectionString = connectionString;
            _tableName = typeof(T).Name;
        }
        private IEnumerable<PropertyInfo> GetProperties => typeof(T).GetProperties();
        private string GenerateInsertQuery()
        {
            var insertQuery = new StringBuilder($"INSERT INTO [{_tableName}] ");

            insertQuery.Append("(");

            var properties = GenerateListOfProperties(GetProperties);
            properties.ForEach(prop => { insertQuery.Append($"[{prop}],"); });

            insertQuery
                .Remove(insertQuery.Length - 1, 1)
                .Append(") VALUES (");

            properties.ForEach(prop => { insertQuery.Append($"@{prop},"); });

            insertQuery
                .Remove(insertQuery.Length - 1, 1)
                .Append(");");

            return insertQuery.ToString();
        }
        private string GenerateUpdateQuery()
        {
            var updateQuery = new StringBuilder($"UPDATE [{_tableName}] SET ");
            var properties = GenerateListOfProperties(GetProperties);

            properties.ForEach(property =>
            {
                if (!property.Equals("Id") && !property.Equals("CreatedTime"))
                {
                    updateQuery.Append($"{property}=@{property},");
                }
            });

            updateQuery.Remove(updateQuery.Length - 1, 1);
            updateQuery.Append(" WHERE Id=@Id");

            return updateQuery.ToString();
        }
        private static List<string> GenerateListOfProperties(IEnumerable<PropertyInfo> listOfProperties)
        {
            return (from prop in listOfProperties
                let attributes = prop.GetCustomAttributes(typeof(DescriptionAttribute), false)
                where attributes.Length <= 0 || (attributes[0] as DescriptionAttribute)?.Description != "ignore"
                select prop.Name).ToList();
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            using var connection = Connection;
            var query = $"SELECT * FROM [{_tableName}]";

            return await connection.QueryAsync<T>(query);
        }
        public async Task<PagingResponse<IEnumerable<T>>> GetAllWithPagingAsync(int currentPageNumber, int pageSize)
        {
            const int maxPagSize = 100;
            pageSize = pageSize is > 0 and <= maxPagSize ? pageSize : maxPagSize;

            var skip = (currentPageNumber - 1) * pageSize;
            var take = pageSize;
            var totalCount = 0;

            using var connection = Connection;

            var query = $"WITH LST AS(SELECT * FROM [{_tableName}] as r)" +
                                @"SELECT 
                                        LST.*, t.Total
                                    FROM LST
                                        CROSS JOIN (SELECT Count(*) AS Total FROM LST) AS t
                                    ORDER BY CreatedTime desc " +
                                "OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;";

            var result = await connection.QueryAsync<T, int, T>(query,
                (p, c) =>
                {
                    totalCount = c;
                    return p; }, new { skip, take}, splitOn: "Total");

            var data = new PagingResponse<IEnumerable<T>>(result, totalCount, currentPageNumber, pageSize);
                return data;
        }
        public async Task<PagingResponse<IEnumerable<T>>> GetAllByUserIdWithPagingAsync(Guid id,int currentPageNumber, int pageSize)
        {
            const int maxPagSize = 100;
            pageSize = pageSize is > 0 and <= maxPagSize ? pageSize : maxPagSize;

            var skip = (currentPageNumber - 1) * pageSize;
            var take = pageSize;
            var totalCount = 0;

            using var connection = Connection;

            var query = $"WITH LST AS(SELECT * FROM [{_tableName}] as r WHERE UserId=@id)" +
                        @"SELECT 
                                        LST.*, t.Total
                                    FROM LST
                                        CROSS JOIN (SELECT Count(*) AS Total FROM LST) AS t
                                    ORDER BY CreatedTime desc " +
                        "OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;";

            var result = await connection.QueryAsync<T, int, T>(query,
                (p, c) =>
                {
                    totalCount = c;
                    return p;
                }, new { id, skip, take }, splitOn: "Total");

            var data = new PagingResponse<IEnumerable<T>>(result, totalCount, currentPageNumber, pageSize);
            return data;
        }
        public async Task<IEnumerable<T>> GetAllByUserIdAsync(Guid id)
        {
            using var connection = Connection;
            var query = $"SELECT * FROM [{_tableName}] Where UserId = @id";

            return await connection.QueryAsync<T>(query, new {id});
        }
        public async Task DeleteRowAsync(Guid id)
        {
            using var connection = Connection;
            await connection.ExecuteAsync($"DELETE FROM [{_tableName}] WHERE Id=@id", new { id });
        }
        public async Task<T> GetAsync(Guid id)
        {
            using var connection = Connection;
                var result = await connection.QueryFirstOrDefaultAsync<T>($"SELECT * FROM [{_tableName}] WHERE Id=@id", new { id });
                return result;
        }
        public async Task<T?> GetByUserIdAsync(Guid id)
        {
            using var connection = Connection;
            var result = await connection.QueryFirstOrDefaultAsync<T>($"SELECT * FROM [{_tableName}] WHERE UserId=@id", new { id });
            return result;
        }
        public async Task<int> SaveRangeAsync(IEnumerable<T> list)
        {
            var inserted = 0;
            var query = GenerateInsertQuery();
            using var connection = Connection;
            inserted += await connection.ExecuteAsync(query, list);

            return inserted;
        }
        public async Task UpdateAsync(T t)
        {
            var updateQuery = GenerateUpdateQuery();

            using var connection = Connection;
            await connection.ExecuteAsync(updateQuery, t);
        }
        public async Task InsertAsync(T t)
        {
            var insertQuery = GenerateInsertQuery();

            using var connection = Connection; 
            await connection.ExecuteAsync(insertQuery, t);
        }
        }
}
