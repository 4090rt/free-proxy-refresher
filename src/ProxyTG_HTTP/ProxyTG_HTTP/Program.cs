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
using ProxyTG_HTTP.HTTP.HttpGet;
using ProxyTG_HTTP.HTTP.HttpGetProxys;
using ProxyTG_HTTP.HTTP.PingRequest;
using ProxyTG_HTTP.MailKit;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using ProxyTG_HTTP.ModelData.ParseData;
using ProxyTG_HTTP.ModelData.PingData;
using ProxyTG_HTTP.Parser;
using ProxyTG_HTTP.SendToMail;
using System;
using System.Net;
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
            build.SetMinimumLevel(ParseLogLevel(jsonallDes.Logging.LogLevel.Default));

            build.AddFilter("Microsoft", ParseLogLevel(jsonallDes.Logging.LogLevel.Default));
        });
        servise.AddScoped<LogSave>();
        servise.AddScoped<DeleteOldLogs>();
        servise.AddScoped<PoolSQLite>();
        servise.AddScoped<DBPathCLass>();

        new Client_GIT_MTProto(loggerFactory.CreateLogger<Client_GIT_MTProto>()).Client_SettingsGit(servise);
        new Client_GIT_HTTP(loggerFactory.CreateLogger<Client_GIT_HTTP>()).Client_SettingsGit_HTTP(servise);
        new Client_Git_Ping(loggerFactory.CreateLogger<Client_Git_Ping>()).Client_SettingsGit_Ping(servise);
        new Client_Google(loggerFactory.CreateLogger<Client_Google>()).Client_SettingsGoogle(servise);

        servise.AddScoped<TableForLog>();
        servise.AddScoped<AddNewLogs>();
        servise.AddScoped<SendLogToMail>();
        servise.AddScoped<MailKitClientYandex>();
        servise.AddScoped<AllLogsRequest>();
        servise.AddScoped<MailKitClient>();
        servise.AddScoped<ParseHttp>();
        servise.AddScoped<ParseMTProto>();
        servise.AddScoped<GetProxys>();
        servise.AddScoped<RequestMTProto>();
        servise.AddScoped<RequestHttp>();
        servise.AddScoped<StrategyClass>();
        servise.AddScoped<HttpClient_Git_MTProto>();
        servise.AddScoped<HttpClient_Git_Http>();
        servise.AddScoped<Fabric_GIT>();
        servise.AddScoped<StrategyGoogle>();
        servise.AddScoped<StrategyYandex>();
        servise.AddScoped<ConsolePing>();
        servise.AddScoped<PingToGit>();
        servise.AddScoped<PingToGoggle>();

        var serviceProvider = servise.BuildServiceProvider();

        var tableForLogFromDi = serviceProvider.GetRequiredService<TableForLog>();
        var serviceNewLog = serviceProvider.GetRequiredService<LogSave>();
        var servicedeletelogs = serviceProvider.GetRequiredService<DeleteOldLogs>();
        var pingConsole = serviceProvider.GetRequiredService<ConsolePing>();

        var getProxy = serviceProvider.GetRequiredService<Fabric_GIT>();

        //Cоздание таблицы
        await tableForLogFromDi.InithializateCreateTable().ConfigureAwait(false);

        //очистка старых сессий
        await servicedeletelogs.LogsDeleteMethod(jsonallDes.Logging.LogReterningDay.Day).ConfigureAwait(false);

        _logger.LogInformation("Cкрипт запущен");
        await serviceNewLog.SaveLog("Cкрипт запущен", DateTime.UtcNow.ToString());

        _logger.LogInformation("Зависимости и сервисы загружены");
        await serviceNewLog.SaveLog("Зависимости и сервисы загружены", DateTime.UtcNow.ToString());

        // лист команд
        CommandList();
        Console.ForegroundColor = ConsoleColor.White;

        var cts = new CancellationTokenSource();

        try
        {
            List<DataPing> list = new List<DataPing>();
            _ = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    string? input = await Console.In.ReadLineAsync().ConfigureAwait(false);

                    if (input == null) { cts.Cancel(); break; }

                    else if (input.ToLower() == "/logsend")
                    {
                        await SendMail(serviceProvider).ConfigureAwait(false);
                    }
                    else if (input.ToLower() == "/ping")
                    {
                        var pingGit = await PingToGitService(serviceProvider).ConfigureAwait(false);
                        var pingGoogle = await PingToGoogleService(serviceProvider).ConfigureAwait(false);

                        list.Add(pingGit);
                        list.Add(pingGoogle);

                        pingConsole.PrintPing(list);
                    }
                    else
                    {
                        Console.WriteLine("Неизвестная команда");
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

        await RequestPruxyMethod(serviceProvider).ConfigureAwait(false);

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
        string line = new string('═', 46);
        string commandName = "  /LogSend";

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(line);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("            Д О С Т У П Н Ы Е   К О М А Н Д Ы");

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(line);
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(commandName);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(new string(' ', 46 - commandName.Length - "Выгрузить логи за текущую сессию".Length));
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Выгрузить логи за текущую сессию");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("  /ping");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(new string(' ', 46 - "  /ping".Length - "Замер пинга до Git и Google".Length));
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Замер пинга до Git и Google");

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(line);
        Console.ResetColor();
    }

    public static async Task<DataPing> PingToGoogleService(ServiceProvider serviceProvider)
    {
        try
        {
            var pingGoogle = serviceProvider.GetRequiredService<PingToGoggle>();

            if (pingGoogle == null)
                return new DataPing();

            DataPing ping = await pingGoogle.RequestPing().ConfigureAwait(false);
            return ping;
        }
        catch (Exception ex)
        {
            ExceptionLog.LogError(ex, _logger);
            return new DataPing();
        }
    }
    public static async Task<DataPing> PingToGitService(ServiceProvider serviceProvider)
    {
        try
        {
            var pingGit= serviceProvider.GetRequiredService<PingToGit>();

            if (pingGit == null)
                return new DataPing();

            DataPing ping = await pingGit.RequestPing().ConfigureAwait(false);
            return ping;
        }
        catch (Exception ex)
        {
            ExceptionLog.LogError(ex, _logger);
            return new DataPing();
        }
    }

    public static async Task RequestPruxyMethod(ServiceProvider serviceProvider)
    {
        var getProxy = serviceProvider.GetRequiredService<Fabric_GIT>();
        var serviceNewLog = serviceProvider.GetRequiredService<LogSave>();
        try
        {
            var timer = new System.Threading.Timer(async _ =>
            {
                var strategy1 = getProxy.HttpGetPRoxysFabric("mtproto");
                var strategy2 = getProxy.HttpGetPRoxysFabric("http");

                List<ProxyData> strategy1Go = await strategy1.HttpClients().ConfigureAwait(false);
                List<ProxyData> strategy2Go = await strategy2.HttpClients().ConfigureAwait(false);

            }, null, TimeSpan.Zero, TimeSpan.FromHours(12));
        }
        catch (Exception ex)
        {
            _logger.LogError("Таймер выбросил исключение" + ex.Message);
            await serviceNewLog.SaveLog("Асинхронная задача выкинула исключение:" + ex.Message, DateTime.UtcNow.ToString());
        }
    }
}