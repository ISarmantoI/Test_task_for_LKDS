using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Api.Controllers;

/// <summary>
/// Контроллер для генерации тестовых данных.
/// Маршрут: /api/seed
/// Используется только в целях разработки и тестирования.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SeedController : ControllerBase
{
    private readonly ISeedService _seedService;

    public SeedController(ISeedService seedService)
    {
        _seedService = seedService;
    }

    /// <summary>
    /// POST /api/seed
    /// Запускает генерацию тестовых данных: 10 организаций × 100 сотрудников.
    /// Операция может занять несколько секунд.
    /// Возвращает HTTP 200 с сообщением о количестве созданных записей.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Seed()
    {
        try
        {
            var (organizations, employees) = await _seedService.SeedAsync();
            return Ok(new
            {
                message = $"Создано {organizations} организаций и {employees} сотрудников"
            });
        }
        catch (SqlException ex)
        {
            return StatusCode(500, new { error = $"Ошибка подключения к БД: {ex.Message}" });
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
        }
    }
}
