using LoginPortal.Server.Models;
using LoginPortal.Server.Services.Interfaces;
using System.Data.SqlClient;

namespace LoginPortal.Server.Services
{
    public sealed class MappingService : IMappingService
    {
        private readonly string? _connString;
        private readonly ILogger<MappingService> _logger;

        public MappingService(IConfiguration config, ILogger<MappingService> logger)
        {
            _connString = config.GetConnectionString("TCSWEB");
            _logger = logger;
        }

        private static (string keyCol, string valCol) Columns(string table) =>
            table switch
            {
                "COMPANY" => ("COMPANYKEY", "COMPANYNAME"),
                "MODULE" => ("MODULEURL", "MODULENAME"),
                _ => throw new ArgumentOutOfRangeException(nameof(table)),
            };

        public async Task<IDictionary<string,string>> GetCompaniesAsync() { return await ReadAsync("COMPANY"); }
        public async Task<IDictionary<string,string>> GetModulesAsync() { return await ReadAsync("MODULE"); }

        private async Task<IDictionary<string,string>> ReadAsync(string table)
        {
            var dict = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);

            await using var conn = new SqlConnection(_connString);
            await using var comm = new SqlCommand($"SELECT * FROM dbo.{table}", conn);
            await conn.OpenAsync();

            string keyCol, valCol;
            (keyCol, valCol) = Columns(table);

            await using var reader = await comm.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string? key = reader[keyCol]?.ToString();
                string? val = reader[valCol]?.ToString();
                Console.WriteLine($"key: {key}, value: {val}");
                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(val))
                {
                    dict[key!] = val!;
                }
            }

            return dict;
        }
    }
}
