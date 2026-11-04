using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ModelData.ParseData;
using ProxyTG_HTTP.TestSocks5.CheckTcpTunnel;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ProxyTG_HTTP.TestShadowsocks
{
    public class TestShadowTunnelCreate
    {
        private string _UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

        public static readonly ArrayPool<byte> pool = ArrayPool<byte>.Shared;

        public byte[] _subkey;

        private static byte[] BuildNonce(int counter)
        {
            byte[] n = new byte[12];
            n[0] = (byte)(counter & 0xFF);
            n[1] = (byte)((counter >> 8) & 0xFF);
            n[2] = (byte)((counter >> 16) & 0xFF);
            n[3] = (byte)((counter >> 24) & 0xFF);
            return n; // little-endian
        }

        public async Task<bool> Test(ShadowSocksParse shadowSocksParse, CancellationToken cancellationToken, string targetHost = "httpbin.org", int targetPort = 80)
        {

            if (shadowSocksParse.ServerHost is null || shadowSocksParse.ServerPort is null)
                return false;

            if (!int.TryParse(shadowSocksParse.ServerPort, out int port) || port <= 0 || port > 65535)
                return false;

            TcpClient? tcpClient = null;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(7));

            try
            {
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(shadowSocksParse.ServerHost, port, cts.Token).ConfigureAwait(false);

                Stream stream = tcpClient.GetStream();

                var targetAddress = BuildTargetAddress(targetHost, targetPort); // получаем адресные данные для шифрования

                var msterkey = EvpBytesToKey(shadowSocksParse.ServerPassword, 32); // получаем ключ из пароля

                // создаем рандомную соль для генерации под-ключа
                byte[] salt = RandomNumberGenerator.GetBytes(32);
                _subkey = HKDF.DeriveKey(HashAlgorithmName.SHA1, //алгоритм хэширования
                    msterkey, //ключ
                    32,// длина под-ключа
                    salt,// соль
                    Encoding.ASCII.GetBytes("ss-subkey"));// информация для генерации под-ключа

                var createPacket = EncryptAddress(salt, _subkey, msterkey, targetAddress); // шифрем пароль + адресные данные

                int nonce = 2;
                string requesttext = HttpGetRequest(targetHost, _UserAgent); //текст запроса
                byte[] request = Encoding.UTF8.GetBytes(requesttext); 

                var requestres = EncryptPayload(_subkey, request, ref nonce);// шифруем запрос

                await stream.WriteAsync(createPacket, cts.Token);
                await stream.WriteAsync(requestres, cts.Token);

                byte[] probe = new byte[1];
                using var readCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
                readCts.CancelAfter(TimeSpan.FromSeconds(2));
                try
                {
                    int r = await stream.ReadAsync(probe, readCts.Token).ConfigureAwait(false);
                    return r > 0; // пришли данные — рукопожатие принято
                }
                catch (OperationCanceledException)
                {
                    // таймаут чтения: сервер молчит, но и не закрыл — считаем успехом
                    return true;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                tcpClient?.Dispose();
                throw;
            }
            catch (Exception)
            {
                tcpClient?.Dispose();
                return false;
            }
            finally
            {
                tcpClient?.Dispose();
            }
        }

        public static byte[] EncryptPayload(byte[] subkey, byte[] payload, ref int nonceCounter)
        {
            try
            {
                // создаем объект шифрования AES-GCM с использованием под-ключа и длины тега 16 байт
                using var aes = new AesGcm(subkey, 16);

                byte[] lenplain = { (byte)(payload.Length >> 8), (byte)(payload.Length & 0xFF) }; // берем2 байта от длины данных 
                byte[] lenplainlenght = new byte[lenplain.Length]; // длинна
                byte[] taglenht = new byte[16]; // тэг
                aes.Encrypt(BuildNonce(nonceCounter++), lenplain, lenplainlenght, taglenht); // зашифровали

                byte[] encaddhipher = new byte[payload.Length];// данные 
                byte[] enctag = new byte[16];// тэг
                aes.Encrypt(BuildNonce(nonceCounter++), payload, encaddhipher, enctag); //зашифровали данные

                using var stream = new MemoryStream();// записали в поток
                stream.Write(lenplainlenght); // cipher длины (2)
                stream.Write(taglenht);       // tag длины (16)
                stream.Write(encaddhipher);   // cipher данных (N)
                stream.Write(enctag);

                return stream.ToArray();

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EncryptAddress failed: {ex}");
                throw;
            }
        }

        public static byte[] EncryptAddress(byte[] salt, byte[] subkey, byte[] masterKey, byte[] addressPlaintext)
        {
            try
            {
                using var aes = new AesGcm(subkey, 16);// размер тега 16 байт

                byte[] lenghtplain = { (byte)(addressPlaintext.Length >> 8), (byte)(addressPlaintext.Length & 0xFF) };// нашли 2 байты от длинны адресных данных
                byte[] endplainlenght = new byte [16]; // 16 байт тега для длины адресных данных
                byte[] encLengthCipher = new byte[lenghtplain.Length]; // длина адресных данных
                aes.Encrypt(new byte[12], lenghtplain, encLengthCipher, endplainlenght);// шифруем длину адресных данных

                byte[] encAddrCipher = new byte[addressPlaintext.Length]; // адресные данные
                byte[] noncel = BuildNonce(1); // буффер счетки гарантирует что блоки зашифруюются по разнмоу
                byte[] encAddrTag = new byte[16]; // 16 байт тега  говори что данные не изменены и не тронуты
                aes.Encrypt(noncel, addressPlaintext, encAddrCipher, encAddrTag);// шифруем адресные данные

                using var ms = new MemoryStream();// записываем в поток
                ms.Write(salt);
                ms.Write(encLengthCipher);
                ms.Write(endplainlenght);
                ms.Write(encAddrCipher);
                ms.Write(encAddrTag);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EncryptAddress failed: {ex}");
                throw;
            }
        }

        private static byte[] EvpBytesToKey(string password, int keyLen)
        {
            byte[] pw = Encoding.UTF8.GetBytes(password);
            using var md5 = MD5.Create();
            var result = new List<byte>();
            byte[] prev = Array.Empty<byte>();

            while (result.Count < keyLen)
            {
                byte[] input = new byte[prev.Length + pw.Length];
                Buffer.BlockCopy(prev, 0, input, 0, prev.Length);
                Buffer.BlockCopy(pw, 0, input, prev.Length, pw.Length);
                prev = md5.ComputeHash(input);
                result.AddRange(prev);
            }

            return result.Take(keyLen).ToArray();
        }

        public static byte[] BuildTargetAddress(string host, int port)
        {
            try
            {
                byte[] hostbytes;
                byte ap;

                if (IPAddress.TryParse(host, out var iP))// определем версию протокола по IP адресу, если это домен то вернем 0x03
                {
                    if (iP.AddressFamily == AddressFamily.InterNetwork)
                    {
                        hostbytes = iP.GetAddressBytes();
                        ap = 0x01;
                    }
                    else
                    {
                        hostbytes = iP.GetAddressBytes();
                        ap = 0x04;
                    }
                }
                else
                { 
                    ap = 0x03;
                    hostbytes = Encoding.UTF8.GetBytes(host);
                    if (hostbytes.Length > 255)
                        throw new ArgumentException("Hostname too long");
                }

                byte highByte = (byte)((port >> 8) & 0xFF);  // старший байт
                byte lowByte = (byte)(port & 0xFF);         // младший байт

                byte[] portBytes = { highByte, lowByte }; // приводим порт к нужному виду

                using var msg = new MemoryStream();// поток для записи данных
                msg.WriteByte(ap);// записываем
                if (ap == 0x03)
                {
                    msg.WriteByte((byte)hostbytes.Length);
                }
                msg.Write(hostbytes, 0, hostbytes.Length);// записываем
                msg.Write(portBytes, 0, portBytes.Length);// записываем
                return msg.ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EncryptAddress failed: {ex}");
                throw;
            }
        }

        public string HttpGetRequest(string host, string userAgent)
        {
            return $"GET / HTTP/1.1\r\n" +
                $"Host: {host}\r\n" +
                $"User-Agent: {userAgent}\r\n" +
                $"Content-Type: application/json\r\n" +
                $"Connection: close\r\n" +
                $"\r\n";
        }
    }
}
