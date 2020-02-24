using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RootNav.IO
{
    static class Hashing
    {
        public static string Sha256(string path)
        {
            string strResult = "";
            string strHashData = "";

            byte[] arrbytHashValue;
            System.IO.FileStream oFileStream = null;

            System.Security.Cryptography.SHA256CryptoServiceProvider sha256Hasher = new System.Security.Cryptography.SHA256CryptoServiceProvider();
            try
            {
                oFileStream = new System.IO.FileStream(path, System.IO.FileMode.Open);
                arrbytHashValue = sha256Hasher.ComputeHash(oFileStream);
                oFileStream.Close();

                strHashData = System.BitConverter.ToString(arrbytHashValue);
                strHashData = strHashData.Replace("-", "");
                strResult = strHashData;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
            finally
            {
                oFileStream.Close();
            }

            return strResult;
        }
    }
}
