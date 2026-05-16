using Api.Models;
using Api.Repositories;

namespace Api.Services;

public interface IEmployeeService
{
    Task<PagedResult<Employee>> SearchAsync(EmployeeSearchParams searchParams);
    Task<Employee?> GetByIdAsync(int id);
    /// <summary>Создаёт сотрудника. Фото передаётся как IFormFile (может быть null).</summary>
    Task<Employee> CreateAsync(EmployeeRequest request, IFormFile? photo);
    /// <summary>Обновляет сотрудника. Если photo == null — существующее фото сохраняется.</summary>
    Task<Employee?> UpdateAsync(int id, EmployeeRequest request, IFormFile? photo);
    Task<bool> DeleteAsync(int id);
    Task<byte[]?> GetPhotoAsync(int id);
}

/// <summary>
/// Сервис бизнес-логики для работы с сотрудниками.
/// Отвечает за валидацию, конвертацию фото и запись в журнал действий.
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repository;
    // Нужен для проверки существования организации при создании/обновлении сотрудника
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IActionLogService _actionLogService;

    public EmployeeService(
        IEmployeeRepository repository,
        IOrganizationRepository organizationRepository,
        IActionLogService actionLogService)
    {
        _repository = repository;
        _organizationRepository = organizationRepository;
        _actionLogService = actionLogService;
    }

    /// <summary>Поиск сотрудников с фильтрацией и пагинацией (делегирует в репозиторий).</summary>
    public Task<PagedResult<Employee>> SearchAsync(EmployeeSearchParams searchParams)
        => _repository.SearchAsync(searchParams);

    /// <summary>Возвращает сотрудника по ID или null.</summary>
    public Task<Employee?> GetByIdAsync(int id)
        => _repository.GetByIdAsync(id);

    /// <summary>
    /// Создаёт сотрудника после валидации имени и проверки существования организации.
    /// Конвертирует IFormFile в byte[] для хранения в VARBINARY(MAX).
    /// </summary>
    public async Task<Employee> CreateAsync(EmployeeRequest request, IFormFile? photo)
    {
        ValidateNames(request.FirstName, request.LastName);
        await ValidateOrganizationExistsAsync(request.OrganizationId);

        // Конвертируем загруженный файл в массив байт
        byte[]? photoBytes = await ReadPhotoAsync(photo);

        var employee = await _repository.CreateAsync(request, photoBytes);

        await _actionLogService.WriteAsync(
            action: "Create",
            entityType: "Employee",
            entityId: employee.Id,
            details: $"Создан сотрудник: {employee.LastName} {employee.FirstName}");

        return employee;
    }

    /// <summary>
    /// Обновляет данные сотрудника.
    /// Возвращает null, если сотрудник не найден.
    /// </summary>
    public async Task<Employee?> UpdateAsync(int id, EmployeeRequest request, IFormFile? photo)
    {
        ValidateNames(request.FirstName, request.LastName);
        await ValidateOrganizationExistsAsync(request.OrganizationId);

        byte[]? photoBytes = await ReadPhotoAsync(photo);

        var employee = await _repository.UpdateAsync(id, request, photoBytes);

        if (employee is not null)
        {
            await _actionLogService.WriteAsync(
                action: "Update",
                entityType: "Employee",
                entityId: employee.Id,
                details: $"Обновлён сотрудник: {employee.LastName} {employee.FirstName}");
        }

        return employee;
    }

    /// <summary>Удаляет сотрудника. Возвращает true, если запись была найдена.</summary>
    public async Task<bool> DeleteAsync(int id)
    {
        var deleted = await _repository.DeleteAsync(id);

        if (deleted)
        {
            await _actionLogService.WriteAsync(
                action: "Delete",
                entityType: "Employee",
                entityId: id,
                details: $"Удалён сотрудник с id: {id}");
        }

        return deleted;
    }

    /// <summary>Возвращает бинарные данные фото сотрудника или null.</summary>
    public Task<byte[]?> GetPhotoAsync(int id)
        => _repository.GetPhotoAsync(id);

    /// <summary>
    /// Проверяет, что FirstName и LastName не пустые и не состоят из пробелов.
    /// </summary>
    private static void ValidateNames(string? firstName, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException(
                "Поле FirstName не может быть пустым.", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException(
                "Поле LastName не может быть пустым.", nameof(lastName));
    }

    /// <summary>
    /// Проверяет, что организация с указанным ID существует в БД.
    /// Бросает ArgumentException, если не найдена.
    /// </summary>
    private async Task ValidateOrganizationExistsAsync(int organizationId)
    {
        var organization = await _organizationRepository.GetByIdAsync(organizationId);
        if (organization is null)
            throw new ArgumentException(
                "Организация не найдена.", nameof(organizationId));
    }

    /// <summary>
    /// Читает содержимое IFormFile в массив байт.
    /// Возвращает null, если файл не передан.
    /// </summary>
    private static async Task<byte[]?> ReadPhotoAsync(IFormFile? photo)
    {
        if (photo is null)
            return null;

        using var memoryStream = new MemoryStream();
        await photo.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }
}
