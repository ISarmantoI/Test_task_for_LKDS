using Api.Models;
using Api.Repositories;

namespace Api.Services;

public interface IOrganizationService
{
    Task<IReadOnlyList<Organization>> GetAllAsync();
    Task<Organization?> GetByIdAsync(int id);
    Task<Organization> CreateAsync(OrganizationRequest request);
    Task<Organization?> UpdateAsync(int id, OrganizationRequest request);
    Task<bool> DeleteAsync(int id);
}

/// <summary>
/// Сервис бизнес-логики для работы с организациями.
/// Отвечает за валидацию входных данных и запись в журнал действий.
/// </summary>
public class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _repository;
    private readonly IActionLogService _actionLogService;

    public OrganizationService(IOrganizationRepository repository, IActionLogService actionLogService)
    {
        _repository = repository;
        _actionLogService = actionLogService;
    }

    /// <summary>Возвращает все организации (делегирует в репозиторий без дополнительной логики).</summary>
    public Task<IReadOnlyList<Organization>> GetAllAsync()
        => _repository.GetAllAsync();

    /// <summary>Возвращает организацию по ID или null.</summary>
    public Task<Organization?> GetByIdAsync(int id)
        => _repository.GetByIdAsync(id);

    /// <summary>
    /// Создаёт организацию после валидации поля Name.
    /// После успешного создания записывает событие в журнал.
    /// </summary>
    public async Task<Organization> CreateAsync(OrganizationRequest request)
    {
        // Поле Name обязательно — пустое или пробельное значение недопустимо
        ValidateName(request.Name);

        var organization = await _repository.CreateAsync(request);

        // Журналируем создание (ошибка журнала не прерывает операцию)
        await _actionLogService.WriteAsync(
            action: "Create",
            entityType: "Organization",
            entityId: organization.Id,
            details: $"Создана организация: {organization.Name}");

        return organization;
    }

    /// <summary>
    /// Обновляет организацию после валидации.
    /// Возвращает null, если организация не найдена.
    /// </summary>
    public async Task<Organization?> UpdateAsync(int id, OrganizationRequest request)
    {
        ValidateName(request.Name);

        var organization = await _repository.UpdateAsync(id, request);

        // Журналируем только если запись действительно была найдена и обновлена
        if (organization is not null)
        {
            await _actionLogService.WriteAsync(
                action: "Update",
                entityType: "Organization",
                entityId: organization.Id,
                details: $"Обновлена организация: {organization.Name}");
        }

        return organization;
    }

    /// <summary>
    /// Удаляет организацию (и всех её сотрудников — каскадно на уровне репозитория).
    /// Возвращает true, если организация была найдена и удалена.
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        var deleted = await _repository.DeleteAsync(id);

        if (deleted)
        {
            await _actionLogService.WriteAsync(
                action: "Delete",
                entityType: "Organization",
                entityId: id,
                details: $"Удалена организация с id: {id}");
        }

        return deleted;
    }

    /// <summary>
    /// Проверяет, что Name не пустой и не состоит только из пробелов.
    /// Бросает ArgumentException при нарушении.
    /// </summary>
    private static void ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Поле Name организации не может быть пустым или состоять только из пробелов.",
                nameof(name));
    }
}
