namespace Api.Models;


public record OrganizationRequest(
    /// <summary>Наименование организации (обязательное поле).</summary>
    string Name,
    string? LegalAddress,
    string? Phone,
    string? Email,
    string? Inn);

public record EmployeeRequest(
    /// <summary>Идентификатор организации (обязательное поле, должен существовать в БД).</summary>
    int OrganizationId,
    /// <summary>Имя (обязательное поле).</summary>
    string FirstName,
    /// <summary>Фамилия (обязательное поле).</summary>
    string LastName,
    string? MiddleName,
    string? Position,
    string? Department,
    string? Phone,
    string? Email,
    DateOnly? BirthDate,
    DateOnly? HireDate);

public record EmployeeSearchParams(
    string? LastName,
    string? FirstName,
    string? MiddleName,
    string? Position,
    string? Department,
    string? Email,
    string? Phone,
    /// <summary>Фильтр по организации (точное совпадение по ID).</summary>
    int? OrganizationId,
    /// <summary>Номер страницы (начиная с 1).</summary>
    int Page = 1,
    /// <summary>Количество записей на странице (по умолчанию 20).</summary>
    int PageSize = 20);

/// <summary>
/// Универсальный контейнер для постраничного ответа API.
/// </summary>
/// <typeparam name="T">Тип элементов в списке.</typeparam>
public record PagedResult<T>(
    /// <summary>Элементы текущей страницы.</summary>
    IReadOnlyList<T> Items,
    /// <summary>Общее количество записей (без учёта пагинации).</summary>
    int TotalCount,
    /// <summary>Текущий номер страницы.</summary>
    int Page,
    /// <summary>Размер страницы.</summary>
    int PageSize);
