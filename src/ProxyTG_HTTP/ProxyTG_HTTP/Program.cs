using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.DataBase.AddLog;
using ProxyTG_HTTP.DataBase.CreateTable;
using ProxyTG_HTTP.DataBase.DbPath;
using ProxyTG_HTTP.DataBase.GetAllLogsRequest;
using ProxyTG_HTTP.DataBase.LogRetention;
using ProxyTG_HTTP.DataBase.LogSaveClass;
using ProxyTG_HTTP.DataBase.PoolSQLiteConnection;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.HTTP.HTTPClientSettings;
using ProxyTG_HTTP.MailKit;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using ProxyTG_HTTP.SendToMail;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

class Program
{
    private static ILogger<Program> _logger;
    public static async Task Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        _logger = loggerFactory.CreateLogger<Program>();

        var servise = new ServiceCollection();

        var json = File.ReadAllText("appsettings.json");
        var jsonallDes = JsonSerializer.Deserialize<JsonDatStruct>(json);

        servise.AddLogging(build =>
        {
            build.AddConsole();
            build.SetMinimumLevel(ParseLogLevel(jsonallDes.LogLevel.Default));

            build.AddFilter("Microsoft", ParseLogLevel(jsonallDes.LogLevel.Default));
        });
        servise.AddScoped<LogSave>();
        servise.AddScoped<DeleteOldLogs>();
        servise.AddScoped<PoolSQLite>();
        servise.AddScoped<DBPathCLass>();
        servise.AddHttpClient<Client_GIT_MTProto>();
        servise.AddHttpClient<Client_GIT_HTTP>();
        servise.AddHttpClient<Client_ALL>();
        servise.AddScoped<TableForLog>();
        servise.AddScoped<Client_GIT_MTProto>();
        servise.AddScoped<Client_GIT_HTTP>();
        servise.AddScoped<Client_ALL>();
        servise.AddScoped<AddNewLogs>();
        servise.AddScoped<SendLogToMail>();
        servise.AddScoped<MailKitClientYandex>();
        servise.AddScoped<AllLogsRequest>();
        servise.AddScoped<MailKitClient>();

        var serviceProvider = servise.BuildServiceProvider();

        var tableForLogFromDi = serviceProvider.GetRequiredService<TableForLog>();
        var serviceNewLog = serviceProvider.GetRequiredService<LogSave>();
        var servicedeletelogs = serviceProvider.GetRequiredService<DeleteOldLogs>();

        servise.AddScoped<TableForLog>();

        //Cоздание таблицы
        await tableForLogFromDi.InithializateCreateTable().ConfigureAwait(false);

        //очистка старых сессий
        await servicedeletelogs.LogsDeleteMethod(jsonallDes.Retention.Day).ConfigureAwait(false);

        _logger.LogInformation("Cкрипт запущен");
        await serviceNewLog.SaveLog("Cкрипт запущен", DateTime.UtcNow.ToString());

        _logger.LogInformation("Зависимости и сервисы загружены");
        await serviceNewLog.SaveLog("Зависимости и сервисы загружены", DateTime.UtcNow.ToString());

        _logger.LogInformation("Локальная база данных инициализирована");
        await serviceNewLog.SaveLog("Локальная база данных инициализирована", DateTime.UtcNow.ToString());

        // лист команд
        CommandList();
        Console.ForegroundColor = ConsoleColor.White;

        var cts = new CancellationTokenSource();

        try
        {
            _ = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    string? input = await Console.In.ReadLineAsync().ConfigureAwait(false);

                    if (input == null) { cts.Cancel(); break; }

                    else if (input == "/LogSend")
                    {
                        await SendMail(serviceProvider).ConfigureAwait(false);
                    }
                }
            });

            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning("Прогамме успешно завершена!" + ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Асинхронная задача выкинула исключение:" + ex.Message);
            await serviceNewLog.SaveLog("Асинхронная задача выкинула исключение:" + ex.Message, DateTime.UtcNow.ToString());
            cts.Cancel();   
        }
    }

    public static async Task SendMail(ServiceProvider serviceProvider)
    {
        try
        {
            var serviceNewLog = serviceProvider.GetRequiredService<LogSave>();

            _logger.LogInformation("Отправка логов на почту");
            await serviceNewLog.SaveLog("Отправка логов на почту", DateTime.UtcNow.ToString());

            _logger.LogInformation("Выгружаю логи");
            await serviceNewLog.SaveLog("Выгружаю логи", DateTime.UtcNow.ToString());

            var SendMethod = serviceProvider.GetRequiredService<SendLogToMail>();

            if (SendMethod == null)
                return;

            await SendMethod.ToSendMail().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ExceptionLog.LogError(ex, _logger);
        }
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

    public async Task RequestProxy()
    {
        try
        {

        }
        catch (HttpRequestException ex)
        {
            ExceptionLog.LogError(ex,_logger);
        }
    }

    public static void CommandList()
    {
        string CommandSendLogs = "Выгрузить логи за текущую сессию - /LogSend";
        string CommandPing = "Замер пинга до Git и Google - /ping";

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(CommandSendLogs);

        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine(CommandPing);
    }
}