using Microsoft.Data.SqlClient;

namespace Api.Infrastructure;

/// <summary>
/// Инициализатор схемы базы данных.
/// При первом запуске создаёт базу данных (если отсутствует) и все необходимые таблицы.
/// При повторных запусках пропускает уже существующие объекты — идемпотентная операция.
/// </summary>
public class SchemaInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<SchemaInitializer> _logger;

    public SchemaInitializer(IDbConnectionFactory connectionFactory, ILogger<SchemaInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Выполняет полную инициализацию: создаёт БД и таблицы при их отсутствии.
    /// Вызывается один раз при старте приложения до app.Run().
    /// </summary>
    public async Task InitializeAsync()
    {
        // Шаг 1: убеждаемся, что сама база данных существует
        // (подключаемся к master и создаём БД, если нужно)
        await EnsureDatabaseAsync();

        // Шаг 2: подключаемся к целевой БД и создаём таблицы
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        // Таблица организаций
        await EnsureTableAsync(connection, "Organizations", """
            CREATE TABLE Organizations (
                Id           INT IDENTITY(1,1) PRIMARY KEY,
                Name         NVARCHAR(255) NOT NULL,
                LegalAddress NVARCHAR(500),
                Phone        NVARCHAR(50),
                Email        NVARCHAR(255),
                Inn          NVARCHAR(20),
                CreatedAt    DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            );
            """);

        // Таблица сотрудников (внешний ключ на Organizations)
        await EnsureTableAsync(connection, "Employees", """
            CREATE TABLE Employees (
                Id             INT IDENTITY(1,1) PRIMARY KEY,
                OrganizationId INT NOT NULL REFERENCES Organizations(Id),
                FirstName      NVARCHAR(100) NOT NULL,
                LastName       NVARCHAR(100) NOT NULL,
                MiddleName     NVARCHAR(100),
                Position       NVARCHAR(200),
                Department     NVARCHAR(200),
                Phone          NVARCHAR(50),
                Email          NVARCHAR(255),
                BirthDate      DATE,
                HireDate       DATE,
                Photo          VARBINARY(MAX),
                CreatedAt      DATETIME2 NOT NULL DEFAULT GETUTCDATE()
            );
            """);

        // Таблица журнала действий
        await EnsureTableAsync(connection, "ActionLogs", """
            CREATE TABLE ActionLogs (
                Id         INT IDENTITY(1,1) PRIMARY KEY,
                Timestamp  DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                Action     NVARCHAR(100) NOT NULL,
                EntityType NVARCHAR(50),
                EntityId   INT,
                Details    NVARCHAR(MAX)
            );
            """);
    }

    /// <summary>
    /// Проверяет существование базы данных и создаёт её при необходимости.
    /// Подключается к системной БД master, чтобы не зависеть от наличия целевой БД.
    /// </summary>
    private async Task EnsureDatabaseAsync()
    {
        // Строим строку подключения к master на основе строки из конфигурации
        var builder = new SqlConnectionStringBuilder(
            _connectionFactory.CreateConnection().ConnectionString)
        {
            InitialCatalog = "master"
        };

        using var masterConnection = new SqlConnection(builder.ConnectionString);
        await masterConnection.OpenAsync();

        // Извлекаем имя целевой БД из строки подключения
        var dbName = new SqlConnectionStringBuilder(
            _connectionFactory.CreateConnection().ConnectionString).InitialCatalog;

        // Если имя БД не задано или это сам master — ничего не делаем
        if (string.IsNullOrWhiteSpace(dbName) || dbName.Equals("master", StringComparison.OrdinalIgnoreCase))
            return;

        // Проверяем наличие БД через системный каталог
        var checkSql = "SELECT COUNT(1) FROM sys.databases WHERE name = @DbName";
        using var checkCmd = new SqlCommand(checkSql, masterConnection);
        checkCmd.Parameters.AddWithValue("@DbName", dbName);
        var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

        if (!exists)
        {
            _logger.LogInformation("База данных '{DbName}' не найдена, создаём...", dbName);

            // Имя БД нельзя передать параметром в DDL — берём из конфигурации, не из пользовательского ввода
            using var createCmd = new SqlCommand($"CREATE DATABASE [{dbName}]", masterConnection);
            await createCmd.ExecuteNonQueryAsync();

            _logger.LogInformation("База данных '{DbName}' успешно создана.", dbName);
        }
    }

    /// <summary>
    /// Создаёт таблицу, если она ещё не существует.
    /// </summary>
    /// <param name="connection">Открытое соединение с целевой БД.</param>
    /// <param name="tableName">Имя таблицы для проверки и создания.</param>
    /// <param name="ddl">DDL-скрипт CREATE TABLE.</param>
    private async Task EnsureTableAsync(SqlConnection connection, string tableName, string ddl)
    {
        var exists = await TableExistsAsync(connection, tableName);
        if (exists)
        {
            _logger.LogInformation("Таблица '{TableName}' уже существует, пропускаем.", tableName);
            return;
        }

        _logger.LogInformation("Создаём таблицу '{TableName}'...", tableName);
        using var cmd = new SqlCommand(ddl, connection);
        await cmd.ExecuteNonQueryAsync();
        _logger.LogInformation("Таблица '{TableName}' успешно создана.", tableName);
    }

    /// <summary>
    /// Проверяет существование таблицы через INFORMATION_SCHEMA.
    /// </summary>
    private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
              AND TABLE_NAME = @TableName
            """;

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@TableName", tableName);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }
}
