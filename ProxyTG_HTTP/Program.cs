using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.Cache;
using ProxyTG_HTTP.Controller;
using ProxyTG_HTTP.CreatePDF;
using ProxyTG_HTTP.DataBase.AddLog;
using ProxyTG_HTTP.DataBase.CreateTable;
using ProxyTG_HTTP.DataBase.DbPath;
using ProxyTG_HTTP.DataBase.GetAllLogsRequest;
using ProxyTG_HTTP.DataBase.LogRetention;
using ProxyTG_HTTP.DataBase.LogSaveClass;
using ProxyTG_HTTP.DataBase.PoolSQLiteConnection;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ExceptionBase.LogInfoANDLogWarn;
using ProxyTG_HTTP.HTTP.HTTPClientSettings;
using ProxyTG_HTTP.HTTP.HttpGet;
using ProxyTG_HTTP.HTTP.HttpGetProxys;
using ProxyTG_HTTP.HTTP.PingRequest;
using ProxyTG_HTTP.MailKit;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using ProxyTG_HTTP.ModelData.JsonPtoxyTest;
using ProxyTG_HTTP.ModelData.ParseData;
using ProxyTG_HTTP.ModelData.PingData;
using ProxyTG_HTTP.Parser;
using ProxyTG_HTTP.ReadedJson;
using ProxyTG_HTTP.SendToMail;
using ProxyTG_HTTP.TestMtproto;
using ProxyTG_HTTP.TestSocks5;
using ProxyTG_HTTP.TestSocks5.CheckTcpTunnel;
using ProxyTG_HTTP.TestSocks5.CheckWebProxy;
using SimpleW;
using SimpleW.Observability;
using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using static System.Net.WebRequestMethods;

class Program
{
    private static Microsoft.Extensions.Logging.ILogger<Program> _logger;
    public static int _ischekedd = 0;
    public static CancellationTokenSource? _cts;


    public static async Task Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        _logger = loggerFactory.CreateLogger<Program>();

        var servise = new ServiceCollection();

        var jsonallDes = await ReadAndDeserializeJson.MethodJson<JsonDatStruct>();

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
        new Client_GIT_SOCKS5(loggerFactory.CreateLogger<Client_GIT_SOCKS5>()).Client_SettingsGit_SOCKS5(servise);
        new Client_Git_Ping(loggerFactory.CreateLogger<Client_Git_Ping>()).Client_SettingsGit_Ping(servise);
        new Client_Google(loggerFactory.CreateLogger<Client_Google>()).Client_SettingsGoogle(servise);

        servise.AddScoped<TableForLog>();
        servise.AddScoped<AddNewLogs>();
        servise.AddScoped<SendLogToMail>();
        servise.AddScoped<CreateFile>();
        servise.AddScoped<MailKitClientYandex>();
        servise.AddScoped<AllLogsRequest>();
        servise.AddScoped<MailKitClient>();
        servise.AddScoped<ParseSocks5>();
        servise.AddScoped<ParseMTProto>();
        servise.AddScoped<GetProxys>();
        servise.AddScoped<RequestMTProto>();
        servise.AddScoped<RequestSocks5>();
        servise.AddScoped<StrategyClass>();
        servise.AddScoped<HttpClient_Git_MTProto>();
        servise.AddScoped<Fabric_GIT>();
        servise.AddScoped<StrategyGoogle>();
        servise.AddScoped<StrategyYandex>();
        servise.AddScoped<ConsolePing>();
        servise.AddScoped<PingToGit>();
        servise.AddScoped<PingToGoggle>();
        servise.AddScoped<MemoryCacheMTProtoList>();
        servise.AddScoped<MemoryCacheSocks5List>();
        servise.AddScoped<TestSocksClient>();
        servise.AddScoped<TestSocks5WebProxy>();
        servise.AddScoped<TestCicle>();
        servise.AddScoped<TestSocs5TunnelCreate>();
        servise.AddScoped<TestCircle>();
        servise.AddScoped<UserWebProxy>();
        servise.AddScoped<UseTcpTunnel>();
        servise.AddScoped<Strategy_TcpSocks5_Test, UserWebProxy>();
        servise.AddScoped<StrategyCLassTcp>();
        servise.AddScoped<MetodWebProxy>();
        servise.AddScoped<MetodTcpTunnel>();
        servise.AddScoped<Fabric_TcpSocks5_Test, MetodWebProxy>();
        servise.AddScoped<ProxyTestFactory>();
        servise.AddScoped<JsonWriteTcpTests>();
        servise.AddScoped<TestCircleMtproto>();
        servise.AddScoped<TestMtprtotoTunnel>();
        servise.AddMemoryCache();

        var serviceProvider = servise.BuildServiceProvider();

        ControllerGetAllProxy.Services = serviceProvider;

        var tableForLogFromDi = serviceProvider.GetRequiredService<TableForLog>();
        var serviceNewLog = serviceProvider.GetRequiredService<LogSave>();
        var servicedeletelogs = serviceProvider.GetRequiredService<DeleteOldLogs>();
        var pingConsole = serviceProvider.GetRequiredService<ConsolePing>();

        var getProxy = serviceProvider.GetRequiredService<Fabric_GIT>();

        //Cоздание таблицы
        await tableForLogFromDi.InithializateCreateTable().ConfigureAwait(false);

        //очистка старых сессий
        await servicedeletelogs.LogsDeleteMethod(jsonallDes.Logging.LogReterningDay.Day).ConfigureAwait(false);

        //запуск локального хоста
        await HostRun();

        // _logger.LogInformation("Cкрипт запущен");
        await serviceNewLog.SaveLog("Cкрипт запущен", DateTime.UtcNow.ToString());

        // _logger.LogInformation("Зависимости и сервисы загружены");
        await serviceNewLog.SaveLog("Зависимости и сервисы загружены", DateTime.UtcNow.ToString());

        // лист команд
        CommandList();
        Console.ForegroundColor = ConsoleColor.White;

        var cts = new CancellationTokenSource();

        await RequestPruxyMethod(serviceProvider).ConfigureAwait(false);

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
                    else if (input.ToLower() == "/socks5")
                    {
                        await SelectProxyTestMethod(serviceProvider).ConfigureAwait(false);
                    }

                    else if (input.ToLower() == "/check")
                    {
                        await StartProxyTestMethod(serviceProvider).ConfigureAwait(false);
                    }
                    else if (input.ToLower() == "/ping")
                    {
                        Task<DataPing> dataPingGit = PingToGitService(serviceProvider);
                        Task<DataPing> dataPingGoogle = PingToGoogleService(serviceProvider);

                        await Task.WhenAll(dataPingGit, dataPingGoogle).ConfigureAwait(false);

                        DataPing pingGit = await dataPingGit.ConfigureAwait(false);
                        DataPing pingGoogle = await dataPingGoogle.ConfigureAwait(false);

                        list.Add(pingGit);
                        list.Add(pingGoogle);

                        pingConsole.PrintPing(list);
                        list.Clear();
                    }
                    else if (input.ToLower() == "/stop")
                    {
                       var running = Volatile.Read(ref _cts);
                        if (running != null)
                        {
                            running.Cancel();
                            Console.WriteLine("Проверка прокси остановлена");
                        }
                        else
                        {
                            Console.WriteLine("Проверка прокси не запущена");
                        }
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
            catch (TaskCanceledException)
            {
                // _logger.LogWarning("Программа успешно завершена!");
                _proxyTimer?.Dispose();
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

            // _logger.LogInformation("Отправка логов на почту");
            await serviceNewLog.SaveLog("Отправка логов на почту", DateTime.UtcNow.ToString());

            // _logger.LogInformation("Выгружаю логи");
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
    public static async Task SelectProxyTestMethod(ServiceProvider serviceProvider)
    {
        try
        {
            var factory = serviceProvider.GetRequiredService<ProxyTestFactory>();
            var storage = serviceProvider.GetRequiredService<JsonWriteTcpTests>();
            var logSave = serviceProvider.GetRequiredService<LogSave>();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("            М Е Т О Д   П Р О В Е Р К И");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  1");
            Console.ResetColor();
            Console.WriteLine("  HTTP — через SocketsHttpHandler, api.ipify.org");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  2");
            Console.ResetColor();
            Console.WriteLine("  TCP  — SOCKS5 туннель, httpbin.org");

            Console.Write("  Выбор (1/2): ");

            string? input = await Console.In.ReadLineAsync().ConfigureAwait(false);

            if (!int.TryParse(input?.Trim(), out int choice) || choice < 1 || choice > 2)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  Введено не 1 и не 2 — выбор не изменён");
                Console.ResetColor();
                return;
            }

            string method = choice == 1 ? JsonWriteTcpTests.HttpMethod : JsonWriteTcpTests.TcpMethod;

            await storage.SaveMethod(method).ConfigureAwait(false);
            await logSave.SaveLog($"Метод проверки SOCKS5 выбран: {method}", DateTime.UtcNow.ToString()).ConfigureAwait(false);

            // читаем выбор обратно из файла: это единственный способ
            // убедиться, что сохранение реально дошло
            string savedMethod = (await storage.ReadJsomethod().ConfigureAwait(false)).Method_Socks5_Test;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"  Метод сохранён: {savedMethod}");
            Console.ResetColor();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Выбор метода проверки: " + ex.Message);
        }
        catch (Exception ex)
        {
            ExceptionLog.LogError(ex, _logger);
        }
    }

    public static async Task StartProxyTestMethod(ServiceProvider serviceProvider)
    {
        try
        {
            var factory = serviceProvider.GetRequiredService<ProxyTestFactory>();
            var storage = serviceProvider.GetRequiredService<JsonWriteTcpTests>();
            var logSave = serviceProvider.GetRequiredService<LogSave>();

            string savedMethod = (await storage.ReadJsomethod().ConfigureAwait(false)).Method_Socks5_Test;

            var tester = factory.FactoryMethod(savedMethod);

            if (Interlocked.CompareExchange(ref _ischekedd, 1, 0) == 1)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  Проверка уже идёт, дождитесь завершения");
                Console.ResetColor();
                return;
            }

            var ctstok = new CancellationTokenSource();
            Volatile.Write(ref _cts, ctstok);

            _ = Task.Run(async () =>
            {
                try
                {
                    TesteProxyModelCunt result = await tester.TestFactoryMethod().ConfigureAwait(false);
                    await PrintCheckResult(result, logSave).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Возникло исключени" + ex.Message + ex.StackTrace);
                }
                finally
                {
                    Volatile.Write(ref _cts, null);
                    Interlocked.Exchange(ref _ischekedd, 0);
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Выбор метода проверки: " + ex.Message);
        }
        catch (Exception ex)
        {
            ExceptionLog.LogError(ex, _logger);
        }
    }

    private static async Task PrintCheckResult(TesteProxyModelCunt result, LogSave logSave)
    {
        try
        {
            string message;

            if (result.Failed)
                message = "Проверка прервана с ошибкой, подробности в логе";
            else if (result.Tested == 0)
                message = "Нет SOCKS5-прокси для проверки";
            else if (result.Alive == 0)
                message = $"Проверено {result.Tested}, рабочих 0 — все прокси мертвы";
            else
                message = $"Проверка выполнена: проверено {result.Tested}, рабочих {result.Alive}";

            Console.WriteLine();
            Console.ForegroundColor = result.Failed ? ConsoleColor.Red
                : result.Alive > 0 ? ConsoleColor.Green : ConsoleColor.Yellow;
            Console.WriteLine("  " + message);
            Console.ResetColor();

            await logSave.SaveLog("Проверка SOCK5: " + message, DateTime.UtcNow.ToString()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ExceptionLog.LogError(ex, _logger);
        }
    }

    private static Microsoft.Extensions.Logging.LogLevel ParseLogLevel(string level)
    {
        try
        {
            return level?.ToLower() switch
            {
                "trace" => Microsoft.Extensions.Logging.LogLevel.Trace,
                "debug" => Microsoft.Extensions.Logging.LogLevel.Debug,
                "information" => Microsoft.Extensions.Logging.LogLevel.Information,
                "warning" => Microsoft.Extensions.Logging.LogLevel.Warning,
                "error" => Microsoft.Extensions.Logging.LogLevel.Error,
                "critical" => Microsoft.Extensions.Logging.LogLevel.Critical,
                "none" => Microsoft.Extensions.Logging.LogLevel.None,
                _ => Microsoft.Extensions.Logging.LogLevel.Information
            };
        }
        catch (Exception ex)
        { 
            Console.WriteLine(ex.Message.ToString() + ex.StackTrace.ToString());
            return new Microsoft.Extensions.Logging.LogLevel();
        }
    }

    public static void CommandList()
    {
        int width = 56;
        string line = new string('═', width);
        string commandName = "  /LogSend";
        string logsendDesc = "Выгрузить логи за текущую сессию";
        string pingDesc = "Замер пинга до Git и Google";
        string socks5Desc = "Выбор метода проверки прокси";
        string socks5Name = "  /socks5";
        string checkDesc = "Запустить проверку выбранным методом";
        string checkName = "  /check";

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(line);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("            Д О С Т У П Н Ы Е   К О М А Н Д Ы");

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(line);
        Console.ResetColor();

        WriteCommand(commandName, logsendDesc, width);
        WriteCommand("  /ping", pingDesc, width);
        WriteCommand(socks5Name, socks5Desc, width);
        WriteCommand(checkName, checkDesc, width);

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(line);
        Console.ResetColor();
    }

    private static void WriteCommand(string name, string description, int width)
    {
        int padding = width - name.Length - description.Length;
        if (padding < 1)
            padding = 1;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(name);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(new string(' ', padding));
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(description);
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
    public static Timer _proxyTimer = null!;

    public static async Task RequestPruxyMethod(ServiceProvider serviceProvider)
    {
        int _isRefreshing = 0;

        var getProxy = serviceProvider.GetRequiredService<Fabric_GIT>();
        var serviceNewLog = serviceProvider.GetRequiredService<LogSave>();
        var cacheMtpRoto = serviceProvider.GetRequiredService<MemoryCacheMTProtoList>();
        var cacheSocks5 = serviceProvider.GetRequiredService<MemoryCacheSocks5List>();
        try
        {
            // WarningAndInfoLog.LogInfo("Запускаю таймер обновления прокси (каждые 12 часов)", _logger);
            await serviceNewLog.SaveLog("Запускаю таймер обновления прокси (каждые 12 часов)", DateTime.UtcNow.ToString());
            using var cts = new CancellationTokenSource();


            _proxyTimer = new Timer(async _ =>
            {
                if (Interlocked.Exchange(ref _isRefreshing, 1) == 1)
                {
                    // WarningAndInfoLog.LogInfo("Обновление уже выполняется, пропускаем тик", _logger);
                    return;
                }

                try
                {
                    var mtProtoStrategy = getProxy.HttpGetPRoxysFabric("mtproto");
                    var socks5Strategy = getProxy.HttpGetPRoxysFabric("socks5");

                    Task<List<ProxyData>> mtProtoTask = mtProtoStrategy.HttpClients();
                    Task<List<ProxyData>> socks5Task = socks5Strategy.HttpClients();

                    await Task.WhenAll(mtProtoTask, socks5Task).ConfigureAwait(false);

                    List<ProxyData> mtProtoData = await mtProtoTask.ConfigureAwait(false);
                    List<ProxyData> socks5Data = await socks5Task.ConfigureAwait(false);

                    WarningAndInfoLog.LogInfo($"Получено прокси: MTProto {mtProtoData.Count}, SOCKS5 {socks5Data.Count}", _logger);
                    await serviceNewLog.SaveLog($"Получено прокси: MTProto {mtProtoData.Count}, SOCKS5 {socks5Data.Count}", DateTime.UtcNow.ToString());

                    List<HttpParse> socks5Parses = socks5Data.Select(p => new HttpParse { IP = p.Server, Port = p.Port, Version = p.Version, Date = p.Date }).ToList();
                    List<MtProtoParse> MTProtoParses = mtProtoData.Select(p => new MtProtoParse
                    { ServerKey = p.Server, PortKey = p.Port, SecretKey = p.Secret ?? string.Empty, Version = p.Version, Date = p.Date }).ToList();

                    cacheMtpRoto.Cache(MTProtoParses);
                    cacheSocks5.Cache(socks5Parses);

                    WarningAndInfoLog.LogInfo($"✅ Кэш обновлён: MTProto {MTProtoParses.Count}, SOCKS5 {socks5Parses.Count}", _logger);
                    await serviceNewLog.SaveLog($"Кэш обновлён: MTProto {MTProtoParses.Count}, SOCKS5 {socks5Parses.Count}", DateTime.UtcNow.ToString());
                }
                catch (Exception ex)
                {
                    ExceptionLog.LogError(ex, _logger);
                    await serviceNewLog.SaveLog("Ошибка обновления прокси: " + ex.Message, DateTime.UtcNow.ToString());
                }
                finally
                {
                    Interlocked.Exchange(ref _isRefreshing, 0);
                }

            }, null, TimeSpan.Zero, TimeSpan.FromHours(12));
        }
        catch (Exception ex)
        {
            ExceptionLog.LogError(ex, _logger);
            await serviceNewLog.SaveLog("Таймер обновления прокси не запущен: " + ex.Message, DateTime.UtcNow.ToString());
            _proxyTimer.Dispose();
        }
    }

    public static async Task HostRun()
    {
        try
        {
            Log.SetSink(log => Console.WriteLine($"{log.LocalTime:HH:mm:ss.fff} [{log.LevelSmiley}|{log.LevelName3Len}] {log.Source} {log.Message}" +
                $" {log.Exception?.Message} {log.Exception?.Source}"), SimpleW.Observability.LogLevel.Debug);

            var server = new SimpleWServer(IPAddress.Any, 2015);

            server.MapControllers<Controller>("/api");

            await server.StartAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ExceptionLog.LogError(ex, _logger);
        }
    }
}