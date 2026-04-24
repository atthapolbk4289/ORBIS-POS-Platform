using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace PosSystem.Helpers
{
    public interface ISqlHelper
    {
        Task<List<T>> QueryAsync<T>(string sql, SqlParameter[]? parameters = null) where T : new();
        Task<T?> QueryFirstOrDefaultAsync<T>(string sql, SqlParameter[]? parameters = null) where T : new();
        Task<int> ExecuteAsync(string sql, SqlParameter[]? parameters = null);
        Task<object?> ExecuteScalarAsync(string sql, SqlParameter[]? parameters = null);
        Task<int> ExecuteStoredProcAsync(string procName, SqlParameter[]? parameters = null);
        Task<SqlConnection> OpenConnectionAsync();
    }

    public class SqlHelper : ISqlHelper
    {
        private readonly string _connectionString;

        public SqlHelper(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<SqlConnection> OpenConnectionAsync()
        {
            var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            return conn;
        }

        public async Task<List<T>> QueryAsync<T>(string sql, SqlParameter[]? parameters = null) where T : new()
        {
            var results = new List<T>();
            using var conn = await OpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(MapReader<T>(reader));
            }
            return results;
        }

        public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, SqlParameter[]? parameters = null) where T : new()
        {
            using var conn = await OpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapReader<T>(reader);
            }
            return default;
        }

        public async Task<int> ExecuteAsync(string sql, SqlParameter[]? parameters = null)
        {
            using var conn = await OpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<object?> ExecuteScalarAsync(string sql, SqlParameter[]? parameters = null)
        {
            using var conn = await OpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            
            return await cmd.ExecuteScalarAsync();
        }

        public async Task<int> ExecuteStoredProcAsync(string procName, SqlParameter[]? parameters = null)
        {
            using var conn = await OpenConnectionAsync();
            using var cmd = new SqlCommand(procName, conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            
            return await cmd.ExecuteNonQueryAsync();
        }

        private T MapReader<T>(SqlDataReader reader) where T : new()
        {
            var obj = new T();
            var props = typeof(T).GetProperties();
            foreach (var prop in props)
            {
                try 
                {
                    var ordinal = reader.GetOrdinal(prop.Name);
                    if (!reader.IsDBNull(ordinal))
                    {
                        var value = reader.GetValue(ordinal);
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        
                        if (targetType == typeof(Guid) && value is string strGuid)
                        {
                            prop.SetValue(obj, Guid.Parse(strGuid));
                        }
                        else
                        {
                            prop.SetValue(obj, Convert.ChangeType(value, targetType));
                        }
                    }
                } 
                catch 
                { 
                    /* column not in result set */ 
                }
            }
            return obj;
        }
    }
}
