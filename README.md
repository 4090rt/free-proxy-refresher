# ProxyTG_HTTP

Агрегатор **HTTP**, **SOCKS5** и **MTProto** (Telegram) прокси с встроенным кэшированием, REST-API, логированием и отправкой логов на почту.

Скрипт периодически скачивает публичные списки прокси, парсит их, хранит в кэше и раздаёт по HTTP. Если источник прокси недоступен — отдаёт «старые» (stale) данные, а не пустые списки.

---

## Оглавление

- [Требования](#требования)
- [Быстрый старт](#быстрый-старт)
- [Конфигурация](#конфигурация)
- [REST API](#rest-api)
- [Консольные команды](#консольные-команды)
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
      "HTTP": "https://raw.githubusercontent.com/proxygenerator1/ProxyGenerator/main/Stable/http.txt",
      "Socks5": "https://raw.githubusercontent.com/proxymint/free-proxy-list/main/proxies/socks5.txt"
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

    "PDFilepath": {
      "PathPDF": "LogsFile"
    }
  }
}
```

### Описание полей

| Поле | Описание | Пример |
|---|---|---|
| `Logging.LogLevel.Default` | Уровень логирования (`Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`, `None`) | `Information` |
| `Logging.ProxySources.MTProto` | URL списка MTProto-прокси (TXT, одна запись на строку) | `https://.../all_proxies.txt` |
| `Logging.ProxySources.HTTP` | URL списка HTTP-прокси (TXT, формат `ip:port`) | `https://.../http.txt` |
| `Logging.ProxySources.Socks5` | URL списка SOCKS5-прокси (TXT, формат `ip:port`) | `https://.../socks5.txt` |
| `Logging.LogReterningDay.Day` | Сколько дней хранить логи в БД (удаляются при старте) | `1` |
| `Logging.StrategyMailKit.Strategy` | Почтовый провайдер для `/logsend`: `Google` или `Yandex` | `Google` |
| `Logging.StrategyMailKit.Mail` | Email отправителя | `you@gmail.com` |
| `Logging.StrategyMailKit.Password` | Пароль / app-password для SMTP | `abcd efgh ijkl mnop` |
| `Logging.StrategyMailKit.recipientsemail` | Email получателя логов | `receiver@example.com` |
| `Logging.PingToSerivceURL.Google` | URL для пинга до Google (`/ping`) | `https://www.google.com/` |
| `Logging.PingToSerivceURL.GIT` | URL для пинга до GitHub (`/ping`) | `https://api.github.com` |
| `Logging.PDFilepath.PathPDF` | Имя папки/файла для PDF-отчёта (относительно папки приложения) | `LogsFile` |

> **Примечание по почте.** Gmail требует включённый двухфакторный вход и Application password (не обычный пароль). Для Yandex — пароль приложения в настройках безопасности аккаунта.

---

## REST API

Сервер слушает порт `2015` (адрес `0.0.0.0`, только IPv4).

| Метод | Путь | Описание |
|---|---|---|
| `GET` | `/api/proxy/HTTP` | Список HTTP-прокси |
| `GET` | `/api/proxy/MTPROTO` | Список MTProto-прокси |
| `GET` | `/api/proxy/SOCKS5` | Список SOCKS5-прокси |
| `GET` | `/api/proxy/HTTPandMTProto` | HTTP + MTProto |
| `GET` | `/api/proxy/ALL` | HTTP + MTProto + SOCKS5 |

> При обращении из локальной сети используйте `127.0.0.1`, а не `localhost` — сервер слушает IPv4, а `localhost` может резолвиться в IPv6 (`::1`).

### Пример: получить HTTP-прокси

```bash
curl http://127.0.0.1:2015/api/proxy/HTTP
```

Ответ:

```json
[
  {
    "IP": "1.32.48.243",
    "Port": "8081"
  },
  {
    "IP": "101.32.241.115",
    "Port": "8080"
  }
]
```

### Пример: получить MTProto-прокси

```bash
curl http://127.0.0.1:2015/api/proxy/MTPROTO
```

Ответ — элемент списка `MtProtoParse` (для подключения через Telegram: `server, port, secret`):

```json
[
  {
    "ServerKey": "185.76.151.150",
    "PortKey": "443",
    "SecretKey": "bfefffffffffffffffffffffffffffff"
  }
]
```

Example подключения в клиенте Telegram: `185.76.151.150:443` с secret `bfefffffffffffffffffffffffffffff`.

### Пример: получить SOCKS5-прокси

```bash
curl http://127.0.0.1:2015/api/proxy/SOCKS5
```

Список SOCKS5 отдаётся в том же формате, что и HTTP — `{ "IP": ..., "Port": ... }`:

```json
[
  {
    "IP": "5.45.126.128",
    "Port": "8080"
  },
  {
    "IP": "80.253.246.238",
    "Port": "6618"
  }
]
```

### Пример: все списки сразу

```bash
curl http://127.0.0.1:2015/api/proxy/ALL
```

Ответ:

```json
{
  "listHTTP": [ { "IP": "1.32.48.243", "Port": "8081" } ],
  "listMTPRoto": [ { "ServerKey": "185.76.151.150", "PortKey": "443", "SecretKey": "..." } ],
  "listSocks5": [ { "IP": "5.45.126.128", "Port": "8080" } ]
}
```

### Пример: оба списка (HTTP + MTProto)

```bash
curl http://127.0.0.1:2015/api/proxy/HTTPandMTProto
```

Ответ:

```json
{
  "listHTTP": [
    { "IP": "1.32.48.243", "Port": "8081" }
  ],
  "listMTPRoto": [
    { "ServerKey": "185.76.151.150", "PortKey": "443", "SecretKey": "..." }
  ]
}
```

### Поведение при пустом / устаревшем кэше

- Есть свежие данные → возвращаются свежие.
- Свежих нет, есть «старые» (stale) → возвращаются stale (данные не старше ~13–14 часов).
- Нет вообще → пустой JSON-массив `[]`, в консоль пишется предупреждение.

---

## Консольные команды

| Команда | Описание |
|---|---|
| `/logsend` | Выгрузить логи текущей сессии из БД и отправить на почту (HTML-таблица + PDF-вложение) |
| `/ping` | Замерить пинг до Google и GitHub, вывести таблицу |

Пример:

```text
/LogSend    Выгрузить логи за текущую сессию
/ping       Замер пинга до Git и Google
```

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
   Парсинг:  ip:port строки → HttpParse (HTTP и SOCKS5)
            │  t.me/proxy?server=… строки → MtProtoParse
                   │
                   ▼
   Кэш (IMemoryCache): fresh (13 ч) + stale (14 ч)
                   │
                   ▼
   REST API: /proxy/HTTP, /proxy/SOCKS5, /proxy/MTPROTO,
             /proxy/HTTPandMTProto, /proxy/ALL
```

Отдельные подсистемы:

- **Таймер** — обновление прокси с защитой от наложенных запусков (при уже идущем обновлении тик пропускается).
- **Кэш** — «свежие» и «старые» данные; при падении/недоступности источника прокси сервис продолжает отдавать последние сохранённые.
- **Логи** — каждое значимое событие пишется в консоль и в SQLite.
- **Пинг** — замер задержек до Google и GitHub по команде `/ping`.

---

## Кэширование

| Ключ | Назначение | Время жизни |
|---|---|---|
| HTTP свежий | Актуальный список HTTP-прокси | 13 ч absolute + sliding |
| HTTP stale | «Запасной» список HTTP-прокси | absolute 13 ч / sliding 14 ч |
| MTProto свежий | Актуальный список MTProto-прокси | 13 ч absolute + sliding |
| MTProto stale | «Запасной» список MTProto-прокси | absolute 13 ч / sliding 14 ч |
| SOCKS5 свежий | Актуальный список SOCKS5-прокси | 13 ч absolute + sliding |
| SOCKS5 stale | «Запасной» список SOCKS5-прокси | absolute 13 ч / sliding 14 ч |

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
- ошибки (с трейсом);
- отправка писем, очистка логов, пинг.

---

## Отправка логов на почту

Команда `/logsend`:

1. Читает все логи из БД (`AllLogsRequest`).
2. Формирует **PDF-отчёт** (таблица `Log | Time`) через `CreateFile`, сохраняет в папке из `PDFilepath.PathPDF`.
3. Собирает письмо: HTML-таблица + PDF-вложение.
4. Отправляет через выбранную стратегию (`Google` или `Yandex`) по данным SMTP.

Если логов нет или PDF не создался — письмо всё равно уходит, только без вложения.

---

## Структура проекта

```
ProxyTG_HTTP/
├── Program.cs                      # Главный класс: DI, таймер, сервер, команды
├── appsettings.json                # Конфигурация
├── Cache/
│   ├── MemoryCacheHttpList.cs      # Кэш HTTP-прокси (fresh + stale)
│   ├── MemoryCacheMTProtoList.cs   # Кэш MTProto-прокси (fresh + stale)
│   └── MemoryCacheSocks5List.cs    # Кэш SOCKS5-прокси (fresh + stale)
├── Controller/
│   └── ControllerGetAllProxy.cs    # REST-контроллер /proxy/*
├── CreatePDF/
│   └── CreateFile.cs               # Генерация PDF-отчёта (QuestPDF/TerraPDF)
├── DataBase/
│   ├── AddLog/AddNewLogs.cs        # INSERT в LogBase
│   ├── CreateTable/TableForLog.cs  # Создание таблицы
│   ├── DbPath/DBPathCLass.cs       # Путь к БД
│   ├── GetAllLogsRequest/AllLogsRequest.cs  # SELECT всех логов
│   ├── LogRetention/DeleteOldLogs.cs        # Удаление старых логов
│   ├── LogSaveClass/LogSave.cs     # Обёртка сохранения лога
│   └── PoolSQLiteConnection/PoolSQLite.cs   # Пул SQLite-соединений
├── ExceptionBase/
│   ├── ExceptionLog.cs             # Логирование исключений
│   ├── InvalidOperationLog.cs      # Логирование InvalidOperationException
│   └── LogInfoANDLogWarn/WarningAndInfoLog.cs  # Info/Warning логи
├── HTTP/
│   ├── HTTPClientSettings/         # Настройки HTTP-клиентов (Git, Google, Ping)
│   ├── HttpGet/GetProxys.cs        # Скачивание списков
│   ├── HttpGetProxys/              # Фабрика и стратегии запросов прокси
│   └── PingRequest/                # Пинг до Git и Google
├── MailKit/
│   ├── MailKitClient.cs            # SMTP Gmail
│   ├── MailKitClientYandex.cs      # SMTP Yandex
│   └── MailKitStrategyFactory.cs   # Фабрика почтовых стратегий
├── ModelData/                      # Модели (JSON-конфиг, прокси, пинг, логи)
├── Parser/
│   ├── ProxyLineParser.cs           # Общий парсер строк ip:port (HTTP + SOCKS5)
│   ├── ParseHttp.cs                 # HTTP-прокси → HttpParse
│   ├── ParseSocks5.cs               # SOCKS5-прокси → HttpParse
│   └── ParseMTProto.cs              # t.me/proxy?... строки → MtProtoParse
├── ReadedJson/
│   └── ReadAndDeserializeJson.cs   # Чтение и кэширование appsettings.json
└── SendToMail/
    └── SendLogToMail.cs            # Оркестрация: логи → PDF → почта
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
| `System.Data.SQLite` | База данных |
| `TerraPDF` | Генерация PDF-отчётов |

---

## Запуск как служба

Программа рассчитана на запуск в консоли (читать команды). Для фоновой работы можно использовать:

- Windows — Task Scheduler / NSSM.
- Linux — `systemd` unit / `screen` / `tmux`.

Замечание: при запуске без консольного ввода (stdin = EOF) программа завершается сразу — это ожидаемое поведение интерактивного приложения.