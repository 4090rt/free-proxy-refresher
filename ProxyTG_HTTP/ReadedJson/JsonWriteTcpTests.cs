using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ModelData.JsonPtoxyTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ReadedJson
{
    public class JsonWriteTcpTests
    {
        private readonly ILogger<JsonWriteTcpTests> _logger;
        public static string _filename = "TestProxy_Settings.json";

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, object> _cache =
            new System.Collections.Concurrent.ConcurrentDictionary<Type, object>();

        public JsonWriteTcpTests(ILogger<JsonWriteTcpTests> logger)
        {
            _logger = logger;
        }

        public const string HttpMethod = "http";
        public const string TcpMethod = "tcp";

        public Task SaveMethod(string method)
        {
            string normalized = (method ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized != TcpMethod)
                normalized = HttpMethod;

            return WriteJsonMethod(normalized);
        }

        public async Task WriteJsonMethod(string jsonData)
        {
            if (string.IsNullOrEmpty(jsonData))
            {
                _logger.LogWarning("Передан пустое значение JSON.");
                return;
            }

            try
            {
                TestClient testClient1 = new TestClient()
                {
                    Method_Socks5_Test = jsonData
                };

                var jsonser2 = JsonSerializer.Serialize(testClient1);

                string tempFile = _filename + ".tmp";
                await File.WriteAllTextAsync(tempFile, jsonser2, new UTF8Encoding(false)).ConfigureAwait(false);
                File.Move(tempFile, _filename, true);

                _cache.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при записи JSON в файл.");
                return;
            }
        }

        public async Task<TestClient> ReadJsomethod()
        {
            if (_cache.TryGetValue(typeof(TestClient), out var cached) && cached != null)
                return (TestClient)cached;

            TestClient testClient;
            try
            {
                if (!File.Exists(_filename))
                    await WriteJsonMethod(HttpMethod).ConfigureAwait(false);

                using var fs = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new StreamReader(fs, new UTF8Encoding(false));
                string readed = await reader.ReadToEndAsync().ConfigureAwait(false);

                testClient = JsonSerializer.Deserialize<TestClient>(readed);

                // нормализуем значение, а не структуру JSON:
                // ToLowerInvariant на всей строке ломает имя свойства,
                // и System.Text.Json молча вернёт null
                if (testClient.Method_Socks5_Test != null)
                {
                    string normalized = testClient.Method_Socks5_Test.Trim().ToLowerInvariant();
                    testClient.Method_Socks5_Test = normalized == TcpMethod ? TcpMethod : HttpMethod;
                }
                else
                {
                    testClient.Method_Socks5_Test = HttpMethod;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при чтении JSON из файла.");
                return new TestClient { Method_Socks5_Test = HttpMethod };
            }

            _cache[typeof(TestClient)] = testClient;
            return testClient;
        }
    }
}
