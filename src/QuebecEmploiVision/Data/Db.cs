using System.Data;
using Microsoft.Data.SqlClient;

namespace QuebecEmploiVision.Data;

/// <summary>
/// Accès SQL Server minimal et explicite (ADO.NET). Toute la logique SQL vit dans
/// les scripts database/ (schéma, vues, procédures) : la couche C# reste fine
/// et lisible, ce qui rend chaque requête explicable en entrevue.
/// </summary>
public interface IDb
{
    Task<DataTable> QueryAsync(string sql, params SqlParameter[] parameters);
    Task<int> ExecuteAsync(string sql, params SqlParameter[] parameters);
    Task<object?> ScalarAsync(string sql, params SqlParameter[] parameters);
    string ConnectionString { get; }
}

public class SqlServerDb : IDb
{
    private readonly string _connectionString;

    public string ConnectionString => _connectionString;

    public SqlServerDb(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string DefaultConnection absente.");
    }

    public async Task<DataTable> QueryAsync(string sql, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        if (parameters.Length > 0) command.Parameters.AddRange(parameters);
        var table = new DataTable();
        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        table.Load(reader);
        return table;
    }

    public async Task<int> ExecuteAsync(string sql, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        if (parameters.Length > 0) command.Parameters.AddRange(parameters);
        await connection.OpenAsync();
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<object?> ScalarAsync(string sql, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        if (parameters.Length > 0) command.Parameters.AddRange(parameters);
        await connection.OpenAsync();
        return await command.ExecuteScalarAsync();
    }
}
