using BSL.Models.Enum;

namespace BSL.Models.Dto;

/// <summary>
/// Параметры фильтрации, сортировки и пагинации книг.
/// Напрямую маппятся на методы твоего IBookService.
/// </summary>
public sealed record BookFilterQuery(
    OrderBy? OrderByYear = null,
    string? Author = null,
    string? PublisherStartsWith = null,
    int Page = 1,
    int PageSize = 12);

/// <summary>
/// Универсальный ответ с пагинацией для таблиц и карточек на фронтенде.
/// </summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

/// <summary>
/// Результат сравнительного нагрузочного теста IFS-кэша против LRU-кэша.
/// </summary>
public sealed record CacheBenchmarkResponse(
    int TotalRequests,
    double IfsTotalMs,
    double IfsP50Ms,
    double IfsP95Ms,
    double LruTotalMs,
    double LruP50Ms,
    double LruP95Ms,
    double SpeedupFactor);