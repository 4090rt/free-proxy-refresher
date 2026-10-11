# ProxyTG_HTTP

Агрегатор **SOCKS5** и **MTProto** (Telegram) прокси с встроенным кэшированием, REST-API, проверкой доступности SOCKS5, логированием и отправкой логов на почту.

Скрипт периодически скачивает публичные списки прокси, парсит их, хранит в кэше и раздаёт по HTTP. Если источник прокси недоступен — отдаёт «старые» (stale) данные, а не пустые списки.

> Поддержка **HTTP-прокси удалена**. Проект работает только с SOCKS5 и MTProto. Класс `HttpParse` — это модель SOCKS5-прокси (имя историческое).

---

## Оглавление

- [Требования](#требования)
- [Быстрый старт](#быстрый-старт)
- [Конфигурация](#конфигурация)
- [REST API](#rest-api)
- [Консольные команды](#консольные-команды)
- [Проверка SOCKS5](#проверка-socks5)
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
      "Socks5": "https://raw.githubusercontent.com/monosans/proxy-list/main/proxies/socks5.txt"
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
| `Logging.LogReterningDay.Day` | Сколько дней хранить логи в БД (удаляются при старте) | `1` |
| `Logging.StrategyMailKit.Strategy` | Почтовый провайдер для `/logsend`: `Google` или `Yandex` | `Google` |
| `Logging.StrategyMailKit.Mail` | Email отправителя | `you@gmail.com` |
| `Logging.StrategyMailKit.Password` | Пароль / app-password для SMTP | `abcd efgh ijkl mnop` |
| `Logging.StrategyMailKit.recipientsemail` | Email получателя логов | `receiver@example.com` |
| `Logging.PingToSerivceURL.Google` | URL для пинга до Google (`/ping`) | `https://www.google.com/` |
| `Logging.PingToSerivceURL.GIT` | URL для пинга до GitHub (`/ping`) | `https://api.github.com` |
| `Logging.PdfSaveFilePath.FAILEPATH` | Имя папки/файла для PDF-отчёта (относительно папки приложения) | `LogsFile` |
| `Logging.TestProxyUrl.Url` | URL, по которому проверяется живость SOCKS5-прокси | `https://api.ipify.org` |

> **Примечание по почте.** Gmail требует включённый двухфакторный вход и Application password (не обычный пароль). Для Yandex — пароль приложения в настройках безопасности аккаунта.

---

## REST API

Сервер слушает порт `2015` (адрес `0.0.0.0`, только IPv4). Все маршруты смонтированы под префиксом `/api`.

| Метод | Путь | Описание |
|---|---|---|
| `GET` | `/api/proxy/MTPROTO` | Список MTProto-прокси |
| `GET` | `/api/proxy/SOCKS5` | Проверенные SOCKS5 (если проверка была), иначе сырьё |
| `GET` | `/api/proxy/ALL` | MTProto + SOCKS5 |
| `GET` | `/api/proxy/STATS` | Сводка: сколько сырья, сколько проверено, время последней проверки |

> При обращении из локальной сети используйте `127.0.0.1`, а не `localhost` — сервер слушает IPv4, а `localhost` может резолвиться в IPv6 (`::1`).

### Пример: получить MTProto-прокси

```bash
curl http://127.0.0.1:2015/api/proxy/MTPROTO
```

Ответ — элементы `MtProtoParse` (для подключения через Telegram: `server, port, secret`):

```json
[
  {
    "ServerKey": "185.76.151.150",
    "PortKey": "443",
    "SecretKey": "bfefffffffffffffffffffffffffffff",
    "Date": "",
    "Version": ""
  }
]
```

Example подключения в клиенте Telegram: `185.76.151.150:443` с secret `bfefffffffffffffffffffffffffffff`.

### Пример: получить SOCKS5-прокси

```bash
curl http://127.0.0.1:2015/api/proxy/SOCKS5
```

Элементы `HttpParse` (`IP`, `Port`, `Date`, `Version`):

```json
[
  {
    "IP": "5.45.126.128",
    "Port": "8080",
    "Date": "",
    "Version": ""
  },
  {
    "IP": "80.253.246.238",
    "Port": "6618",
    "Date": "",
    "Version": ""
  }
]
```

### Пример: все списки сразу

```bash
curl http://127.0.0.1:2015/api/proxy/ALL
```

Ответ (`AllStrcut`):

```json
{
  "listMTPRoto": [ { "ServerKey": "185.76.151.150", "PortKey": "443", "SecretKey": "...", "Date": "", "Version": "" } ],
  "listSocks5": [ { "IP": "5.45.126.128", "Port": "8080", "Date": "", "Version": "" } ]
}
```

### Пример: статистика

```bash
curl http://127.0.0.1:2015/api/proxy/STATS
```

Ответ (`ControllerStats`):

```json
{
  "ProxiesAll": 1234,
  "ProxiesTested": 87,
  "DateTime": "2026-10-10T12:34:56.789Z"
}
```

### Поведение при пустом / устаревшем кэше

- Есть свежие данные → возвращаются свежие.
- Свежих нет, есть «старые» (stale) → возвращаются stale (данные не старше ~13–14 часов).
- Нет вообще → пустой JSON-массив `[]`, в консоль пишется предупреждение.

**`/SOCKS5` и проверенный кэш.** Если проверка SOCKS5 уже выполнялась, `/SOCKS5` и `/ALL` отдают **только проверенный список**. Если проверка завершилась с нулём живых прокси, отдаётся пустой список `[]` (а не сырьё) — это осознанно: «живых нет» — тоже результат. Сырьё отдаётся только пока проверки ещё не было.

---

## Консольные команды

| Команда | Описание |
|---|---|
| `/logsend` | Выгрузить логи текущей сессии из БД и отправить на почту (HTML-таблица + PDF-вложение) |
| `/ping` | Замерить пинг до Google и GitHub, вывести таблицу |
| `/socks5` | Выбрать метод проверки SOCKS5 (сохраняет выбор, саму проверку не запускает) |
| `/check` | Запустить проверку выбранным методом (в фоне) |

Пример:

```text
/LogSend    Выгрузить логи за текущую сессию
/ping       Замер пинга до Git и Google
/socks5     Выбор метода проверки прокси
/check      Запустить проверку выбранным методом
```

---

## Проверка SOCKS5

Проверка доступности SOCKS5-прокси запускается вручную командой `/check` по методу, выбранному в `/socks5`. Выбор хранится в файле `TestProxy_Settings.json` (`Method_Socks5_Test`).

Доступны два метода:

| # | Метод | Как проверяет |
|---|---|---|
| 1 | `http` | HTTP-запрос через `SocketsHttpHandler` (прокси-клиент) на `Logging.TestProxyUrl.Url` |
| 2 | `tcp` | Ручное SOCKS5-рукопожатие и CONNECT-туннель к `httpbin.org:80`, проверка ответа `HTTP/1.1 200` |

Цепочка вызовов:

```
Fabric_TcpSocks5_Test.TestFactoryMethod
  └─ Strategy_TcpSocks5_Test.TestMethod
       └─ TestCicle.TestMethod / TestCircle.Circle   (Parallel.ForEachAsync, MaxDegreeOfParallelism = 10)
            └─ TestSocks5WebProxy.TestProxy / TestSocs5TunnelCreate.Test
  → MemoryCacheSocks5List.CacheTested(...)  → /SOCKS5
```

Все звенья принимают `CancellationToken token = default` и прокидывают его вниз до `ParallelOptions.CancellationToken` и сетевых вызовов; внутри создаётся связанный `CancellationTokenSource` (`CreateLinkedTokenSource`) с таймаутом на один прокси (10 с для HTTP-метода, 7 с для TCP).

Результат проверки — `TesteProxyModelCunt { Tested, Alive, Failed }`; `Program.cs` печатает один из четырёх взаимоисключающих исходов:

| Условие | Сообщение |
|---|---|
| `Failed` | Проверка прервана с ошибкой, подробности в логе |
| `Tested == 0` | Нет SOCKS5-прокси для проверки |
| `Alive == 0` | Проверено N, рабочих 0 — все прокси мертвы |
| иначе | Проверка выполнена: проверено N, рабочих M |

От двойного запуска защищает статический флаг `_ischekedd` (захват через `Interlocked.CompareExchange` до `Task.Run`, сброс в `finally`).

---

## Как это работает

```
                     ┌────────────────────────────┐
                     │  appsettings.json          │
                     └─────────────┬──────────────┘
                                   ▼
   Timer (каждые 12 ч, старт сразу)
                   │
                   ▼
   Скачивание списков прокси (Git, HTTP-клиенты с Polly)
                   │
                   ▼
   Парсинг:  ip:port строки → HttpParse (SOCKS5)
             │  t.me/proxy?server=… строки → MtProtoParse
                   │
                   ▼
   Кэш (IMemoryCache): fresh (13 ч) + stale (14 ч)
                   │
                   ▼
   REST API: /proxy/MTPROTO, /proxy/SOCKS5, /proxy/ALL, /proxy/STATS
                   │
   Проверка SOCKS5 (/check) → проверенный кэш (13 ч) → /proxy/SOCKS5
```

Отдельные подсистемы:

- **Таймер** — обновление прокси с защитой от наложенных запусков (при уже идущем обновлении тик пропускается).
- **Кэш** — «свежие» и «старые» данные; при падении/недоступности источника прокси сервис продолжает отдавать последние сохранённые.
- **Проверка SOCKS5** — ручной прогон через `/check`, результат кладётся в отдельный проверенный кэш.
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
- ход и итог проверки SOCKS5;
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
│   ├── CheckedSocks5.cs            # Модель проверенного списка (Proxies + CheckedAtUtc)
│   ├── MemoryCacheMTProtoList.cs   # Кэш MTProto-прокси (fresh + stale)
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
│   ├── HttpGetProxys/              # Фабрика и стратегии запросов прокси
│   └── PingRequest/                # Пинг до Git и Google
├── MailKit/                        # SMTP-клиенты и фабрика стратегий (Google/Yandex)
├── ModelData/                      # Модели (JSON-конфиг, прокси, пинг, логи, статистика)
├── Parser/
│   ├── ParseSocks5.cs              # SOCKS5-прокси → HttpParse
│   └── ParseMTProto.cs             # t.me/proxy?... строки → MtProtoParse
├── ReadedJson/
│   ├── ReadAndDeserializeJson.cs   # Чтение appsettings.json
│   └── JsonWriteTcpTests.cs        # Чтение/запись выбранного метода проверки
├── SendToMail/
│   └── SendLogToMail.cs            # Оркестрация: логи → PDF → почта
└── TestSocks5/                     # Проверка SOCKS5 (HTTP- и TCP-методы)
    ├── Fabric_TcpSocks5_Test.cs    # Интерфейс фабрики + реализации методов
    ├── Strategy_TcpSocks5_Test.cs  # Интерфейс стратегии + звенья цепочки
    ├── CheckWebProxy/              # HTTP-метод (TestCicle, TestSocks5WebProxy, TestSocksClient)
    └── CheckTcpTunnel/             # TCP-метод (TestCircle, TestSocs5TunnelCreate)
```

---

## Стек и библиотеки

| Пакет | Назначение |
|---|---|
| `.NET 9` | Платформа |
| `SimpleW` | Лёгкий HTTP-сервер |
| `MailKit` / `MimeKit` | Отправка почты по SMTP |
| `Microsoft.Extensions.*` | Logging, DI, HttpClient, MemoryCache |
| `Polly` | Retry / circuit breaker для HTTP-клиентов |
| `System.Data.SQLite` (`System.Data.SQLite.Core`) | База данных |
| `TerraPDF` | Генерация PDF-отчётов |

---

## Запуск как служба

Программа рассчитана на запуск в консоли (читать команды). Для фоновой работы можно использовать:

- Windows — Task Scheduler / NSSM.
- Linux — `systemd` unit / `screen` / `tmux`.

Замечание: при запуске без консольного ввода (stdin = EOF) программа завершается сразу — это ожидаемое поведение интерактивного приложения.
