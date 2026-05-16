using Api.Models;
using Api.Repositories;

namespace Api.Services;

public interface IActionLogService
{
    /// <summary>
    /// Записывает событие в журнал.
    /// Ошибки записи не пробрасываются — они только логируются,
    /// чтобы не прерывать основную операцию.
    /// </summary>
    Task WriteAsync(string action, string entityType, int entityId, string? details = null);
}

public class ActionLogService : IActionLogService
{
    private readonly IActionLogRepository _repository;
    private readonly ILogger<ActionLogService> _logger;

    public ActionLogService(IActionLogRepository repository, ILogger<ActionLogService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task WriteAsync(string action, string entityType, int entityId, string? details = null)
    {
        try
        {
            // Создаём запись с текущим временем UTC
            var entry = new ActionLog(0, DateTime.UtcNow, action, entityType, entityId, details);
            await _repository.CreateAsync(entry);
        }
        catch (Exception ex)
        {
            // Ошибка журналирования не должна прерывать основной запрос
            _logger.LogError(ex,
                "Не удалось записать в журнал: действие={Action}, тип={EntityType}, id={EntityId}",
                action, entityType, entityId);
        }
    }
}
