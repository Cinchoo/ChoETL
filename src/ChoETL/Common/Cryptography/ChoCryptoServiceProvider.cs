using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoCryptoServiceProvider
    {
        private readonly ICryptoTransform DESDecrypt;
        private readonly ICryptoTransform DESEncrypt;

        private const string DES1 = "FlOwErS";
        private const string DES2 = "meatBALL";
        private const string DES3 = "TurniP";
        private const string DES4 = "MacroBiotic";

        private const int DES5 = 47;
        private const int DES6 = 91;
        private const int DES7 = 11;

        public ChoCryptoServiceProvider()
        {
            TripleDESCryptoServiceProvider DESCryptoProvider = new TripleDESCryptoServiceProvider();
            MD5CryptoServiceProvider hashMD5 = new MD5CryptoServiceProvider();
            DESCryptoProvider.Key = hashMD5.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(DESKey));
            DESCryptoProvider.Mode = CipherMode.ECB;


            DESDecrypt = DESCryptoProvider.CreateDecryptor();
            DESEncrypt = DESCryptoProvider.CreateEncryptor();
        }

        public string DecryptDES(string encText)
        {
            byte[] buffer = Convert.FromBase64String(encText);
            return System.Text.ASCIIEncoding.ASCII.GetString(DESDecrypt.TransformFinalBlock(buffer, 0, buffer.Length));
        }

        public string EncryptDES(string plainText)
        {
            byte[] buffer = System.Text.ASCIIEncoding.ASCII.GetBytes(plainText);
            return Convert.ToBase64String(DESEncrypt.TransformFinalBlock(buffer, 0, buffer.Length));
        }

        public string DESKey
        {
            get { return DES1 + Strings.Format(DES5, "0000") + DES2 + Strings.Format(DES6, "0000") + DES3 + Strings.Format(DES7, "0000") + DES4; }
        }
    }

}
