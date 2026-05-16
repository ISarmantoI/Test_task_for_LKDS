using Api.Infrastructure;
using Api.Models;
using Microsoft.Data.SqlClient;

namespace Api.Repositories;

public interface IActionLogRepository
{
    /// <summary>Записывает новую запись в журнал.</summary>
    Task CreateAsync(ActionLog entry);
    /// <summary>Возвращает постраничный список записей, отсортированных по убыванию времени.</summary>
    Task<PagedResult<ActionLog>> GetPagedAsync(int page, int pageSize);
}

/// <summary>
/// Репозиторий журнала действий на основе ADO.NET.
/// Использует один батч-запрос (два result set) для получения COUNT и данных страницы.
/// </summary>
public class ActionLogRepository : IActionLogRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ActionLogRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Вставляет запись в таблицу ActionLogs.
    /// Поля EntityType, EntityId, Details могут быть null.
    /// </summary>
    public async Task CreateAsync(ActionLog entry)
    {
        const string sql = """
            INSERT INTO ActionLogs (Timestamp, Action, EntityType, EntityId, Details)
            VALUES (@Timestamp, @Action, @EntityType, @EntityId, @Details)
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Timestamp", entry.Timestamp);
        command.Parameters.AddWithValue("@Action", entry.Action);
        command.Parameters.AddWithValue("@EntityType", (object?)entry.EntityType ?? DBNull.Value);
        command.Parameters.AddWithValue("@EntityId", (object?)entry.EntityId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Details", (object?)entry.Details ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Возвращает постраничный список записей журнала, отсортированных по убыванию Timestamp.
    /// Использует один SQL-запрос с двумя result set:
    /// первый — COUNT(*), второй — данные страницы.
    /// </summary>
    public async Task<PagedResult<ActionLog>> GetPagedAsync(int page, int pageSize)
    {
        // Два запроса в одном батче: сначала общее количество, затем данные страницы
        const string sql = """
            SELECT COUNT(*) FROM ActionLogs;

            SELECT Id, Timestamp, Action, EntityType, EntityId, Details
            FROM ActionLogs
            ORDER BY Timestamp DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        int offset = (page - 1) * pageSize;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Offset", offset);
        command.Parameters.AddWithValue("@PageSize", pageSize);

        await using var reader = await command.ExecuteReaderAsync();

        // Первый result set: общее количество записей
        int totalCount = 0;
        if (await reader.ReadAsync())
            totalCount = reader.GetInt32(0);

        // Переходим ко второму result set: данные страницы
        await reader.NextResultAsync();

        var items = new List<ActionLog>();
        while (await reader.ReadAsync())
            items.Add(MapRow(reader));

        return new PagedResult<ActionLog>(items, totalCount, page, pageSize);
    }

    /// <summary>Маппинг строки SqlDataReader в доменную модель ActionLog.</summary>
    private static ActionLog MapRow(SqlDataReader reader)
    {
        return new ActionLog(
            Id: reader.GetInt32(reader.GetOrdinal("Id")),
            Timestamp: reader.GetDateTime(reader.GetOrdinal("Timestamp")),
            Action: reader.GetString(reader.GetOrdinal("Action")),
            EntityType: reader.IsDBNull(reader.GetOrdinal("EntityType"))
                ? null : reader.GetString(reader.GetOrdinal("EntityType")),
            EntityId: reader.IsDBNull(reader.GetOrdinal("EntityId"))
                ? null : reader.GetInt32(reader.GetOrdinal("EntityId")),
            Details: reader.IsDBNull(reader.GetOrdinal("Details"))
                ? null : reader.GetString(reader.GetOrdinal("Details"))
        );
    }
}
