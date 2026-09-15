using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.ModelData.PingData
{
    public class ConsolePing
    {
        public void PrintPing(List<DataPing> dataPings)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("======================= ЗАМЕР ПИНГА =======================");
            Console.ResetColor();

            if (dataPings == null || dataPings.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  Нет данных о пинге");
                Console.ResetColor();
                return;
            }

            foreach (var item in dataPings)
            {
                PrintRow(item);
            }

            Console.ResetColor();
            Console.WriteLine();
        }

        private void PrintRow(DataPing item)
        {
            bool ok = item.PingMs >= 0;
            var pingColor = ok ? ConsoleColor.Green : ConsoleColor.Red;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  ────────────────────────────────────────────────");
            Console.ResetColor();

            Console.Write("  ХОСТ    : ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(item.Host);
            Console.ResetColor();

            Console.Write("  ПИНГ    : ");
            Console.ForegroundColor = pingColor;
            Console.WriteLine(ok ? $"{item.PingMs} ms" : "нет ответа / ошибка");
            Console.ResetColor();

            Console.Write("  СТАТУС  : ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(item.Status);
            Console.ResetColor();

            if (!string.IsNullOrEmpty(item.Error) && item.Error != "-")
            {
                Console.Write("  ОШИБКА  : ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(item.Error);
                Console.ResetColor();
            }
        }
    }
}