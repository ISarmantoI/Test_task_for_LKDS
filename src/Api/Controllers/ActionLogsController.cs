using Api.Models;
using Api.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Api.Controllers;

/// <summary>
/// Контроллер для чтения журнала действий.
/// Маршрут: /api/actionlogs
/// Только чтение — записи создаются автоматически сервисами при мутирующих операциях.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ActionLogsController : ControllerBase
{
    // Используем репозиторий напрямую — сервисный слой для чтения журнала избыточен
    private readonly IActionLogRepository _repository;

    public ActionLogsController(IActionLogRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// GET /api/actionlogs?page=1&amp;pageSize=50
    /// Возвращает постраничный список записей журнала,
    /// отсортированных по убыванию времени (новейшие — первыми).
    /// По умолчанию: страница 1, 50 записей на странице.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var result = await _repository.GetPagedAsync(page, pageSize);
            return Ok(result);
        }
        catch (SqlException ex)
        {
            return StatusCode(500, new { error = $"Ошибка подключения к БД: {ex.Message}" });
        }
        catch (Exception ex) when (IsDbException(ex))
        {
            return StatusCode(500, new { error = $"Ошибка подключения к БД: {ex.Message}" });
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>Проверяет, является ли исключение ошибкой базы данных.</summary>
    private static bool IsDbException(Exception ex)
    {
        var typeName = ex.GetType().FullName ?? string.Empty;
        return typeName.Contains("SqlException", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("DbException", StringComparison.OrdinalIgnoreCase);
    }
}
