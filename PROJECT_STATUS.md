# Статус проекта

Обзор от 11.10.2026. Сборка: **0 ошибок, 12 предупреждений** (все `CS86xx` — nullable).
Состояние: код собирается, Linux-публикация проверена. Осталась неготовность git-гигиены и пара
незначительных мелочей.

---

## 1. Что сделано

### Блок A — конфигурация и парсинг

- **A1** — ✅ `JsonLoggingData.PdfSaveFilePath` приведён к ключу `appsettings.json`
  `PdfSaveFilePath`; `SendLogToMail` читает `PdfSaveFilePath.FAILEPATH`. PDF из `/logsend`
  больше не падает на `Path.Combine(base, null)`.
- **A2** — ✅ `MemoryCacheSocks5List`: ключи кэша `const`; `/SOCKS5` при пустом
  проверенном кэше отдаёт пустой список и пишет warning.
- **A4** — ✅ `GetProxys.GetMethod`: при не-2xx/пустом теле возвращает пустой
  `ReadOnlyMemory<byte>`, `HttpRequestException` ловится отдельно.
- **A3** — ❌ `.gitignore` отсутствует (git пока не трогаем) — см. [§2](#2-блокер-git).

### Блок B — инфраструктура

- **B1** — ✅ `PoolSQLite`: условие занятости пула `_aviable?.Count + _InUse.Count < _maxcountPool`.
- **B2** — ✅ `AllLogsRequest`: убрано двойное закрытие соединения (закрытие только в `finally`).
- **B3** — ✅ `TestSocks5WebProxy` / `TestSocs5TunnelCreate`: `catch` больше не спамит в БД.
- **B4** — ✅ лёгкий вариант: кэши отдают `IReadOnlyList<T>`, контроллер возвращает
  `IReadOnlyList<T>` напрямую, `AllStrcut` хранит `IReadOnlyList<T>` — копий `.ToList()` нет.
- **B5** — ✅ ключи `MemoryCacheMTProtoList` (`cache_key`, `staleCachekey`) сделаны `const`.
- **B6** — ✅ `System.Data.SQLite.Core` восстановлен (пакет `SQLite` не даёт namespace
  `System.Data.SQLite`), `System.Net.Http` удалён.
- **B7** — ✅ csproj выровнен под Linux-Dockerfile: `RuntimeIdentifiers win-x64;linux-x64`,
  `ContainerRuntimeIdentifier linux-x64`, `ContainerBaseImage mcr.microsoft.com/dotnet/runtime:9.0`.
  Проверено `dotnet publish -r linux-x64` — успешно, нативные `libe_sqlite3.so` и
  `SQLite.Interop.dll` копируются. (Docker-демон в среде выключен, `docker build` не гонялся.)
- **B8** — ⏸️ не делаем: кэширование чтения `appsettings.json` в `ReadAndDeserializeJson`
  (не критично).

### Блок C — функциональность и документация

- **C3** — ✅ `GET /proxy/STATS` (`ControllerStats`: `ProxiesAll`, `ProxiesTested`, `DateTime`).
- **C5** — ✅ свойство `TestClient.Method_Socks5_Test` и `TestProxy_Settings.json` приведены
  к одному имени (была поломка сборки).
- **C1** — ✅ лог прогресса в `TestCicle` вынесен из `Parallel.ForEachAsync` — пишется один раз
  после цикла (как в `TestCircle`).
- **C2** — ✅ единый `CancellationToken` протянут через всю цепочку проверки (см. §1.1).
- **C10** — ✅ документация (`README.md`, этот файл) обновлена под текущий код.
- **`/ALL`** — ✅ согласован с `/SOCKS5`: проверенный кэш есть → отдаём его (даже пустым),
  иначе сырьё.

### 1.1 Токен проверки (C2)

Интерфейсы и все звенья принимают `CancellationToken token = default`:

| Звено | Файл |
|---|---|
| `Fabric_TcpSocks5_Test.TestFactoryMethod` | `TestSocks5/Fabric_TcpSocks5_Test.cs:14` |
| `MetodWebProxy` / `MetodTcpTunnel` | `TestSocks5/Fabric_TcpSocks5_Test.cs:26,50` |
| `Strategy_TcpSocks5_Test.TestMethod` | `TestSocks5/Strategy_TcpSocks5_Test.cs:16` |
| `UserWebProxy` / `UseTcpTunnel` / `StrategyCLassTcp` | `TestSocks5/Strategy_TcpSocks5_Test.cs:30,54,78` |
| `TestCicle.TestMethod` | `TestSocks5/CheckWebProxy/TestCicle.cs:28` |
| `TestCircle.Circle` | `TestSocks5/CheckTcpTunnel/TestCircle.cs:30` |
| `TestSocks5WebProxy.TestProxy` / `TestSocs5TunnelCreate.Test` | соответствующие файлы |

- Токен уходит в `ParallelOptions.CancellationToken` и в сетевые вызовы.
- Внутри создаётся **связанный** источник: `CancellationTokenSource.CreateLinkedTokenSource(token)`
  + `CancelAfter(...)` на один прокси (10 с — HTTP, 7 с — TCP).
- Промежуточные `catch` фильтруют отмену (`when (ex is not OperationCanceledException)`),
  а циклы и низкоуровневые методы перебрасывают `OperationCanceledException` при отмене
  внешнего токена (таймаут одного прокси по-прежнему = «прокси мёртв»).
- ⚠️ Внешний токен пока никем не взводится: команда `/stop` — на стороне пользователя,
  `Program.cs` в рамках задачи не менялся.

---

## 2. Блокер: git

- **`.gitignore` отсутствует**.
- В индексе — служебные `.vs/`, `bin/`, `obj/`.
- **В рабочем дереве есть дублирующее дерево `src/ProxyTG_HTTP/ProxyTG_HTTP/`** — старая
  раскладка (в ней остались старые имена `PDFilepath`, `PathPDF`). Нужно удалить.
- В истории две структуры проекта.
- **Пароль почты в `appsettings.json`** (`StrategyMailKit`) попадал в коммит.

Git-история пока не переписывается (по договорённости).

---

## 3. Осталось

- **`Program.cs`** (по прежней договорённости не трогаем):
  - `CS8618` на `_logger` (`Program.cs:40`);
  - текст ошибки в `StartProxyTestMethod` пишет «Выбор метода проверки» вместо
    «Не удалось запустить проверку».
- **`/stop`** — не подключён (владелец токена в `Program.cs`).
- **Семафоры** `TestCicle`/`TestCircle` раздельные при общем флаге `_ischekedd` — гонка
  только при запуске в обход `Program`.
- **B8** — кэширование чтения `appsettings.json`.
- **git** — `.gitignore`, удаление `src/`, чистка истории, вынос пароля.

---

## 4. Что добавить (по желанию)

- **Прогресс проверки** — перезаписываемая строка раз в N прокси.
- **Ограничение отдачи**: `GET /proxy/SOCKS5?count=50`.
- **Автопроверка по таймеру** (сейчас только вручную через `/check`).

---

## 5. Проверка вручную

```
1. dotnet build -t:Rebuild                    → 0 ошибок
2. GET /api/proxy/MTPROTO, /SOCKS5, /ALL, /STATS → 200
3. /socks5 → метод сохранён в TestProxy_Settings.json, проверка не стартовала
4. /check  → проверка пошла в фоне
5. /check повторно → «Проверка уже идёт, дождитесь завершения»
6. по завершении → «Проверка выполнена: проверено N, рабочих M»
7. dotnet publish -r linux-x64                → успешно, нативка SQLite на месте
```

Пункты 2 и 4–6 вживую не прогонялись (в среде сборки нет доступа к публичным спискам/сети).
