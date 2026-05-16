using Microsoft.Data.SqlClient;

namespace Api.Infrastructure;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}

public class SqlConnectionFactory : IDbConnectionFactory
{
    // Строка подключения, прочитанная из конфигурации при старте приложения
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration config)
        // Если строка подключения не задана — бросаем исключение сразу при старте (fail-fast)
        => _connectionString = config.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException(
               "Строка подключения 'DefaultConnection' не найдена в конфигурации.");

    public SqlConnection CreateConnection()
        => new SqlConnection(_connectionString);
}
