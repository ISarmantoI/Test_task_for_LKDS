namespace Api.Models;

public record ActionLog(
    int Id,
    DateTime Timestamp,
    string Action,
    string? EntityType,
    int? EntityId,
    string? Details);
