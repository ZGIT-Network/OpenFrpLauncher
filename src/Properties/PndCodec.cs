using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OpenFrp.Launcher
{
    public class PndCodec
    {
        public static byte[] KeyIV
        {
            get
            {
                var strs = $"OfOn.{AppDomain.CurrentDomain.BaseDirectory}";
                if (!string.IsNullOrEmpty(strs) && strs.Length > 0)
                {
                    return Encoding.UTF8.GetBytes(strs.Remove(8, strs.Length - 8));
                }
                else
                {
                    return Encoding.UTF8.GetBytes("");
                }

            }
        }

        public static string EncryptString(byte[] str)
        {
            DESCryptoServiceProvider des = new();
            des.Key = des.IV = KeyIV;
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    var bytes = str;
                    cs.Write(bytes, 0, bytes.Length);
                    cs.FlushFinalBlock();
                }
                var sb = new StringBuilder();
                foreach (var bi in ms.ToArray())
                {
                    sb.Append(bi.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static byte[] Descrypt(string str)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            byte[] bytes = new byte[str.Length / 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)Convert.ToInt32(str.Substring(i * 2, 2), 16);
            }
            des.Key = des.IV = KeyIV;

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(bytes, 0, bytes.Length);
                    cs.FlushFinalBlock();
                }

                return ms.ToArray();
            }

        }
    }
}
