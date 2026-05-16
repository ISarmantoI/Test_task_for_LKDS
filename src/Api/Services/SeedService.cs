using Api.Models;
using Api.Repositories;

namespace Api.Services;

public interface ISeedService
{
    /// <summary>
    /// Генерирует 10 организаций по 100 сотрудников каждая.
    /// Возвращает количество созданных организаций и сотрудников.
    /// </summary>
    Task<(int organizations, int employees)> SeedAsync();
}

/// <summary>
/// Сервис генерации тестовых данных.
/// Использует встроенные массивы русских имён, фамилий, должностей и адресов.
/// Не обращается к внешним сервисам — всё генерируется локально через System.Random.
/// </summary>
public class SeedService : ISeedService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IActionLogService _actionLogService;

    // --- Словари для генерации данных ---

    /// <summary>Мужские имена.</summary>
    private static readonly string[] MaleFirstNames =
    [
        "Александр", "Дмитрий", "Максим", "Сергей", "Андрей",
        "Алексей", "Артём", "Илья", "Кирилл", "Михаил",
        "Никита", "Роман", "Евгений", "Владимир", "Иван",
        "Павел", "Антон", "Денис", "Виктор", "Николай"
    ];

    /// <summary>Женские имена.</summary>
    private static readonly string[] FemaleFirstNames =
    [
        "Анна", "Мария", "Елена", "Ольга", "Наталья",
        "Татьяна", "Ирина", "Светлана", "Юлия", "Екатерина",
        "Людмила", "Надежда", "Галина", "Валентина", "Марина",
        "Вера", "Алина", "Дарья", "Полина", "Ксения"
    ];

    /// <summary>Мужские фамилии.</summary>
    private static readonly string[] MaleLastNames =
    [
        "Иванов", "Смирнов", "Кузнецов", "Попов", "Васильев",
        "Петров", "Соколов", "Михайлов", "Новиков", "Фёдоров",
        "Морозов", "Волков", "Алексеев", "Лебедев", "Семёнов",
        "Егоров", "Павлов", "Козлов", "Степанов", "Николаев"
    ];

    /// <summary>Женские фамилии.</summary>
    private static readonly string[] FemaleLastNames =
    [
        "Иванова", "Смирнова", "Кузнецова", "Попова", "Васильева",
        "Петрова", "Соколова", "Михайлова", "Новикова", "Фёдорова",
        "Морозова", "Волкова", "Алексеева", "Лебедева", "Семёнова",
        "Егорова", "Павлова", "Козлова", "Степанова", "Николаева"
    ];

    /// <summary>Мужские отчества.</summary>
    private static readonly string[] MalePatronymics =
    [
        "Александрович", "Дмитриевич", "Сергеевич", "Андреевич", "Алексеевич",
        "Михайлович", "Иванович", "Павлович", "Николаевич", "Владимирович"
    ];

    /// <summary>Женские отчества.</summary>
    private static readonly string[] FemalePatronymics =
    [
        "Александровна", "Дмитриевна", "Сергеевна", "Андреевна", "Алексеевна",
        "Михайловна", "Ивановна", "Павловна", "Николаевна", "Владимировна"
    ];

    /// <summary>Должности.</summary>
    private static readonly string[] Positions =
    [
        "Менеджер", "Разработчик", "Аналитик", "Бухгалтер", "Юрист",
        "Маркетолог", "Дизайнер", "Тестировщик", "Системный администратор", "HR-специалист",
        "Финансовый директор", "Технический директор", "Руководитель проекта", "Консультант",
        "Специалист по продажам", "Логист", "Экономист", "Секретарь", "Инженер", "Архитектор"
    ];

    /// <summary>Отделы.</summary>
    private static readonly string[] Departments =
    [
        "Отдел разработки", "Бухгалтерия", "Юридический отдел", "Отдел маркетинга", "HR-отдел",
        "Финансовый отдел", "IT-отдел", "Отдел продаж", "Отдел логистики", "Административный отдел"
    ];

    /// <summary>Организационно-правовые формы для названий компаний.</summary>
    private static readonly string[] CompanyPrefixes = ["ООО", "АО", "ПАО", "ЗАО", "ИП"];

    /// <summary>Слова для названий компаний.</summary>
    private static readonly string[] CompanyNames =
    [
        "Ромашка", "Берёза", "Сосна", "Дуб", "Клён",
        "Альфа", "Бета", "Гамма", "Дельта", "Омега",
        "Прогресс", "Развитие", "Успех", "Перспектива", "Горизонт",
        "Технологии", "Инновации", "Решения", "Системы", "Сервис"
    ];

    /// <summary>Города для адресов.</summary>
    private static readonly string[] Cities =
    [
        "Москва", "Санкт-Петербург", "Новосибирск", "Екатеринбург", "Казань",
        "Нижний Новгород", "Челябинск", "Самара", "Омск", "Ростов-на-Дону"
    ];

    /// <summary>Улицы для адресов.</summary>
    private static readonly string[] Streets =
    [
        "ул. Ленина", "ул. Пушкина", "пр. Мира", "ул. Гагарина", "ул. Советская",
        "пр. Победы", "ул. Садовая", "ул. Центральная", "пр. Строителей", "ул. Молодёжная"
    ];

    public SeedService(
        IOrganizationRepository organizationRepository,
        IEmployeeRepository employeeRepository,
        IActionLogService actionLogService)
    {
        _organizationRepository = organizationRepository;
        _employeeRepository = employeeRepository;
        _actionLogService = actionLogService;
    }

    /// <summary>
    /// Генерирует 10 организаций, каждая с 100 сотрудниками.
    /// Названия организаций уникальны в рамках одной операции генерации.
    /// После завершения записывает событие SeedData в журнал.
    /// </summary>
    public async Task<(int organizations, int employees)> SeedAsync()
    {
        var random = new Random();
        int orgCount = 0;
        int empCount = 0;

        // Отслеживаем использованные названия для обеспечения уникальности
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < 10; i++)
        {
            // Генерируем уникальное название организации
            string orgName = GenerateUniqueOrgName(random, usedNames);
            usedNames.Add(orgName);

            // Формируем адрес из случайного города и улицы
            string city = Cities[random.Next(Cities.Length)];
            string street = Streets[random.Next(Streets.Length)];
            int building = random.Next(1, 200);
            string address = $"{city}, {street}, д. {building}";

            string phone = $"+7 ({random.Next(900, 999)}) {random.Next(100, 999)}-{random.Next(10, 99)}-{random.Next(10, 99)}";
            // Email формируем из транслитерированного названия компании
            string email = $"info@{orgName.ToLower().Replace(" ", "").Replace("ООО", "").Replace("АО", "").Replace("ПАО", "").Replace("ЗАО", "").Replace("ИП", "").Trim()}.ru";
            string inn = GenerateInn(random);

            var orgRequest = new OrganizationRequest(
                Name: orgName,
                LegalAddress: address,
                Phone: phone,
                Email: email,
                Inn: inn);

            var organization = await _organizationRepository.CreateAsync(orgRequest);
            orgCount++;

            // Генерируем 100 сотрудников для текущей организации
            for (int j = 0; j < 100; j++)
            {
                // Случайно определяем пол для согласования имени/фамилии/отчества
                bool isFemale = random.Next(2) == 0;

                string firstName = isFemale
                    ? FemaleFirstNames[random.Next(FemaleFirstNames.Length)]
                    : MaleFirstNames[random.Next(MaleFirstNames.Length)];

                string lastName = isFemale
                    ? FemaleLastNames[random.Next(FemaleLastNames.Length)]
                    : MaleLastNames[random.Next(MaleLastNames.Length)];

                string middleName = isFemale
                    ? FemalePatronymics[random.Next(FemalePatronymics.Length)]
                    : MalePatronymics[random.Next(MalePatronymics.Length)];

                string position = Positions[random.Next(Positions.Length)];
                string department = Departments[random.Next(Departments.Length)];

                string empPhone = $"+7 ({random.Next(900, 999)}) {random.Next(100, 999)}-{random.Next(10, 99)}-{random.Next(10, 99)}";
                // Email сотрудника: транслит фамилии + первая буква имени @ домен компании
                string empEmail = $"{TransliterateForEmail(lastName)}.{TransliterateForEmail(firstName)[0]}@{TransliterateForEmail(orgName.Split(' ').Last())}.ru";

                // Возраст от 25 до 60 лет
                int age = random.Next(25, 61);
                var birthDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-age).AddDays(-random.Next(365)));

                // Дата приёма: от 1 до 15 лет назад
                int yearsAgo = random.Next(1, 16);
                var hireDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-yearsAgo).AddDays(-random.Next(365)));

                var empRequest = new EmployeeRequest(
                    OrganizationId: organization.Id,
                    FirstName: firstName,
                    LastName: lastName,
                    MiddleName: middleName,
                    Position: position,
                    Department: department,
                    Phone: empPhone,
                    Email: empEmail,
                    BirthDate: birthDate,
                    HireDate: hireDate);

                // Фото не генерируем (null)
                await _employeeRepository.CreateAsync(empRequest, null);
                empCount++;
            }
        }

        // Записываем итоговое событие в журнал
        await _actionLogService.WriteAsync(
            action: "SeedData",
            entityType: "System",
            entityId: 0,
            details: $"Создано {orgCount} организаций и {empCount} сотрудников");

        return (orgCount, empCount);
    }

    /// <summary>
    /// Генерирует уникальное название организации.
    /// Если сгенерированное имя уже использовалось — добавляет порядковый номер.
    /// </summary>
    private static string GenerateUniqueOrgName(Random random, HashSet<string> usedNames)
    {
        string prefix = CompanyPrefixes[random.Next(CompanyPrefixes.Length)];
        string name = CompanyNames[random.Next(CompanyNames.Length)];
        string candidate = $"{prefix} {name}";

        if (!usedNames.Contains(candidate))
            return candidate;

        // Добавляем числовой суффикс для уникальности: "ООО Ромашка 2", "ООО Ромашка 3" и т.д.
        int counter = 2;
        while (usedNames.Contains($"{candidate} {counter}"))
            counter++;

        return $"{candidate} {counter}";
    }

    /// <summary>
    /// Генерирует случайный 10-значный ИНН (только для тестовых данных, не валидный).
    /// </summary>
    private static string GenerateInn(Random random)
        => string.Concat(Enumerable.Range(0, 10).Select(_ => random.Next(0, 10).ToString()));

    /// <summary>
    /// Транслитерирует русский текст в латиницу для формирования email-адресов.
    /// Использует упрощённую схему транслитерации (ГОСТ не соблюдается).
    /// </summary>
    private static string TransliterateForEmail(string text)
    {
        var map = new Dictionary<char, string>
        {
            ['а'] = "a",  ['б'] = "b",  ['в'] = "v",  ['г'] = "g",  ['д'] = "d",
            ['е'] = "e",  ['ё'] = "yo", ['ж'] = "zh", ['з'] = "z",  ['и'] = "i",
            ['й'] = "y",  ['к'] = "k",  ['л'] = "l",  ['м'] = "m",  ['н'] = "n",
            ['о'] = "o",  ['п'] = "p",  ['р'] = "r",  ['с'] = "s",  ['т'] = "t",
            ['у'] = "u",  ['ф'] = "f",  ['х'] = "kh", ['ц'] = "ts", ['ч'] = "ch",
            ['ш'] = "sh", ['щ'] = "sch",['ъ'] = "",   ['ы'] = "y",  ['ь'] = "",
            ['э'] = "e",  ['ю'] = "yu", ['я'] = "ya"
        };

        var result = new System.Text.StringBuilder();
        foreach (char c in text.ToLower())
        {
            if (map.TryGetValue(c, out string? transliterated))
                result.Append(transliterated);
            else if (char.IsLetterOrDigit(c))
                result.Append(c);
            // Пробелы и спецсимволы пропускаем
        }

        // Если транслитерация дала пустую строку — возвращаем заглушку
        return result.Length > 0 ? result.ToString() : "user";
    }
}
