using Api.Infrastructure;
using Api.Models;
using Microsoft.Data.SqlClient;

namespace Api.Repositories;

public interface IEmployeeRepository
{
    /// <summary>Поиск сотрудников с фильтрацией и пагинацией.</summary>
    Task<PagedResult<Employee>> SearchAsync(EmployeeSearchParams searchParams);
    Task<Employee?> GetByIdAsync(int id);
    Task<Employee> CreateAsync(EmployeeRequest request, byte[]? photo);
    Task<Employee?> UpdateAsync(int id, EmployeeRequest request, byte[]? photo);
    Task<bool> DeleteAsync(int id);
    /// <summary>Возвращает бинарные данные фото или null, если фото отсутствует.</summary>
    Task<byte[]?> GetPhotoAsync(int id);
}
public class EmployeeRepository : IEmployeeRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EmployeeRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Выполняет поиск сотрудников по заданным фильтрам с пагинацией.
    /// Текстовые поля ищутся через LIKE '%значение%' (регистронезависимо).
    /// Несколько фильтров объединяются через AND.
    /// </summary>
    public async Task<PagedResult<Employee>> SearchAsync(EmployeeSearchParams searchParams)
    {
        // Динамически строим список условий WHERE и соответствующих параметров
        var conditions = new List<string>();
        var parameters = new List<(string Name, object Value)>();

        if (!string.IsNullOrWhiteSpace(searchParams.LastName))
        {
            conditions.Add("LastName LIKE @LastName");
            parameters.Add(("@LastName", $"%{searchParams.LastName}%"));
        }
        if (!string.IsNullOrWhiteSpace(searchParams.FirstName))
        {
            conditions.Add("FirstName LIKE @FirstName");
            parameters.Add(("@FirstName", $"%{searchParams.FirstName}%"));
        }
        if (!string.IsNullOrWhiteSpace(searchParams.MiddleName))
        {
            conditions.Add("MiddleName LIKE @MiddleName");
            parameters.Add(("@MiddleName", $"%{searchParams.MiddleName}%"));
        }
        if (!string.IsNullOrWhiteSpace(searchParams.Position))
        {
            conditions.Add("Position LIKE @Position");
            parameters.Add(("@Position", $"%{searchParams.Position}%"));
        }
        if (!string.IsNullOrWhiteSpace(searchParams.Department))
        {
            conditions.Add("Department LIKE @Department");
            parameters.Add(("@Department", $"%{searchParams.Department}%"));
        }
        if (!string.IsNullOrWhiteSpace(searchParams.Email))
        {
            conditions.Add("Email LIKE @Email");
            parameters.Add(("@Email", $"%{searchParams.Email}%"));
        }
        if (!string.IsNullOrWhiteSpace(searchParams.Phone))
        {
            conditions.Add("Phone LIKE @Phone");
            parameters.Add(("@Phone", $"%{searchParams.Phone}%"));
        }
        if (searchParams.OrganizationId.HasValue)
        {
            // Фильтр по организации — точное совпадение, не LIKE
            conditions.Add("OrganizationId = @OrganizationId");
            parameters.Add(("@OrganizationId", searchParams.OrganizationId.Value));
        }

        // Если условий нет — WHERE-clause пустой (возвращаем всех сотрудников)
        string whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : string.Empty;

        // Защита от некорректных значений пагинации
        int page = searchParams.Page < 1 ? 1 : searchParams.Page;
        int pageSize = searchParams.PageSize < 1 ? 20 : searchParams.PageSize;
        int offset = (page - 1) * pageSize;

        // Запрос для подсчёта общего количества записей (без пагинации)
        string countSql = $"""
            SELECT COUNT(*)
            FROM Employees
            {whereClause}
            """;

        // Запрос для получения страницы данных
        // HasPhoto вычисляется в SQL: 1 если Photo IS NOT NULL, иначе 0
        string dataSql = $"""
            SELECT Id, OrganizationId, FirstName, LastName, MiddleName,
                   Position, Department, Phone, Email, BirthDate, HireDate,
                   CASE WHEN Photo IS NOT NULL THEN 1 ELSE 0 END AS HasPhoto,
                   CreatedAt
            FROM Employees
            {whereClause}
            ORDER BY LastName, FirstName
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        // Получаем общее количество записей
        using var countCommand = new SqlCommand(countSql, connection);
        foreach (var (name, value) in parameters)
            countCommand.Parameters.AddWithValue(name, value);
        int totalCount = (int)(await countCommand.ExecuteScalarAsync())!;

        // Получаем данные текущей страницы
        var results = new List<Employee>();
        using var dataCommand = new SqlCommand(dataSql, connection);
        foreach (var (name, value) in parameters)
            dataCommand.Parameters.AddWithValue(name, value);
        dataCommand.Parameters.AddWithValue("@Offset", offset);
        dataCommand.Parameters.AddWithValue("@PageSize", pageSize);

        using var reader = await dataCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            results.Add(MapEmployee(reader));

        return new PagedResult<Employee>(results, totalCount, page, pageSize);
    }

    /// <summary>Возвращает сотрудника по ID или null, если не найден.</summary>
    public async Task<Employee?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT Id, OrganizationId, FirstName, LastName, MiddleName,
                   Position, Department, Phone, Email, BirthDate, HireDate,
                   CASE WHEN Photo IS NOT NULL THEN 1 ELSE 0 END AS HasPhoto,
                   CreatedAt
            FROM Employees
            WHERE Id = @Id
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapEmployee(reader);

        return null;
    }

    /// <summary>
    /// Создаёт нового сотрудника. Фото передаётся как byte[] (может быть null).
    /// Возвращает созданную запись с присвоенным ID.
    /// </summary>
    public async Task<Employee> CreateAsync(EmployeeRequest request, byte[]? photo)
    {
        const string sql = """
            INSERT INTO Employees
                (OrganizationId, FirstName, LastName, MiddleName, Position, Department,
                 Phone, Email, BirthDate, HireDate, Photo)
            OUTPUT INSERTED.Id, INSERTED.OrganizationId, INSERTED.FirstName, INSERTED.LastName,
                   INSERTED.MiddleName, INSERTED.Position, INSERTED.Department,
                   INSERTED.Phone, INSERTED.Email, INSERTED.BirthDate, INSERTED.HireDate,
                   CASE WHEN INSERTED.Photo IS NOT NULL THEN 1 ELSE 0 END AS HasPhoto,
                   INSERTED.CreatedAt
            VALUES (@OrganizationId, @FirstName, @LastName, @MiddleName, @Position, @Department,
                    @Phone, @Email, @BirthDate, @HireDate, @Photo)
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        AddEmployeeParameters(command, request, photo);

        using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return MapEmployee(reader);
    }

    /// <summary>
    /// Обновляет данные сотрудника.
    /// Если photo == null — существующее фото в БД сохраняется без изменений.
    /// Если photo передан — фото перезаписывается.
    /// </summary>
    public async Task<Employee?> UpdateAsync(int id, EmployeeRequest request, byte[]? photo)
    {
        // Выбираем SQL в зависимости от того, передано ли новое фото
        string sql = photo != null
            ? """
              UPDATE Employees
              SET OrganizationId = @OrganizationId,
                  FirstName      = @FirstName,
                  LastName       = @LastName,
                  MiddleName     = @MiddleName,
                  Position       = @Position,
                  Department     = @Department,
                  Phone          = @Phone,
                  Email          = @Email,
                  BirthDate      = @BirthDate,
                  HireDate       = @HireDate,
                  Photo          = @Photo
              OUTPUT INSERTED.Id, INSERTED.OrganizationId, INSERTED.FirstName, INSERTED.LastName,
                     INSERTED.MiddleName, INSERTED.Position, INSERTED.Department,
                     INSERTED.Phone, INSERTED.Email, INSERTED.BirthDate, INSERTED.HireDate,
                     CASE WHEN INSERTED.Photo IS NOT NULL THEN 1 ELSE 0 END AS HasPhoto,
                     INSERTED.CreatedAt
              WHERE Id = @Id
              """
            : """
              UPDATE Employees
              SET OrganizationId = @OrganizationId,
                  FirstName      = @FirstName,
                  LastName       = @LastName,
                  MiddleName     = @MiddleName,
                  Position       = @Position,
                  Department     = @Department,
                  Phone          = @Phone,
                  Email          = @Email,
                  BirthDate      = @BirthDate,
                  HireDate       = @HireDate
              OUTPUT INSERTED.Id, INSERTED.OrganizationId, INSERTED.FirstName, INSERTED.LastName,
                     INSERTED.MiddleName, INSERTED.Position, INSERTED.Department,
                     INSERTED.Phone, INSERTED.Email, INSERTED.BirthDate, INSERTED.HireDate,
                     CASE WHEN INSERTED.Photo IS NOT NULL THEN 1 ELSE 0 END AS HasPhoto,
                     INSERTED.CreatedAt
              WHERE Id = @Id
              """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@OrganizationId", request.OrganizationId);
        command.Parameters.AddWithValue("@FirstName", request.FirstName);
        command.Parameters.AddWithValue("@LastName", request.LastName);
        command.Parameters.AddWithValue("@MiddleName", (object?)request.MiddleName ?? DBNull.Value);
        command.Parameters.AddWithValue("@Position", (object?)request.Position ?? DBNull.Value);
        command.Parameters.AddWithValue("@Department", (object?)request.Department ?? DBNull.Value);
        command.Parameters.AddWithValue("@Phone", (object?)request.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("@Email", (object?)request.Email ?? DBNull.Value);
        // DateOnly конвертируем в DateTime для передачи в SqlCommand
        command.Parameters.AddWithValue("@BirthDate", request.BirthDate.HasValue
            ? (object)request.BirthDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        command.Parameters.AddWithValue("@HireDate", request.HireDate.HasValue
            ? (object)request.HireDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);

        if (photo != null)
        {
            // Используем явный тип VarBinary(-1) для VARBINARY(MAX)
            var photoParam = command.Parameters.Add("@Photo", System.Data.SqlDbType.VarBinary, -1);
            photoParam.Value = photo;
        }

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapEmployee(reader);

        return null;
    }

    /// <summary>Удаляет сотрудника по ID. Возвращает true, если запись была найдена.</summary>
    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Employees WHERE Id = @Id";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        int rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    /// <summary>
    /// Возвращает бинарные данные фото сотрудника.
    /// Возвращает null, если сотрудник не найден или фото отсутствует.
    /// </summary>
    public async Task<byte[]?> GetPhotoAsync(int id)
    {
        const string sql = "SELECT Photo FROM Employees WHERE Id = @Id";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            if (reader.IsDBNull(0))
                return null;
            return (byte[])reader.GetValue(0);
        }

        return null;
    }

    /// <summary>
    /// Вспомогательный метод: добавляет параметры сотрудника в SqlCommand.
    /// Используется при создании (INSERT).
    /// </summary>
    private static void AddEmployeeParameters(SqlCommand command, EmployeeRequest request, byte[]? photo)
    {
        command.Parameters.AddWithValue("@OrganizationId", request.OrganizationId);
        command.Parameters.AddWithValue("@FirstName", request.FirstName);
        command.Parameters.AddWithValue("@LastName", request.LastName);
        command.Parameters.AddWithValue("@MiddleName", (object?)request.MiddleName ?? DBNull.Value);
        command.Parameters.AddWithValue("@Position", (object?)request.Position ?? DBNull.Value);
        command.Parameters.AddWithValue("@Department", (object?)request.Department ?? DBNull.Value);
        command.Parameters.AddWithValue("@Phone", (object?)request.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("@Email", (object?)request.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@BirthDate", request.BirthDate.HasValue
            ? (object)request.BirthDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);
        command.Parameters.AddWithValue("@HireDate", request.HireDate.HasValue
            ? (object)request.HireDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value);

        // Фото хранится как VARBINARY(MAX); передаём DBNull если фото нет
        var photoParam = command.Parameters.Add("@Photo", System.Data.SqlDbType.VarBinary, -1);
        photoParam.Value = photo is not null ? (object)photo : DBNull.Value;
    }

    /// <summary>
    /// Маппинг строки SqlDataReader в доменную модель Employee.
    /// Кэшируем ordinal-индексы дат для небольшой оптимизации.
    /// </summary>
    private static Employee MapEmployee(SqlDataReader reader)
    {
        int birthDateOrdinal = reader.GetOrdinal("BirthDate");
        int hireDateOrdinal = reader.GetOrdinal("HireDate");

        return new Employee(
            Id: reader.GetInt32(reader.GetOrdinal("Id")),
            OrganizationId: reader.GetInt32(reader.GetOrdinal("OrganizationId")),
            FirstName: reader.GetString(reader.GetOrdinal("FirstName")),
            LastName: reader.GetString(reader.GetOrdinal("LastName")),
            MiddleName: reader.IsDBNull(reader.GetOrdinal("MiddleName"))
                ? null : reader.GetString(reader.GetOrdinal("MiddleName")),
            Position: reader.IsDBNull(reader.GetOrdinal("Position"))
                ? null : reader.GetString(reader.GetOrdinal("Position")),
            Department: reader.IsDBNull(reader.GetOrdinal("Department"))
                ? null : reader.GetString(reader.GetOrdinal("Department")),
            Phone: reader.IsDBNull(reader.GetOrdinal("Phone"))
                ? null : reader.GetString(reader.GetOrdinal("Phone")),
            Email: reader.IsDBNull(reader.GetOrdinal("Email"))
                ? null : reader.GetString(reader.GetOrdinal("Email")),
            // SQL Server возвращает DATE как DateTime — конвертируем в DateOnly
            BirthDate: reader.IsDBNull(birthDateOrdinal)
                ? null : DateOnly.FromDateTime(reader.GetDateTime(birthDateOrdinal)),
            HireDate: reader.IsDBNull(hireDateOrdinal)
                ? null : DateOnly.FromDateTime(reader.GetDateTime(hireDateOrdinal)),
            // HasPhoto вычислен в SQL как 0/1
            HasPhoto: reader.GetInt32(reader.GetOrdinal("HasPhoto")) == 1,
            CreatedAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        );
    }
}
