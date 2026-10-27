using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData.ParseData;
using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace ProxyTG_HTTP.TestMtproto
{
    public class TestMtprtotoTunnel
    {
        private readonly ILogger<TestMtprtotoTunnel> _logger;
        public static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

        public TestMtprtotoTunnel(ILogger<TestMtprtotoTunnel> logger) => _logger = logger;

        public async Task<bool> TestMethod(MtProtoParse mtProtoParse, CancellationToken cancellationTokenSource)
        {
            byte[] buf = _arrayPool.Rent(1024);
            TcpClient? tcp = null;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            try
            {
                tcp = new TcpClient();
                await tcp.ConnectAsync(mtProtoParse.ServerKey, int.Parse(mtProtoParse.PortKey), cts.Token);
                tcp.NoDelay = true;

                using Stream stream = tcp.GetStream();

                var (key, domain) = SecretNormalizate.Normalizate(mtProtoParse.SecretKey);
                bool useTls = domain != null;

                await stream.WriteAsync(CreatePacket(key, 2, useTls), cts.Token).ConfigureAwait(false);

                int read = 0;
                try
                {
                    read = await stream.ReadAsync(buf.AsMemory(0, buf.Length), cts.Token).ConfigureAwait(false);
                }
                catch
                {
                    return false;
                }

                if (read <= 0) return false;

                int off = 0;

                if (useTls)
                {
                    if (read >= 6 && buf[0] == 0x14 && buf[1] == 0x03 && buf[2] == 0x03)
                        off = 6;
                    if (read - off >= 5 && buf[off] == 0x17 && buf[off + 1] == 0x03 && buf[off + 2] == 0x03)
                        off += 5;
                }

                if (read - off < 4) return false;

                if (buf[off] == 0x00 && buf[off + 1] == 0x00 && buf[off + 2] == 0x00 && buf[off + 3] == 0x00)
                    return true;

                int err = BinaryPrimitives.ReadInt32LittleEndian(buf.AsSpan(off, 4));
                if (err < 0)
                    return true;

                return true;
            }
            catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
            {
                tcp?.Dispose();
                throw;
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                tcp?.Dispose();
                return false;
            }
            finally
            {
                _arrayPool.Return(buf);
            }
        }

        /// <summary>
        /// Пакет «куда идем»: 64-байтовый обфусцированный заголовок + фрейм с req_pq_multi,
        /// для dd (useTls) — в TLS-записях (dummy CCS + application_data).
        /// </summary>
        public byte[] CreatePacket(byte[] hexkey, int targetDc = 2, bool useTls = true)
        {
            try
            {
                // 1. заголовок: 64 случайных байта
                byte[] p = new byte[64];
                RandomNumberGenerator.Fill(p);
                if (p[0] == 0xef) p[0] = 0x80; // не выглядеть как abridged-транспорт

                // 2. ключ зависит от байтов самого заголовка: SHA256(header[8..40] || secret), iv = header[40..56]
                byte[] seed = new byte[48];
                p.AsSpan(8, 32).CopyTo(seed);
                hexkey.AsSpan(0, 16).CopyTo(seed.AsSpan(32));
                using var aes = Aes.Create();
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                aes.IV = new byte[16];
                aes.Key = SHA256.HashData(seed);
                using var enc = aes.CreateEncryptor();

                // 3. сервер после расшифровки ждёт в байтах 56..62: тег 0xeeeeeeee + DC;
                //    эти байты — 3-й блок CTR (48..63) → счётчик = iv + 3
                byte[] ctr = p.AsSpan(40, 16).ToArray();
                for (int i = 15, c = 3; i >= 0 && c > 0; i--)
                { 
                    int s = ctr[i] + c;
                    ctr[i] = (byte)s; 
                    c = s >> 8;
                }
                byte[] ks = new byte[16];
                enc.TransformBlock(ctr, 0, 16, ks, 0);
                byte[] tail = new byte[8];
                BinaryPrimitives.WriteUInt32LittleEndian(tail, 0xeeeeeeee);
                BinaryPrimitives.WriteInt16LittleEndian(tail.AsSpan(4), (short)targetDc);
                for (int i = 0; i < 8; i++)
                    p[56 + i] = (byte)(tail[i] ^ ks[8 + i]);

                // 4. проба: незашифрованный req_pq_multi (auth_key_id=0 | msg_id | len | конструктор | nonce)
                byte[] msg = new byte[40];
                BinaryPrimitives.WriteInt64LittleEndian(msg.AsSpan(8), DateTimeOffset.UtcNow.ToUnixTimeSeconds() << 32);
                BinaryPrimitives.WriteInt32LittleEndian(msg.AsSpan(16), 20);
                BinaryPrimitives.WriteUInt32LittleEndian(msg.AsSpan(20), 0xbe7e8ef1u);
                RandomNumberGenerator.Fill(msg.AsSpan(24, 16));

                // 5. склейка: заголовок + int32 длина фрейма + сообщение
                byte[] payload = new byte[64 + 4 + msg.Length];
                p.CopyTo(payload, 0);
                BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(64), msg.Length);
                msg.CopyTo(payload.AsSpan(68));
                if (!useTls) return payload;

                // 6. TLS: dummy ChangeCipherSpec + application_data record
                byte[] tls = new byte[11 + payload.Length];
                byte[] ccs = { 0x14, 0x03, 0x03, 0x00, 0x01, 0x01 };
                ccs.CopyTo(tls, 0);
                tls[6] = 0x17; tls[7] = 0x03; tls[8] = 0x03;
                BinaryPrimitives.WriteUInt16BigEndian(tls.AsSpan(9), (ushort)payload.Length);
                payload.CopyTo(tls.AsSpan(11));
                return tls;
            }
            catch (Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                throw;
            }
        }
    }
}
