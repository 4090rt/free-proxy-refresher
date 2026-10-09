using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ModelData.ParseData;
using System.Buffers.Binary;
using System.Buffers;
using ProxyTG_HTTP.ExceptionBase;

namespace ProxyTG_HTTP.TestSocks5
{
    public class TestSocs5
    {
        private readonly ILogger<TestSocs5> _logger;

        public TestSocs5(ILogger<TestSocs5> logger)
        { 
            _logger = logger;
        }

        private static readonly ArrayPool<byte> pool = ArrayPool<byte>.Shared;

        public async Task<Stream?> Test(HttpParse httpParse, string targetHost = "httpbin.org", int targetPort = 80)
        {
            byte[] buffer = pool.Rent(15);         // заголовок ответа (4 байта)
            byte[] bufferFinally = pool.Rent(258); // хвост ответа: 1 (длина) + 255 (домен) + 2 (порт)
            TcpClient? client = null;

            try
            {
                client = new TcpClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(7));
                await client.ConnectAsync(httpParse.IP, int.Parse(httpParse.Port), cts.Token);

                client.NoDelay = true; //меньше задержка
                client.ReceiveTimeout = 5000;
                client.SendTimeout = 5000;

                Stream stream = client.GetStream();

                await stream.WriteAsync(new byte[] { 0x05, 0x01, 0x00 }, cts.Token).ConfigureAwait(false);
                await stream.ReadExactlyAsync(buffer.AsMemory(0, 2), cts.Token).ConfigureAwait(false);

                if (buffer[0] != 0x05)
                    throw new Exception("Неверная версия в ответе на приветствие");
                if (buffer[1] != 0x00)
                    throw new Exception($"Метод аутентификации отклонён: 0x{buffer[1]:X2}");

                byte[] packet = CreatePacket(targetHost, targetPort.ToString()); // куда идём через прокси

                //запрос на подключение
                await stream.WriteAsync(packet, cts.Token).ConfigureAwait(false);
                // читаем ответ
                await stream.ReadExactlyAsync(buffer.AsMemory(0,4), cts.Token).ConfigureAwait(false);

                if (buffer[0] != 0x05)
                    throw new Exception("Неверная версия в ответе");
                if (buffer[1] != 0x00) 
                    throw new Exception($"CONNECT отклонён: 0x{buffer[1]:X2}");

                int tailne = buffer[3] switch
                {
                    0x01 => 4 + 2,
                    0x04 => 16 + 2,
                    0x03 => await ReadDomainTailAsync(stream, cts.Token).ConfigureAwait(false),
                    _ => throw new Exception(" не известный тип адреса")
                };

                await stream.ReadExactlyAsync(bufferFinally.AsMemory(0, tailne), cts.Token);

                return stream; 
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                client?.Dispose(); 
                return null;
            }
            finally
            {
                pool.Return(buffer);
                pool.Return(bufferFinally);
            }
        }

        public byte[] CreatePacket(string host, string port)
        {
            try
            {
                byte[] packet = new byte[7 + host.Length];
                byte[] hostbytes = Encoding.ASCII.GetBytes(host);

                packet[0] = 0x05; // версия протокола sock
                packet[1] = 0x01; // команда connect открытие соединения
                packet[2] = 0x00; // зарезервированное поле
                packet[3] = 0x03; // тип адреса 0x3 домен 0x4 ipv6 0x1 Ipv4
                packet[4] = (byte)host.Length; // длина домена
                hostbytes.CopyTo(packet.AsSpan(5));// домен — было packet[5..]: срез массива делает копию, запись уходила в пустоту
                BinaryPrimitives.WriteUInt16BigEndian
                    (packet.AsSpan(^2), (ushort)int.Parse(port)); //порт приводим к 16 битному виду и заполняем последние 2 байта

                return packet;
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                throw;
            }
        }

        // отдельно дочитываем длину домена (1 байт), возвращаем остаток: домен + порт
        private static async ValueTask<int> ReadDomainTailAsync(Stream stream, CancellationToken token)
        {
            byte[] len = new byte[1];
            await stream.ReadExactlyAsync(len, token).ConfigureAwait(false);
            return len[0] + 2;
        }
    }
}
