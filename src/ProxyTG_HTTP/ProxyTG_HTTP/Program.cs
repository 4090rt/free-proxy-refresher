using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.DataBase.CreateTable;
using ProxyTG_HTTP.HTTP.HTTPClientSettings;
using ProxyTG_HTTP.ModelData;
using System.Text.Json;

class Program
{ 
    public Program() { }

    public async Task Main(string[] args)
    {
        var servise = new ServiceCollection();

        var json = File.ReadAllText("appsettings.json");
        var jsonallDes = JsonSerializer.Deserialize<JsonDatStruct>(json);

        servise.AddLogging(build =>
        {
            build.AddConsole();
            build.SetMinimumLevel(ParseLogLevel(jsonallDes.LogLevel.Default));

            build.AddFilter("Microsoft", ParseLogLevel(jsonallDes.LogLevel.Default));
        });

        servise.AddHttpClient<Client_GIT_MTProto>();
        servise.AddHttpClient<Client_GIT_HTTP>();
        servise.AddHttpClient<Client_ALL>();

        var serviceProvider = servise.BuildServiceProvider();

        var client1 = serviceProvider.GetRequiredService<Client_GIT_MTProto>();
        var client2 = serviceProvider.GetRequiredService<Client_GIT_HTTP>();
        var client3 = serviceProvider.GetRequiredService<Client_ALL>();

        servise.AddScoped<TableForLog>();
    }

    private static LogLevel ParseLogLevel(string level)
    {
        try
        {
            return level?.ToLower() switch
            {
                "trace" => LogLevel.Trace,
                "debug" => LogLevel.Debug,
                "information" => LogLevel.Information,
                "warning" => LogLevel.Warning,
                "error" => LogLevel.Error,
                "critical" => LogLevel.Critical,
                "none" => LogLevel.None,
                _ => LogLevel.Information
            };
        }
        catch (Exception ex)
        { 
            Console.WriteLine(ex.Message.ToString() + ex.StackTrace.ToString());
            return new LogLevel();
        }
    }
}