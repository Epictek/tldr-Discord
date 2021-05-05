using System.IO;
using Microsoft.Extensions.Configuration;

internal static class Config
{
    static Config()
    {
        AppSetting = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
    }

    public static IConfiguration AppSetting { get; }
}