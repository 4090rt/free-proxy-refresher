using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData.JsonDataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ReadedJson
{
    public static class ReadAndDeserializeJson
    {
        public static string filename = "appsettings.json";
        private static readonly ConcurrentDictionary<Type,object> _cache = new ConcurrentDictionary<Type,object>();

        public static async Task<T> MethodJson<T>() where T : new()
        {
            if (_cache.TryGetValue(typeof(T), out var cached))
                return (T)cached;

            T resylt;
            try
            {
                var json = System.IO.File.ReadAllText(filename);
                resylt = JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                if (_cache.TryGetValue(typeof(T), out var old) && old is T oldVal)
                    return oldVal;

                throw new InvalidDataException($"Не удалось прочитать/десериализовать {filename}");
            }

            _cache[typeof(T)] = resylt;
            return resylt;
        }
        public static void ResetCache() => _cache.Clear();

        public static T MethodJsonSync<T>() where T : new() => MethodJson<T>().GetAwaiter().GetResult();
    }
}
