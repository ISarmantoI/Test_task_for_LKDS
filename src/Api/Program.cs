using Api.Infrastructure;
using Api.Repositories;
using Api.Services;

// -----------------------------------------------------------------------
// Точка входа приложения — настройка DI-контейнера и HTTP-конвейера
// -----------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

// Слушаем порт 8080 при запуске в Docker.
// Переменная окружения ASPNETCORE_URLS переопределяет это значение, если задана.
builder.WebHost.UseUrls("http://+:8080");

// --- Регистрация сервисов в DI-контейнере ---

// Поддержка контроллеров (Web API)
builder.Services.AddControllers();

// Фабрика подключений к БД — singleton, т.к. строка подключения не меняется
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

// Репозитории — scoped: новый экземпляр на каждый HTTP-запрос
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IActionLogRepository, ActionLogRepository>();

// Сервисы бизнес-логики — scoped
builder.Services.AddScoped<IActionLogService, ActionLogService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<ISeedService, SeedService>();

var app = builder.Build();

// --- Инициализация схемы БД до запуска приложения ---
// Если DDL-скрипт завершится ошибкой — приложение не стартует (fail-fast)
var schemaInitializer = new SchemaInitializer(
    app.Services.GetRequiredService<IDbConnectionFactory>(),
    app.Services.GetRequiredService<ILogger<SchemaInitializer>>());
await schemaInitializer.InitializeAsync();

// --- Настройка HTTP-конвейера ---

// Раздача статических файлов React из папки wwwroot
app.UseStaticFiles();

// Маршрутизация к контроллерам API
app.MapControllers();

// SPA fallback: все маршруты, не совпавшие с API, отдают index.html
// Это позволяет React Router обрабатывать навигацию на стороне клиента
app.MapFallbackToFile("index.html");

app.Run();

// Делаем класс Program доступным для WebApplicationFactory в интеграционных тестах
public partial class Program { }
