using System.IO;
using Microsoft.Extensions.Configuration;

static class Config
{
    public static IConfiguration AppSetting { get; }

    static Config()
    {
        AppSetting = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
    }
}