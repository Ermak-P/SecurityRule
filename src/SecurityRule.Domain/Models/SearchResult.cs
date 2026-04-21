namespace SecurityRule.Domain.Models;

public record SearchResult(
    string EntityType,
    int EntityId,
    string FieldName,
    string FieldValue,
    string NavigateUrl);
