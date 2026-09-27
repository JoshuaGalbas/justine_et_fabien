using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using WeddingApi.Functions.Models;

namespace WeddingApi.Functions.Services;

public interface IHouseholdRepository
{
    Task<HouseholdAccount?> GetByEmailAsync(string email);
    Task SaveAsync(HouseholdAccount account);
    Task<List<HouseholdAccount>> ListAsync();
    Task RecordAuditAsync(AuditLogEntry entry);
    Task<List<AuditLogEntry>> ListAuditEntriesAsync();
}

public sealed class HouseholdRepository : IHouseholdRepository
{
    private const string DefaultTableName = "Households";
    private const string AuditTableName = "AuditLog";
    private readonly Lazy<TableClient> _tableClient;
    private readonly Lazy<TableClient> _auditTableClient;

    public HouseholdRepository()
    {
        _tableClient = new Lazy<TableClient>(CreateHouseholdsTableClient);
        _auditTableClient = new Lazy<TableClient>(CreateAuditTableClient);
    }

    public async Task<HouseholdAccount?> GetByEmailAsync(string email)
    {
        var normalized = email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var query = $"Email eq '{EscapeFilter(normalized)}'";
        await foreach (var entity in _tableClient.Value.QueryAsync<HouseholdTableEntity>(filter: query))
        {
            return entity.ToHouseholdAccount();
        }

        return null;
    }

    public async Task SaveAsync(HouseholdAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        var entity = HouseholdTableEntity.FromHousehold(account);
        await _tableClient.Value.UpsertEntityAsync(entity, TableUpdateMode.Replace);
    }

    public async Task<List<HouseholdAccount>> ListAsync()
    {
        var results = new List<HouseholdAccount>();

        await foreach (var entity in _tableClient.Value.QueryAsync<HouseholdTableEntity>())
        {
            results.Add(entity.ToHouseholdAccount());
        }

        return results;
    }

    public async Task RecordAuditAsync(AuditLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var entity = new AuditLogTableEntity
        {
            PartitionKey = "audit",
            RowKey = Guid.NewGuid().ToString("N"),
            Action = entry.Action,
            HouseholdId = entry.HouseholdId,
            Actor = entry.Actor,
            Details = JsonSerializer.Serialize(entry.Details ?? new Dictionary<string, string>()),
            PerformedAt = entry.PerformedAt
        };

        await _auditTableClient.Value.UpsertEntityAsync(entity, TableUpdateMode.Replace);
    }

    public async Task<List<AuditLogEntry>> ListAuditEntriesAsync()
    {
        var entries = new List<AuditLogEntry>();

        await foreach (var entity in _auditTableClient.Value.QueryAsync<AuditLogTableEntity>())
        {
            var details = string.IsNullOrWhiteSpace(entity.Details)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(entity.Details) ?? new Dictionary<string, string>();

            entries.Add(new AuditLogEntry
            {
                Id = entity.RowKey,
                Action = entity.Action,
                HouseholdId = entity.HouseholdId,
                Actor = entity.Actor,
                Details = details,
                PerformedAt = entity.PerformedAt
            });
        }

        return entries
            .OrderByDescending(x => x.PerformedAt)
            .ToList();
    }

    private static TableClient CreateHouseholdsTableClient()
    {
        var tableClient = CreateClient(DefaultTableName);
        tableClient.CreateIfNotExists();
        return tableClient;
    }

    private static TableClient CreateAuditTableClient()
    {
        var tableClient = CreateClient(AuditTableName);
        tableClient.CreateIfNotExists();
        return tableClient;
    }

    private static TableClient CreateClient(string tableName)
    {
        var connectionString = Environment.GetEnvironmentVariable("TableStorage__ConnectionString")
            ?? Environment.GetEnvironmentVariable("TableStorage:ConnectionString")
            ?? "UseDevelopmentStorage=true";

        return new TableClient(connectionString, tableName);
    }

    private static string EscapeFilter(string value) => value.Replace("'", "''");
}

public sealed class AuditLogEntry
{
    public string Id { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string HouseholdId { get; set; } = string.Empty;
    public string Actor { get; set; } = "system";
    public DateTimeOffset PerformedAt { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, string> Details { get; set; } = new();
}

public sealed class HouseholdTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string Email { get; set; } = string.Empty;
    public string HouseholdName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string AdultsJson { get; set; } = "[]";
    public string ChildrenJson { get; set; } = "[]";
    public string RsvpJson { get; set; } = "{}";

    public static HouseholdTableEntity FromHousehold(HouseholdAccount household)
    {
        return new HouseholdTableEntity
        {
            PartitionKey = "household",
            RowKey = household.Id,
            Email = household.Email,
            HouseholdName = household.HouseholdName,
            PasswordHash = household.PasswordHash,
            PasswordSalt = household.PasswordSalt,
            CreatedAt = household.CreatedAt,
            UpdatedAt = household.UpdatedAt,
            AdultsJson = JsonSerializer.Serialize(household.Adults),
            ChildrenJson = JsonSerializer.Serialize(household.Children),
            RsvpJson = JsonSerializer.Serialize(household.Rsvp)
        };
    }

    public HouseholdAccount ToHouseholdAccount()
    {
        var account = new HouseholdAccount
        {
            Id = RowKey,
            HouseholdName = HouseholdName,
            Email = Email,
            PasswordHash = PasswordHash,
            PasswordSalt = PasswordSalt,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            Adults = JsonSerializer.Deserialize<List<HouseholdGuest>>(AdultsJson) ?? new List<HouseholdGuest>(),
            Children = JsonSerializer.Deserialize<List<HouseholdGuest>>(ChildrenJson) ?? new List<HouseholdGuest>(),
            Rsvp = JsonSerializer.Deserialize<Dictionary<string, string>>(RsvpJson) ?? new Dictionary<string, string>()
        };

        if (string.IsNullOrEmpty(account.Id))
        {
            account.Id = RowKey;
        }

        return account;
    }
}

public sealed class AuditLogTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string Action { get; set; } = string.Empty;
    public string HouseholdId { get; set; } = string.Empty;
    public string Actor { get; set; } = "system";
    public DateTimeOffset PerformedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Details { get; set; } = string.Empty;
}
