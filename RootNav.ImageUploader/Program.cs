using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RootNav.Data;
using RootNav.Data.IO;
using System.Security.Cryptography;
using System.IO;
using RootNav.Data.IO.Databases;
using MySql.Data.MySqlClient;

namespace RootNav.ImageUploader
{
    class Program
    {
        static void Main(string[] args)
        {
            string RootDirectory = @"C:\Users\mpound\Desktop\140215";
            string ImageDirectory = RootDirectory;//RootDirectory.Remove(RootDirectory.LastIndexOf("\\"));

            Dictionary<string, string> FileTagMap = new Dictionary<string, string>();

            using (StreamReader strm = new StreamReader(RootDirectory + "\\mapped.txt"))
            {
                while (!strm.EndOfStream)
                {
                    string line = strm.ReadLine();
                    string[] parts = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    string fullPath = ImageDirectory + "\\" +  parts[0];

                    string cropPath = fullPath.Insert(fullPath.LastIndexOf("\\") + 1, "crop\\");
                    

                    string tag = parts[1].Replace("\"", "");

                    FileTagMap.Add(fullPath, tag);
                }
            }

            #region DB connect and test
            byte[] key = Encoding.ASCII.GetBytes("ght29cj8f230f30d");
            byte[] IV = Encoding.ASCII.GetBytes("a32fg32bnds2df90");
            byte[] enc = new byte[] { 138, 208, 147, 69, 17, 213, 75, 13, 125, 185, 237, 113, 181, 64, 221, 16 };

            ConnectionParams cp = new ConnectionParams()
            {
                Server = "ssbmb.nottingham.ac.uk",
                Database = "arab_rootnav",
                Port = "3306",
                Source = ConnectionSource.MySQLDatabase,
                Username = "sbzmpp",
                Password = "z7LVM8mnTFttEYTA"//DecryptStringFromBytes_Aes(enc, key, IV)
            };

            System.Console.Write("Connecting to database: ");

            MySQLDatabaseManager manager = new MySQLDatabaseManager();
            manager.Open(cp.ToMySQLConnectionString());

            
            string imagesTableCheckString = "SELECT TABLE_NAME as name FROM information_schema.TABLES WHERE TABLE_SCHEMA = '" + manager.Connection.Database + "'";
            MySqlCommand imagesTableCheckCommand = new MySqlCommand(imagesTableCheckString, manager.Connection as MySqlConnection);
            bool confirmedImageDB = false;
            using (MySqlDataReader Reader = imagesTableCheckCommand.ExecuteReader())
            {
                while (Reader.Read())
                {
                    if (Reader["name"] as String == "images")
                    {
                        confirmedImageDB = true;
                    }
                }
            }

            if (manager.IsOpen)
                System.Console.WriteLine("Done");
            else
                System.Console.WriteLine("Could not connect");

            if (!confirmedImageDB)
            {
                Console.WriteLine("Updating database to include image table");

                string creationString = "CREATE TABLE IF NOT EXISTS images(" +
                                  "Tag CHAR(64) NOT NULL, " +
                                  "Stamp TIMESTAMP NOT NULL, " +
                                  "Image LONGBLOB, " +
                                  "CONSTRAINT pk_image_one PRIMARY KEY (Tag, Stamp) " +
                                  ") ENGINE=INNODB";

                MySqlCommand creationCommand = new MySqlCommand(creationString, manager.Connection as MySqlConnection);
                int success = creationCommand.ExecuteNonQuery();

            }

            #endregion

            foreach (var kvp in FileTagMap)
            {
                string file = kvp.Key;
                string tag = kvp.Value;

               // if (file != @"I:\Michael\Savannah Rialto DH panel exps\RSDH08 (Marcus) HN vs LN\22\crop\10001.JPG") continue;

                if (tag != "#N/A")
                {
                    byte[] image = File.ReadAllBytes(file);
                    InjectImageIntoDB(tag, image, manager.Connection as MySqlConnection);
                    Console.WriteLine("Inserted " + tag);
                }
            }


            //byte[] jpg = File.ReadAllBytes(@"C:\Users\mpound\Desktop\0002.jpg");
            //string tag = "mp1,0002,test";

            /******************************/
        }

        public static void InjectImageIntoDB(string tag, byte[] image, MySqlConnection connection)
        {
            // Obtain timestamp with associated tag
            string stampQueryString = "SELECT DISTINCT(Stamp) FROM rootdata WHERE Tag = '" + tag + "'";
            MySqlCommand stampQueryCommand = new MySqlCommand(stampQueryString, connection);
            DateTime stamp = DateTime.Now;
            bool stampFound = false;
            bool tagFound = false;
            using (MySqlDataReader Reader = stampQueryCommand.ExecuteReader())
            {
                if (Reader.HasRows)
                {
                    tagFound = true;
                }

                while (Reader.Read())
                {
                    if (Reader["Stamp"] != null)
                    {
                        stamp = (DateTime)Reader["Stamp"];
                        stampFound = true;
                    }
                }
            }

            if (tagFound && stampFound)
            {
                // Query
                string insertString = "INSERT INTO images (Tag, Stamp, Image) VALUES (@Tag, @Stamp, @Image)";

                MySqlCommand insertCommand = new MySqlCommand(insertString, connection);

                // Parameters
                insertCommand.Parameters.Add("@Tag", MySqlDbType.VarChar, 64);
                insertCommand.Parameters.Add("@Stamp", MySqlDbType.Timestamp);
                insertCommand.Parameters.Add("@Image", MySqlDbType.LongBlob);

                // Transaction begin
                MySqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    insertCommand.Parameters["@Tag"].Value = tag;
                    insertCommand.Parameters["@Stamp"].Value = stamp;
                    insertCommand.Parameters["@Image"].Value = image;
                    insertCommand.ExecuteNonQuery();

                }
                catch
                {
                    transaction.Rollback();
                    return;
                }

                // Commit
                transaction.Commit();
            }
        }

        static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream.
            return encrypted;

        }

        static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key
        , byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;
        }
    }

}
