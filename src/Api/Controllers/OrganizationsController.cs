using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Api.Controllers;

/// <summary>
/// Контроллер для управления организациями.
/// Маршрут: /api/organizations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _service;

    public OrganizationsController(IOrganizationService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/organizations
    /// Возвращает список всех организаций, отсортированных по названию.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var organizations = await _service.GetAllAsync();
            return Ok(organizations);
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

    /// <summary>
    /// POST /api/organizations
    /// Создаёт новую организацию.
    /// Возвращает HTTP 201 с созданным объектом.
    /// Возвращает HTTP 400, если поле Name пустое.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrganizationRequest request)
    {
        try
        {
            var organization = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetAll), new { id = organization.Id }, organization);
        }
        catch (ArgumentException ex)
        {
            // Ошибка валидации — возвращаем 400 с описанием и именем поля
            return BadRequest(new { error = ex.Message, field = ex.ParamName });
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

    /// <summary>
    /// PUT /api/organizations/{id}
    /// Обновляет организацию по ID.
    /// Возвращает HTTP 404, если организация не найдена.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] OrganizationRequest request)
    {
        try
        {
            var organization = await _service.UpdateAsync(id, request);
            if (organization is null)
                return NotFound(new { error = "Организация не найдена" });

            return Ok(organization);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message, field = ex.ParamName });
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

    /// <summary>
    /// DELETE /api/organizations/{id}
    /// Удаляет организацию и всех её сотрудников (каскадно).
    /// Возвращает HTTP 204 при успехе, HTTP 404 если не найдена.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { error = "Организация не найдена" });

            return NoContent();
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

    /// <summary>
    /// Вспомогательная проверка: является ли исключение ошибкой базы данных.
    /// Используется для перехвата нестандартных DB-исключений помимо SqlException.
    /// </summary>
    private static bool IsDbException(Exception ex)
    {
        var typeName = ex.GetType().FullName ?? string.Empty;
        return typeName.Contains("SqlException", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("DbException", StringComparison.OrdinalIgnoreCase);
    }
}
