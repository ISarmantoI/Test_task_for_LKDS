namespace Api.Models;

public record Organization(
    int Id,
    string Name,
    string? LegalAddress,
    string? Phone,
    string? Email,
    string? Inn,
    DateTime CreatedAt);
