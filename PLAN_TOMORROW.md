# План на завтра

## Блок 1 — Контроллеры → ровно 3 маршрута
`Controller\ControllerGetAllProxy.cs`

| Действие | Маршрут |
|---|---|
| удалить | `/HTTP` (`:59-80`) |
| удалить | `/SOCKS5_Tested` (`:105-127`) |
| удалить | `/HTTPandMTProto` (`:163-191`) |
| оставить | `/MTPROTO`, `/SOCKS5`, `/ALL` |

Плюс убрать поле `_memoryCacheHttpList` (`:24`) и его `GetRequiredService` (`:31`) — читать его больше негде.

`/SOCKS5` — двухпозиционный, отдаёт проверенные, если они есть:
```csharp
CheckedSocks5? checked_ = _memorySocks5List.GetTestedEntry();
return checked_ != null && checked_.Proxies.Count > 0
    ? checked_.Proxies          // проверка была
    : _memorySocks5List.Get();  // иначе сырьё
```

`/ALL` — только `listMTPRoto` + `listSocks5`. В `ModelData\ParseData\AllStrcut.cs:11` удалить `listHTTP`.

## Блок 2 — Удалить HTTP совсем
HTTP-прокси больше не читает никто (проверка SOCKS5 их не использует: `TestSocks5WebProxy.cs:38` зовёт `_client.TestClient(proxy)`).

- `Program.cs:426,430,436,442,447` — `httpStrategy`, `httpTask`, `httpData`, `httpParses`, `cacheHttp.Cache`
- `Program.cs:405` — `var cacheHttp = ...`
- `Program.cs:93` — `servise.AddScoped<MemoryCacheHttpList>()`
- Логи `Program.cs:440,452` — убрать HTTP из счётчиков
- Удалить `Cache\MemoryCacheHttpList.cs`

## Блок 3 — Обе проверки пишут в один ключ
Сейчас `TestCicle` → `CacheTested` → `cache_key_tested`, `TestCircle` → `CachedTestSocks5_2` → `cache_key_tested2`. Раз `/SOCKS5` отдаёт проверенные — ключ один.

- `Cache\CheckedSocks5.cs` — добавить `public int TotalCount { get; set; }` (сколько проверено всего)
- `Cache\MemoryCacheSocks5List.cs:83` — `CacheTested(alive, total)`, пишет `TotalCount`
- `TestCicle.cs:53` и `TestCircle.cs:56` — оба зовут `CacheTested(list, listProxy.Count)`
- Удалить `cache_key_tested2` (`:19`), `CachedTestSocks5_2` (`:135`), `GetTestSocks5_2` (`:160`)

**Вопрос:** `CacheTested` пишет с `AbsoluteExpiration 3h`, сырьё живёт 13h. Выровнять до 13h? Иначе через 3 часа `/SOCKS5` молча снова вернёт сырьё.

## Блок 4 — Проверка возвращает счётчик
Сигнатуры `Task` → `Task<CheckResult>`, цепочка из 6 методов:
```
Fabric_TcpSocks5_Test.TestFactoryMethod()   + new struct CheckResult(int Tested, int Alive)
  MetodWebProxy / MetodTcpTunnel
    UserWebProxy.TestMethod() / UseTcpTunnel.TestMethod()
      TestCicle.TestMethod() / TestCircle.Circle()
```
Побочный эффект в кэше для счётчика не годится — на пустом списке он не запишется, и прочитается результат прошлого прогона.

Здесь же фиксится молчаливый no-op: `Get()` **никогда не возвращает null** (`MemoryCacheSocks5List.cs:74` отдаёт пустой список). Проверки `TestCicle.cs:36` (`if (listProxy != null)`) и `TestCircle.cs:38` (`if (listProxy == null)`) — мёртвые. Станет `Count > 0`, пусто → `Tested == 0` → консоль скажет об этом.

## Блок 5 — `/socks5`: метод + необязательный запуск
```
  М Е Т О Д   П Р О В Е Р К И
  1  HTTP — SocketsHttpHandler, api.ipify.org
  2  TCP  — SOCKS5 туннель, httpbin.org
  Выбор (1/2): 2
  Метод сохранён: tcp
  Запустить проверку сейчас? (д/н): н
  Проверка не запущена, метод сохранён
```

При `д`:
- перед стартом: `if (_testCicle.IsRunning || _testCircle.IsRunning) → "Проверка уже идёт, дождитесь завершения"` и выход
- `TestCircle` **не имеет `IsRunning`** (`TestCicle.cs:25` есть) — добавить зеркально
- запуск через `Task.Run` + вывод по завершении, чтобы консоль не блокировалась на 20–35 минут:
```csharp
_ = Task.Run(async () => {
    CheckResult r = await tester.TestFactoryMethod();
    if (r.Tested == 0) Console.WriteLine("  Нет SOCKS5-прокси для проверки");
    else Console.WriteLine($"  Проверка выполнена: проверено {r.Tested}, рабочих {r.Alive}");
});
```
При `н` — ничего не запускается, файл уже сохранён.

## Блок 6 — Мелочи
- `TestCircle.cs:33` — `WaitAsync` перенести из `try` до него (как в `TestCicle.cs:29`), иначе `Release()` в `finally` вызывается без захвата
- `CommandList` (`Program.cs:316`) — описание `/socks5` под новый смысл

## Проверка
1. `dotnet build -t:Rebuild` → 0 ошибок
2. `GET /proxy/SOCKS5`, `/proxy/MTPROTO`, `/proxy/ALL` — только эти три живы, `/HTTP` и `/SOCKS5_Tested` → 404
3. `/socks5` → `2` → `н` → файл сохранён, проверка не стартовала
4. `/socks5` → `2` → `д` → повторный `/socks5` → «Проверка уже идёт»
5. Вывод по завершении «проверено N, рабочих M»

## Блок 7 — git (~15 минут)
- `git init` в `C:\пргаграмерование\ProxyTG_HTTP`
- `.gitignore`: `bin/`, `obj/`, `.vs/`, `*.db`, `*.user`, `*.tmp`, `TestProxy_Settings.json`
- Решить: разбивать ли первый коммит, где хранить пароль почты, публичный ли репозиторий

## Уже сделано
`await` + `ConcurrentBag` в обоих циклах, токены через `CreateLinkedTokenSource` (`TestSocks5WebProxy.cs:40-41`, `TestSocs5TunnelCreate.cs:31-32`), `head` = 12 байт. `JsonWriteTcpTests` читается и пишется, `ReadMethodAsync` удалён, нормализация идёт значением после десериализации. Фабрика `ProxyTestFactory` + выбор строкой. Контроллер очищен от `/SOCKS5_Check`, запуск только из консоли. `/socks5` читает файл обратно и передаёт в фабрику. Сборка 0 ошибок.

## Отложено
- `new CancellationTokenSource()` в обоих циклах никем не отменяется
- Ключи кэша `public string`, а не `readonly` — `MemoryCacheSocks5List.cs:16-19`
- Лог на мёртвый прокси `TestSocks5WebProxy.cs:49` — сотни записей в SQLite
- MTProto-парсер возвращает 0 при ответе 200

## Открытые вопросы
1. Выровнять ли `AbsoluteExpiration` для проверенных до 13h (см. Блок 3)?
2. Таймер 12ч — только сырьё обновляет или ещё гоняет проверку?
