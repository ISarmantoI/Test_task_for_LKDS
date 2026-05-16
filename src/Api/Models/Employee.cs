namespace Api.Models;

public record Employee(
    int Id,
    int OrganizationId,
    string FirstName,
    string LastName,
    string? MiddleName,
    string? Position,
    string? Department,
    string? Phone,
    string? Email,
    DateOnly? BirthDate,
    DateOnly? HireDate,
    bool HasPhoto,
    DateTime CreatedAt);
