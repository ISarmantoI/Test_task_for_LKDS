using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Api.Controllers;

/// <summary>
/// Контроллер для управления сотрудниками.
/// Маршрут: /api/employees
/// Поддерживает загрузку фото через multipart/form-data.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _service;

    public EmployeesController(IEmployeeService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/employees
    /// Поиск сотрудников с фильтрацией и пагинацией.
    /// Все текстовые параметры используют поиск по подстроке (LIKE).
    /// Несколько параметров объединяются через AND.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? lastName,
        [FromQuery] string? firstName,
        [FromQuery] string? middleName,
        [FromQuery] string? position,
        [FromQuery] string? department,
        [FromQuery] string? email,
        [FromQuery] string? phone,
        [FromQuery] int? organizationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var searchParams = new EmployeeSearchParams(
                lastName, firstName, middleName,
                position, department, email, phone,
                organizationId, page, pageSize);

            var result = await _service.SearchAsync(searchParams);
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

    /// <summary>
    /// POST /api/employees
    /// Создаёт нового сотрудника. Принимает данные как multipart/form-data.
    /// Поле photo — необязательный файл изображения (до 50 МБ).
    /// Возвращает HTTP 201 с созданным объектом.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(52428800)] // 50 МБ — максимальный размер запроса с фото
    public async Task<IActionResult> Create([FromForm] EmployeeRequest request, IFormFile? photo)
    {
        try
        {
            var employee = await _service.CreateAsync(request, photo);
            return CreatedAtAction(nameof(Search), new { id = employee.Id }, employee);
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
    /// PUT /api/employees/{id}
    /// Обновляет данные сотрудника. Принимает multipart/form-data.
    /// Если photo не передан — существующее фото в БД сохраняется без изменений.
    /// </summary>
    [HttpPut("{id}")]
    [RequestSizeLimit(52428800)] // 50 МБ
    public async Task<IActionResult> Update(int id, [FromForm] EmployeeRequest request, IFormFile? photo)
    {
        try
        {
            var employee = await _service.UpdateAsync(id, request, photo);
            if (employee is null)
                return NotFound(new { error = "Сотрудник не найден" });

            return Ok(employee);
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
    /// DELETE /api/employees/{id}
    /// Удаляет сотрудника по ID.
    /// Возвращает HTTP 204 при успехе, HTTP 404 если не найден.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { error = "Сотрудник не найден" });

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
    /// GET /api/employees/{id}/photo
    /// Возвращает бинарные данные фото сотрудника.
    /// Content-Type определяется автоматически по magic bytes файла:
    /// JPEG (FF D8) → image/jpeg, PNG (89 50 4E 47) → image/png.
    /// </summary>
    [HttpGet("{id}/photo")]
    public async Task<IActionResult> GetPhoto(int id)
    {
        try
        {
            var photoBytes = await _service.GetPhotoAsync(id);
            if (photoBytes is null || photoBytes.Length == 0)
                return NotFound(new { error = "Фото не найдено" });

            var contentType = DetectContentType(photoBytes);
            return File(photoBytes, contentType);
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
    /// Определяет MIME-тип изображения по первым байтам (magic bytes).
    /// JPEG начинается с FF D8, PNG — с 89 50 4E 47.
    /// Если формат не распознан — возвращает application/octet-stream.
    /// </summary>
    private static string DetectContentType(byte[] bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8)
            return "image/jpeg";

        if (bytes.Length >= 4
            && bytes[0] == 0x89
            && bytes[1] == 0x50  // 'P'
            && bytes[2] == 0x4E  // 'N'
            && bytes[3] == 0x47) // 'G'
            return "image/png";

        return "application/octet-stream";
    }

    /// <summary>
    /// Проверяет, является ли исключение ошибкой базы данных.
    /// </summary>
    private static bool IsDbException(Exception ex)
    {
        var typeName = ex.GetType().FullName ?? string.Empty;
        return typeName.Contains("SqlException", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("DbException", StringComparison.OrdinalIgnoreCase);
    }
}
