# ProxyTG_HTTP

Агрегатор **HTTP / HTTPS / SOCKS5 / MTProto (Telegram) / ShadowSocks** прокси с встроенным кэшированием, REST-API, проверкой доступности и отправкой логов на почту.

Скрипт периодически скачивает публичные списки прокси, парсит их, хранит в кэше и раздаёт по HTTP. Если источник прокси недоступен — отдаёт «старые» (stale) данные, а не пустые списки.

> Поддерживаемые протоколы: HTTP, HTTPS, SOCKS5, MTProto, ShadowSocks.

---

## Оглавление

- [Требования](#требования)
- [Быстрый старт](#быстрый-старт)
- [Конфигурация](#конфигурация)
- [REST API](#rest-api)
- [Консольные команды](#консольные-команды)
- [Проверка прокси](#проверка-прокси)
- [Как это работает](#как-это-работает)
- [Кэширование](#кэширование)
- [Логирование и база данных](#логирование-и-база-данных)
- [Отправка логов на почту](#отправка-логов-на-почту)
- [Структура проекта](#структура-проекта)
- [Стек и библиотеки](#стек-и-библиотеки)

---

## Требования

| Требование | Версия |
|---|---|
| .NET SDK | 9.0 |
| ОС | Windows / Linux / macOS |

Проверка версии SDK:

```bash
dotnet --version
```

## Быстрый старт

```bash
# 1. Склонировать репозиторий
git clone <url-репозитория>
cd ProxyTG_HTTP

# 2. Убедиться, что конфигурация заполнена (см. ниже)
#    Открыть ProxyTG_HTTP/appsettings.json

# 3. Запустить
dotnet run --project ProxyTG_HTTP
```

При первом старте приложение:

1. Читает `appsettings.json`.
2. Создаёт базу данных и таблицу логов (если их нет).
3. Очищает логи старше заданного периода (`LogReterningDay.Day`).
4. Поднимает HTTP-сервер на порту **2015**.
5. Сразу запускает первый цикл обновления прокси, далее — каждые **12 часов**.

Программа остаётся в консоли и принимает команды (см. [Консольные команды](#консольные-команды)).

---

## Конфигурация

Все настройки находятся в файле `ProxyTG_HTTP/appsettings.json` (копируется в выходную папку при сборке).

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },

    "ProxySources": {
      "MTProto": "https://raw.githubusercontent.com/SoliSpirit/mtproto/master/all_proxies.txt",
      "Socks5": "https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/socks5.txt",
      "ShadowSocks": "https://raw.githubusercontent.com/10ium/V2Hub3/refs/heads/main/Split/Normal/shadowsocks",
      "Http": "https://raw.githubusercontent.com/databay-labs/free-proxy-list/master/http.txt",
      "Https": "https://raw.githubusercontent.com/databay-labs/free-proxy-list/master/https.txt"
    },

    "LogReterningDay": {
      "Day": 1
    },

    "StrategyMailKit": {
      "Strategy": "Google",
      "Mail": "you@gmail.com",
      "Password": "app_password",
      "recipientsemail": "receiver@example.com"
    },

    "PingToSerivceURL": {
      "Google": "https://www.google.com/",
      "GIT": "https://api.github.com"
    },

    "PdfSaveFilePath": {
      "FAILEPATH": "LogsFile"
    },

    "TestProxyUrl": {
      "Url": "https://api.ipify.org"
    }
  }
}
```

### Описание полей

| Поле | Описание | Пример |
|---|---|---|
| `Logging.LogLevel.Default` | Уровень логирования (`Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`, `None`) | `Information` |
| `Logging.ProxySources.MTProto` | URL списка MTProto-прокси (TXT, `t.me/proxy?...` на строку) | `https://.../all_proxies.txt` |
| `Logging.ProxySources.Socks5` | URL списка SOCKS5-прокси (TXT, формат `ip:port`) | `https://.../socks5.txt` |
| `Logging.ProxySources.ShadowSocks` | URL списка ShadowSocks-прокси (JSON/лист) | `https://.../shadowsocks` |
| `Logging.ProxySources.Http` | URL списка HTTP-прокси (TXT, `ip:port`) | `https://.../http.txt` |
| `Logging.ProxySources.Https` | URL списка HTTPS-прокси (TXT, `ip:port`) | `https://.../https.txt` |
| `Logging.LogReterningDay.Day` | Сколько дней хранить логи в БД (удаляются при старте) | `1` |
| `Logging.StrategyMailKit.Strategy` | Почтовый провайдер для `/logsend`: `Google` или `Yandex` | `Google` |
| `Logging.StrategyMailKit.Mail` | Email отправителя | `you@gmail.com` |
| `Logging.StrategyMailKit.Password` | Пароль / app-password для SMTP | `abcd efgh ijkl mnop` |
| `Logging.StrategyMailKit.recipientsemail` | Email получателя логов | `receiver@example.com` |
| `Logging.PingToSerivceURL.Google` | URL для пинга до Google (`/ping`) | `https://www.google.com/` |
| `Logging.PingToSerivceURL.GIT` | URL для пинга до GitHub (`/ping`) | `https://api.github.com` |
| `Logging.PdfSaveFilePath.FAILEPATH` | Имя папки/файла для PDF-отчёта (относительно папки приложения) | `LogsFile` |
| `Logging.TestProxyUrl.Url` | URL, по которому проверяется живость прокси | `https://api.ipify.org` |

> **Примечание по почте.** Gmail требует включённый двухфакторный вход и Application password (не обычный пароль). Для Yandex — пароль приложения в настройках безопасности аккаунта.

---

## REST API

Сервер слушает порт `2015` (адрес `0.0.0.0`, только IPv4). Все маршруты смонтированы под префиксом `/proxy`.

| Метод | Путь | Описание |
|---|---|---|
| `GET` | `/proxy/MTPROTO` | Список MTProto-прокси |
| `GET` | `/proxy/SOCKS5` | Проверенные SOCKS5 (если проверка была), иначе сырьё |
| `GET` | `/proxy/SHADOWSOCKS` | Проверенные ShadowSocks (если проверка была), иначе сырьё |
| `GET` | `/proxy/HTTP` | Проверенные HTTP-прокси (если проверка была), иначе сырьё |
| `GET` | `/proxy/HTTPS` | Проверенные HTTPS-прокси (если проверка была), иначе сырьё |
| `GET` | `/proxy/ALL` | Все списки вместе |
| `GET` | `/proxy/STATS` | Сводка: сколько сырья, сколько проверено, время последней проверки |

> При обращении из локальной сети используйте `127.0.0.1`, а не `localhost` — сервер слушает IPv4, а `localhost` может резолвиться в IPv6 (`::1`).

### Пример: получить HTTP-прокси

```bash
curl http://127.0.0.1:2015/proxy/HTTP
```

Ответ — элементы `HttpParse` (`HostHttp`, `PortHttp`, `DateGiveHttp`):

```json
[
  {
    "HostHttp": "5.45.126.128",
    "PortHttp": "8080",
    "DateGiveHttp": "2026-..."
  }
]
```

### Пример: получить HTTPS-прокси

```bash
curl http://127.0.0.1:2015/proxy/HTTPS
```

Ответ — элементы `HttpsParse` (`HostHttp`, `PortHttp`, `DateGiveHttp`).

### Пример: все списки сразу

```bash
curl http://127.0.0.1:2015/proxy/ALL
```

Ответ (`AllStrcut`):

```json
{
  "listMTPRoto": [ { "ServerKey": "...", "PortKey": "443", "SecretKey": "...", "Date": "", "Version": "" } ],
  "listSocks5": [ { "IP": "5.45.126.128", "Port": "8080", "Date": "", "Version": "" } ],
  "listShadowSocks": [ ... ],
  "listHTTP": [ { "HostHttp": "...", "PortHttp": "8080", "DateGiveHttp": "..." } ],
  "listHTTPS": [ { "HostHttp": "...", "PortHttp": "443", "DateGiveHttp": "..." } ]
}
```

### Пример: статистика

```bash
curl http://127.0.0.1:2015/proxy/STATS
```

Ответ (`ControllerStats`):

```json
[
  { "ProxiesAll": 1234, "ProxiesTested": 87, "DateTime": "2026-..." },
  { "ProxiesAll": 500, "ProxiesTested": 120, "DateTime": "2026-..." }
]
```

### Поведение при пустом / устаревшем кэше

- Есть свежие данные → возвращаются свежие.
- Свежих нет, есть «старые» (stale) → возвращаются stale (данные не старше ~13–14 часов).
- Нет вообще → пустой JSON-массив `[]`, в консоль пишется предупреждение.

**Проверенный кэш.** Если проверка протокола уже выполнялась, соответствующий маршрут (`/HTTP`, `/HTTPS`, `/SOCKS5`, `/SHADOWSOCKS`) отдаёт **только проверенный список**. Если проверка завершилась с нулём живых прокси, отдаётся пустой список `[]`. Сырьё отдаётся только пока проверки ещё не было.

---

## Консольные команды

| Команда | Описание |
|---|---|
| `/logsend` | Выгрузить логи текущей сессии из БД и отправить на почту (HTML-таблица + PDF-вложение) |
| `/ping` | Замерить пинг до Google и GitHub, вывести таблицу |
| `/socks5` | Выбрать метод проверки SOCKS5 (сохраняет выбор, саму проверку не запускает) |
| `/check` | Запустить проверку всех протоколов (MTProto, SOCKS5, ShadowSocks, HTTP, HTTPS) в фоне |

Пример:

```text
/LogSend    Выгрузить логи за текущую сессию
/ping       Замер пинга до Git и Google
/socks5     Выбор метода проверки прокси
/check      Запустить проверку выбранным методом
```

От двойного запуска одной и той же проверки защищает статический флаг (захват через `Interlocked.CompareExchange` до `Task.Run`, сброс в `finally`).

---

## Проверка прокси

Проверка запускается командой `/check`. Каждый протокол проверяется независимо, результат кладётся в отдельный проверенный кэш и через 13 часов истекает.

### HTTP-прокси

TCP-подключение к прокси → `CONNECT <цель>:<порт> HTTP/1.1` → чтение заголовков ответа. Успех — статус `2xx` (`HTTP/1.1 200 ...` или `HTTP/1.0 200 ...`). Тестовый 호ст/порт цели по умолчанию `httpbin.org:80`.

### HTTPS-прокси

TCP-подключение к прокси → `SslStream.AuthenticateAsClient` (реальный TLS-хендшейк, проверка сертификата отключена, т.к. у бесплатных прокси чаще всего самоподписанные сертификаты) → внутри TLS отправляется `CONNECT <цель>:<порт> HTTP/1.1` → чтение ответа через `SslStream`. Успех — статус `2xx`. Тестовый хост/порт цели по умолчанию `httpbin.org:443`.

Ответ читается до первого `\r\n\r\n` одним `ReadAsync` (ответ на `CONNECT` не имеет тела), затем парсится статус-строка: `head.StartsWith("HTTP/")` и код из `200..299`.

### Цепочка вызовов (HTTP / HTTPS)

```
Timer / /check
  └─ TestCircleHTTP / TestCIrcleHTTPS
       └─ Parallel.ForEachAsync (MaxDegreeOfParallelism = 10)
            └─ TestHTTPTunnel.TunnelCreate / TestHTTPSTunnel.TunnelCreate
                 → MemoryCacheHttp.CacheUpdated / MemoryCacheHTTPS.CacheUpdate → /proxy/HTTP, /proxy/HTTPS
```

Все звенья принимают `CancellationToken` и прокидывают его вниз до сетевых вызовов; внутри создаётся связанный `CancellationTokenSource` (`CreateLinkedTokenSource`) с таймаутом на один прокси (7 с для HTTP, 10 с для HTTPS).

Результат проверки — `TesteProxyModelCunt { Tested, Alive, Failed }`.

---

## Как это работает

```
                      ┌────────────────────────────┐
                      │  appsettings.json          │
                      └─────────────┬──────────────┘
                                    ▼
             Timer (каждые 12 ч, старт сразу)  и  команда /check
                                    │
                                    ▼
       Скачивание списков прокси (HTTP-клиенты с Polly, по одному клиенту на протокол)
                                    │
                                    ▼
   Парсинг строк → HttpParse / HttpsParse / MtpParse / MtProtoParse / ShadowSocksParse
                                    │
                                    ▼
           Кэш (IMemoryCache): fresh (13 ч) + stale (14 ч) + проверенный (13 ч)
                                    │
                                    ▼
   REST API: /proxy/MTPROTO, /proxy/SOCKS5, /proxy/SHADOWSOCKS, /proxy/HTTP, /proxy/HTTPS, /proxy/ALL, /proxy/STATS
                                    │
              Проверка (/check) → проверенный кэш (13 ч) → маршруты /proxy/*
```

Отдельные подсистемы:

- **Таймер** — обновление прокси с защитой от наложенных запусков (при уже идущем обновлении тик пропускается).
- **Кэш** — «свежие» и «старые» данные; при падении/недоступности источника прокси сервис продолжает отдавать последние сохранённые.
- **Проверка прокси** — ручной прогон через `/check`, результат кладётся в отдельный проверенный кэш.
- **Логи** — каждое значимое событие пишется в консоль и в SQLite.
- **Пинг** — замер задержек до Google и GitHub по команде `/ping`.

---

## Кэширование

| Ключ | Назначение | Время жизни |
|---|---|---|
| MTProto свежий | Актуальный список MTProto-прокси | absolute 13 ч / sliding 13 ч |
| MTProto stale | «Запасной» список MTProto-прокси | absolute 13 ч / sliding 14 ч |
| SOCKS5 свежий | Актуальный список SOCKS5-прокси | absolute 13 ч / sliding 13 ч |
| SOCKS5 stale | «Запасной» список SOCKS5-прокси | absolute 13 ч / sliding 14 ч |
| SOCKS5 проверенный | Список живых SOCKS5 после `/check` (`CheckedSocks5`) | absolute 13 ч |
| ShadowSocks свежий | Актуальный список ShadowSocks-прокси | absolute 13 ч / sliding 13 ч |
| ShadowSocks stale | «Запасной» список ShadowSocks-прокси | absolute 13 ч / sliding 14 ч |
| ShadowSocks проверенный | Список живых ShadowSocks после `/check` (`ChekedShadow`) | absolute 13 ч |
| HTTP свежий | Актуальный список HTTP-прокси | absolute 13 ч / sliding 13 ч |
| HTTP stale | «Запасной» список HTTP-прокси | absolute 13 ч / sliding 14 ч |
| HTTP проверенный | Список живых HTTP после `/check` (`ChekedHTTP`) | absolute 13 ч |
| HTTPS свежий | Актуальный список HTTPS-прокси | absolute 13 ч / sliding 13 ч |
| HTTPS stale | «Запасной» список HTTPS-прокси | absolute 13 ч / sliding 14 ч |
| HTTPS проверенный | Список живых HTTPS после `/check` (`CheckHTTPS`) | absolute 13 ч |

Срок жизни свежих данных (13 ч) больше периода таймера (12 ч), поэтому контроллер почти всегда отдаёт актуальные данные без «просадки» в stale между обновлениями.

---

## Логирование и база данных

- **Консоль** — стандартный логгер `Microsoft.Extensions.Logging`.
- **SQLite** — таблица `LogBase (Log TEXT, Date TEXT)`. Файл БД создаётся автоматически.
- **Хранение** — логи только за период, заданный в `LogReterningDay.Day`; при старте записи старше указанного срока удаляются.
- **Пул соединений** — до 10 подключений, с `busy_timeout`, соединения возвращаются в пул после использования.

События, которые логируются:

- запуск/завершение скрипта;
- старт таймера, каждый цикл обновления прокси;
- количество полученных и отданных прокси;
- ход и итог проверки прокси;
- ошибки (с трейсом);
- отправка писем, очистка логов, пинг.

---

## Отправка логов на почту

Команда `/logsend`:

1. Читает все логи из БД (`AllLogsRequest`).
2. Формирует **PDF-отчёт** (таблица `Log | Time`) через `CreateFile`, сохраняет в папке из `PdfSaveFilePath.FAILEPATH`.
3. Собирает письмо: HTML-таблица + PDF-вложение.
4. Отправляет через выбранную стратегию (`Google` или `Yandex`) по данным SMTP.

Если логов нет или PDF не создался — письмо всё равно уходит, только без вложения.

---

## Структура проекта

```
ProxyTG_HTTP/
├── Program.cs                      # Главный класс: DI, таймер, сервер, команды
├── appsettings.json                # Конфигурация
├── TestProxy_Settings.json         # Выбранный метод проверки SOCKS5
├── Cache/
│   ├── ChekedHTTP.cs               # Модель проверенного HTTP-списка (Proxies + CheckedAtUtc)
│   ├── CheckHTTPS.cs               # Модель проверенного HTTPS-списка (Proxies + CheckedAtUtc)
│   ├── CheckedSocks5.cs            # Модель проверенного SOCKS5-списка (Proxies + CheckedAtUtc)
│   ├── ChekedMtproto.cs            # Модель проверенного MTProto-списка (Proxies + CheckedAtUtc)
│   ├── ChekedShadow.cs             # Модель проверенного ShadowSocks-списка (Proxies + CheckedAtUtc)
│   ├── MemoryCacheHttp.cs          # Кэш HTTP-прокси (fresh + stale + проверенный)
│   ├── MemoryCacheHTTPS.cs         # Кэш HTTPS-прокси (fresh + stale + проверенный)
│   ├── MemoryCacheMTProtoList.cs   # Кэш MTProto-прокси (fresh + stale)
│   ├── MemoryCacheShadowSoscks.cs  # Кэш ShadowSocks-прокси (fresh + stale + проверенный)
│   └── MemoryCacheSocks5List.cs    # Кэш SOCKS5-прокси (fresh + stale + проверенный)
├── Controller/
│   └── ControllerGetAllProxy.cs    # REST-контроллер /proxy/*
├── CreatePDF/
│   └── CreateFile.cs               # Генерация PDF-отчёта (TerraPDF)
├── DataBase/
│   ├── AddLog/AddNewLogs.cs        # INSERT в LogBase
│   ├── CreateTable/TableForLog.cs  # Создание таблицы
│   ├── DbPath/DBPathCLass.cs       # Путь к БД
│   ├── GetAllLogsRequest/AllLogsRequest.cs  # SELECT всех логов
│   ├── LogRetention/DeleteOldLogs.cs        # Удаление старых логов
│   ├── LogSaveClass/LogSave.cs     # Обёртка сохранения лога
│   └── PoolSQLiteConnection/PoolSQLite.cs   # Пул SQLite-соединений
├── ExceptionBase/                  # Логирование исключений (ExceptionLog и др.)
├── HTTP/
│   ├── HTTPClientSettings/         # Настройки HTTP-клиентов (Git, Google, Ping)
│   ├── HttpGet/GetProxys.cs        # Скачивание списков
│   ├── HttpGetProxys/              # Фабрика и стратегии запросов прокси (в т.ч. HttpAndHttps)
│   └── PingRequest/                # Пинг до Git и Google
├── MailKit/                        # SMTP-клиенты и фабрика стратегий (Google/Yandex)
├── ModelData/                      # Модели (JSON-конфиг, прокси, пинг, логи, статистика)
│   ├── ParseData/
│   │   ├── HttpParse.cs            # Модель HTTP-прокси (HostHttp, PortHttp, DateGiveHttp)
│   │   ├── HttpsParse.cs           # Модель HTTPS-прокси (HostHttp, PortHttp, DateGiveHttp)
│   │   ├── MtpParse.cs             # Модель SOCKS5-прокси (IP, Port, Version, Date)
│   │   ├── MtProtoParse.cs         # Модель MTProto-прокси (ServerKey, PortKey, SecretKey…)
│   │   ├── ShadowSocksParse.cs
│   │   └── ProxyRecord.cs          # Базовый интерфейс парсинга
│   ├── UpdateData/
│   │   ├── ChekedHTTP.cs / CheckHTTPS.cs
│   │   └── …                       # Модели проверенных списков
│   └── JsonPtoxyTest/
│       └── TesteProxyModelCunt.cs  # Результат проверки (Tested, Alive, Failed)
├── Parser/
│   ├── ParseHttpandHttps.cs        # Парсинг HTTP/HTTPS-прокси из TXT-листов
│   ├── ParseSocks5.cs              # SOCKS5-прокси → HttpParse
│   └── ParseMTProto.cs             # t.me/proxy?… строки → MtProtoParse
├── ReadedJson/
│   ├── ReadAndDeserializeJson.cs   # Чтение appsettings.json
│   └── JsonWriteTcpTests.cs        # Чтение/запись выбранного метода проверки
├── SendToMail/
│   └── SendLogToMail.cs            # Оркестрация: логи → PDF → почта
├── TestHTTP/                       # Проверка HTTP-прокси (CONNECT-туннель)
│   ├── TestHTTPTunnel.cs           # Один прокси: TCP connect → CONNECT → чтение статуса 2xx
│   └── TestCircleHTTP.cs           # Массовая проверка списка (Parallel.ForEachAsync)
├── TestHTTPS/                      # Проверка HTTPS-прокси (TLS → CONNECT-туннель)
│   ├── TestHTTPSTunnel.cs          # Один прокси: SslStream → CONNECT → чтение статуса 2xx
│   └── TestCIrcleHTTPS.cs          # Массовая проверка списка (Parallel.ForEachAsync)
├── TestMtproto/                    # Проверка MTProto-прокси
└── TestSocks5/                     # Проверка SOCKS5-прокси (HTTP- и TCP-методы)
    ├── Fabric_TcpSocks5_Test.cs
    ├── Strategy_TcpSocks5_Test.cs
    ├── CheckWebProxy/              # HTTP-метод
    └── CheckTcpTunnel/             # TCP-метод
```

---

## Стек и библиотеки

| Пакет | Назначение |
|---|---|
| `.NET 9` | Платформа |
| `SimpleW` | Лёгкий HTTP-сервер |
| `MailKit` / `MimeKit` | Отправка почты по SMTP |
| `Microsoft.Extensions.*` | Logging, DI, HttpClient, MemoryCache |
| `Polly` | Retry для HTTP-клиентов |
| `Microsoft.Extensions.Http.Resilience` | Таймауты и повторы HTTP |
| `System.Data.SQLite` (`System.Data.SQLite.Core`) | База данных |
| `TerraPDF` | Генерация PDF-отчётов |

---

## Запуск как служба

Программа рассчитана на запуск в консоли (читать команды). Для фоновой работы можно использовать:

- Windows — Task Scheduler / NSSM.
- Linux — `systemd` unit / `screen` / `tmux`.

Замечание: при запуске без консольного ввода (stdin = EOF) программа завершается сразу — это ожидаемое поведение интерактивного приложения.
