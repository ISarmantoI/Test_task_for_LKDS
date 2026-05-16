using Api.Infrastructure;
using Api.Models;
using Microsoft.Data.SqlClient;

namespace Api.Repositories;

public interface IOrganizationRepository
{
    Task<IReadOnlyList<Organization>> GetAllAsync();
    Task<Organization?> GetByIdAsync(int id);
    Task<Organization> CreateAsync(OrganizationRequest request);
    Task<Organization?> UpdateAsync(int id, OrganizationRequest request);
    Task<bool> DeleteAsync(int id);
}

/// <summary>
/// Репозиторий организаций на основе ADO.NET (без ORM).
/// Все запросы к БД выполняются через SqlConnection и SqlCommand.
/// </summary>
public class OrganizationRepository : IOrganizationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OrganizationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>Возвращает все организации, отсортированные по названию.</summary>
    public async Task<IReadOnlyList<Organization>> GetAllAsync()
    {
        const string sql = """
            SELECT Id, Name, LegalAddress, Phone, Email, Inn, CreatedAt
            FROM Organizations
            ORDER BY Name
            """;

        var results = new List<Organization>();

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(MapOrganization(reader));
        }

        return results;
    }

    /// <summary>Возвращает организацию по ID или null, если не найдена.</summary>
    public async Task<Organization?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT Id, Name, LegalAddress, Phone, Email, Inn, CreatedAt
            FROM Organizations
            WHERE Id = @Id
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
            return MapOrganization(reader);

        return null;
    }

    /// <summary>
    /// Создаёт новую организацию и возвращает созданную запись с присвоенным ID.
    /// Использует OUTPUT INSERTED для получения данных без дополнительного SELECT.
    /// </summary>
    public async Task<Organization> CreateAsync(OrganizationRequest request)
    {
        const string sql = """
            INSERT INTO Organizations (Name, LegalAddress, Phone, Email, Inn)
            OUTPUT INSERTED.Id, INSERTED.Name, INSERTED.LegalAddress, INSERTED.Phone,
                   INSERTED.Email, INSERTED.Inn, INSERTED.CreatedAt
            VALUES (@Name, @LegalAddress, @Phone, @Email, @Inn)
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Name", request.Name);
        // Nullable-поля передаём как DBNull.Value, если значение отсутствует
        command.Parameters.AddWithValue("@LegalAddress", (object?)request.LegalAddress ?? DBNull.Value);
        command.Parameters.AddWithValue("@Phone", (object?)request.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("@Email", (object?)request.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@Inn", (object?)request.Inn ?? DBNull.Value);

        using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return MapOrganization(reader);
    }

    /// <summary>
    /// Обновляет организацию и возвращает обновлённую запись.
    /// Возвращает null, если организация с указанным ID не найдена.
    /// </summary>
    public async Task<Organization?> UpdateAsync(int id, OrganizationRequest request)
    {
        const string sql = """
            UPDATE Organizations
            SET Name = @Name,
                LegalAddress = @LegalAddress,
                Phone = @Phone,
                Email = @Email,
                Inn = @Inn
            OUTPUT INSERTED.Id, INSERTED.Name, INSERTED.LegalAddress, INSERTED.Phone,
                   INSERTED.Email, INSERTED.Inn, INSERTED.CreatedAt
            WHERE Id = @Id
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Name", request.Name);
        command.Parameters.AddWithValue("@LegalAddress", (object?)request.LegalAddress ?? DBNull.Value);
        command.Parameters.AddWithValue("@Phone", (object?)request.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("@Email", (object?)request.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@Inn", (object?)request.Inn ?? DBNull.Value);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
            return MapOrganization(reader);

        return null;
    }

    /// <summary>
    /// Удаляет организацию и всех её сотрудников в рамках одной транзакции.
    /// Возвращает true, если организация была найдена и удалена.
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        const string deleteEmployeesSql = "DELETE FROM Employees WHERE OrganizationId = @Id";
        const string deleteOrgSql = "DELETE FROM Organizations WHERE Id = @Id";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        // Транзакция гарантирует атомарность: либо удаляется всё, либо ничего
        using var transaction = connection.BeginTransaction();
        try
        {
            // Сначала удаляем сотрудников (из-за внешнего ключа)
            using var deleteEmployeesCmd = new SqlCommand(deleteEmployeesSql, connection, transaction);
            deleteEmployeesCmd.Parameters.AddWithValue("@Id", id);
            await deleteEmployeesCmd.ExecuteNonQueryAsync();

            // Затем удаляем саму организацию
            using var deleteOrgCmd = new SqlCommand(deleteOrgSql, connection, transaction);
            deleteOrgCmd.Parameters.AddWithValue("@Id", id);
            int rowsAffected = await deleteOrgCmd.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            // rowsAffected == 0 означает, что организация не была найдена
            return rowsAffected > 0;
        }
        catch
        {
            // При любой ошибке откатываем транзакцию
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Маппинг строки SqlDataReader в доменную модель Organization.
    /// Nullable-колонки проверяются через IsDBNull перед чтением.
    /// </summary>
    private static Organization MapOrganization(SqlDataReader reader)
    {
        return new Organization(
            Id: reader.GetInt32(reader.GetOrdinal("Id")),
            Name: reader.GetString(reader.GetOrdinal("Name")),
            LegalAddress: reader.IsDBNull(reader.GetOrdinal("LegalAddress"))
                ? null : reader.GetString(reader.GetOrdinal("LegalAddress")),
            Phone: reader.IsDBNull(reader.GetOrdinal("Phone"))
                ? null : reader.GetString(reader.GetOrdinal("Phone")),
            Email: reader.IsDBNull(reader.GetOrdinal("Email"))
                ? null : reader.GetString(reader.GetOrdinal("Email")),
            Inn: reader.IsDBNull(reader.GetOrdinal("Inn"))
                ? null : reader.GetString(reader.GetOrdinal("Inn")),
            CreatedAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        );
    }
}
