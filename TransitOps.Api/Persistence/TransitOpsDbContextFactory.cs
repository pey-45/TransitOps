using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TransitOps.Api.Persistence;

public sealed class TransitOpsDbContextFactory : IDesignTimeDbContextFactory<TransitOpsDbContext>
{
    public TransitOpsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? BuildLocalConnectionString();
        var options = new DbContextOptionsBuilder<TransitOpsDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new TransitOpsDbContext(options);
    }

    private static string BuildLocalConnectionString()
    {
        var dotEnvPath = FindDotEnvPath()
            ?? throw new InvalidOperationException(
                "No se encontró .env. Créalo desde .env.example o define ConnectionStrings__DefaultConnection.");
        var values = File.ReadLines(dotEnvPath)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.Ordinal);

        return $"Host=localhost;Port={Required(values, "POSTGRES_PORT")};Database={Required(values, "POSTGRES_DB")};" +
               $"Username={Required(values, "POSTGRES_USER")};Password={Required(values, "POSTGRES_PASSWORD")}";
    }

    private static string? FindDotEnvPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(currentDirectory, ".env"),
            Path.GetFullPath(Path.Combine(currentDirectory, "..", ".env"))
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    private static string Required(IReadOnlyDictionary<string, string> values, string key)
    {
        var environmentValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(environmentValue)) return environmentValue;
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Falta {key} en el entorno y en .env.");
    }
}
