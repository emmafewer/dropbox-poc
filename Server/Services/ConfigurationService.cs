namespace Server.Services;

public class ConfigurationService
{ 

    
    public static string GetConnectionString()
    {
        var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
        
        var connectionString = $"Host=localhost;port=5432;Username=postgres;Password={password};Database=postgres;Maximum Pool Size=200;";

        return connectionString;
    }
}