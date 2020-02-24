using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RootNav.Data.IO
{
    [Serializable]
    public enum ConnectionSource
    {
        MySQLDatabase,
        RSMLDirectory
    }

    [Serializable]
    public class ConnectionParams
    {
        public ConnectionSource Source { get; set; }
        public string Server { get; set; }
        public string Port { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Directory { get; set; }

        public string ToMySQLConnectionString()
        {
            return String.Format("server={0};port={1};database={2};uid={3};password={4};", this.Server, this.Port, this.Database, this.Username, this.Password);
        }

        public string ToXML()
        {
            System.Xml.Serialization.XmlSerializer sx = new System.Xml.Serialization.XmlSerializer(this.GetType());
            StringBuilder xmlString = new StringBuilder();
            System.Xml.XmlWriter wrt = System.Xml.XmlWriter.Create(xmlString);
            sx.Serialize(wrt, this);
            return xmlString.ToString();
        }

        public static ConnectionParams FromXML(string xmlString)
        {
            try
            {
                System.IO.MemoryStream ms = new System.IO.MemoryStream(Encoding.Unicode.GetBytes(xmlString));
                System.Xml.Serialization.XmlSerializer sx = new System.Xml.Serialization.XmlSerializer(typeof(ConnectionParams));
                object param = sx.Deserialize(ms);
                return param as ConnectionParams;
            }
            catch
            {
                // If a C_DATA file exists, it's not valid XML
                return null;
            }
        }

        public static ConnectionParams FromEncryptedStorage()
        {
            // Attempt to read encrypted storage
            string xmlData = EncryptedStorage.ReadEncryptedString("C_DATA");

            if (xmlData != null && xmlData != "")
            {
                return ConnectionParams.FromXML(xmlData);
            }
            else
            {
                return null;
            }
        }
    }
}
