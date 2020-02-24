using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using System.IO;
using RootNav.Data.IO;

namespace RootNav.Data.IO.Databases
{
    /// <summary>
    /// MySQL Database root reader class
    /// </summary>
    public class MySQLRootReader : IRootReader
    {
        private DatabaseManager dbm;
        private ConnectionParams connectionInfo = null;

        public MySQLRootReader(ConnectionParams connectionInfo)
        {
            this.connectionInfo = connectionInfo;
        }

        public bool Initialise()
        {
            dbm = new MySQLDatabaseManager();

            if (!dbm.Open(this.connectionInfo.ToMySQLConnectionString()))
            {
                return false;
            }

            return true;

        }

        public Tuple<SceneMetadata, SceneInfo, byte[]> Read(String tag, bool image)
        {
            SceneInfo scene;
            SceneMetadata metadata;

            MySqlConnection connection = dbm.Connection as MySqlConnection;
            MySqlCommand selectCommand = new MySqlCommand("SELECT Stamp, RelativeID, StartReference, Spline, Label, CompleteArchitecture, User, HullArea FROM rootdata WHERE Tag = (?tag)", connection);
            selectCommand.Parameters.AddWithValue("?tag", tag);

            Dictionary<Tuple<String, DateTime>, List<RootInfo>> roots = new Dictionary<Tuple<String, DateTime>, List<RootInfo>>();
            DateTime stamp = DateTime.Now;
            bool complete = true;

            HashSet<String> plantIDs = new HashSet<string>();

            string user = "";

            using (MySqlDataReader Reader = selectCommand.ExecuteReader())
            {
                while (Reader.Read())
                {
                    if (user == "")
                    {
                        user = Reader.GetString("User");
                    }

                    stamp = Reader.GetDateTime("Stamp");
                    String relativeID = Reader.GetString("RelativeID");
                    SampledSpline spline = null;
                    complete = Reader.GetBoolean("CompleteArchitecture");

                    string label = null;
                    if (!Reader.IsDBNull(4))
                    {
                        label = Reader.GetString("Label");
                    }

                    if (!Reader.IsDBNull(3))
                    {
                        try
                        {
                            spline = RootNav.Data.SplineSerializer.BinaryToObject((byte[])Reader["Spline"]) as SampledSpline;
                        }
                        catch
                        {
                            spline = null;
                        }
                    }

                    SplinePositionReference position = null;
                    if (!Reader.IsDBNull(2))
                    {
                        position = RootNav.Data.SplineSerializer.BinaryToObject((byte[])Reader["StartReference"]) as SplinePositionReference;
                    }

                    String key = relativeID.IndexOf('.') < 0 ? relativeID : relativeID.Substring(0, relativeID.IndexOf('.'));
                    Tuple<string, DateTime> datedKey = new Tuple<string, DateTime>(key, stamp);

                    if (!roots.ContainsKey(datedKey))
                    {
                        roots.Add(datedKey, new List<RootInfo>());
                    }

                    roots[datedKey].Add(new RootInfo() { RelativeID = relativeID, StartReference = position, Spline = spline, Label = label, Stamp = stamp });
                }

                var timeSeparatedRoots = DuplicateTagCheck(roots);

                List<PlantInfo> plants = new List<PlantInfo>();
                foreach (var timeStampedKVP in timeSeparatedRoots)
                {
                    DateTime currentStamp = timeStampedKVP.Key;
                    var stampedRoots = timeStampedKVP.Value;
                    foreach (var kvp in stampedRoots)
                    {
                        plants.Add(PlantInfo.CreateTree(tag, currentStamp, complete, kvp.Value));
                    }
                }

                metadata = new SceneMetadata()
                {
                    Key = tag,
                    Complete = complete,
                    Sequence = null,
                    User = user,
                    Software = "RootNav",
                    Resolution = 1.0,
                    Unit = "pixel",
                };

               scene = new SceneInfo() { Plants = plants };
            }

            // Optional image search
            byte[] imageData = null;

            if (image)
            {
                connection = dbm.Connection as MySqlConnection;
                MySqlCommand imageCommand = new MySqlCommand("SELECT Image FROM images WHERE Tag = (?tag) AND Stamp = (?stamp)", connection);
                imageCommand.Parameters.AddWithValue("?tag", tag);
                imageCommand.Parameters.AddWithValue("?stamp", stamp);
                using (MySqlDataReader imageReader = imageCommand.ExecuteReader())
                {
                    if (imageReader.HasRows)
                    {
                        imageReader.Read();
                        imageData = imageReader["Image"] as byte[];
                    }
                }
            }

            return new Tuple<SceneMetadata, SceneInfo, byte[]> (metadata, scene, imageData);
        }

        private Dictionary<DateTime, Dictionary<String, List<RootInfo>>> DuplicateTagCheck(Dictionary<Tuple<String, DateTime>, List<RootInfo>> roots)
        {
            HashSet<DateTime> stamps = new HashSet<DateTime>();
            foreach (var key in roots.Keys)
            {
                stamps.Add(key.Item2);
            }

            Dictionary<DateTime, Dictionary<String, List<RootInfo>>> timeSeparatedRoots = new Dictionary<DateTime, Dictionary<string, List<RootInfo>>>();

            int index = 0;
            foreach (DateTime stamp in stamps)
            {
                if (!timeSeparatedRoots.ContainsKey(stamp))
                {
                    timeSeparatedRoots.Add(stamp, new Dictionary<string, List<RootInfo>>());
                }

                foreach (var kvp in roots)
                {
                    if (kvp.Key.Item2 == stamp)
                    {
                        timeSeparatedRoots[stamp].Add(kvp.Key.Item1, kvp.Value);
                    }
                }
                index++;
            }

            return timeSeparatedRoots;
        }

        public List<String> FilterTags(String[] searchTerms, bool any, DateTime? date = null)
        {
            StringBuilder selectString = new StringBuilder("SELECT DISTINCT(Tag) FROM rootdata WHERE ");

            bool searchWords = searchTerms.Length == 0 ? false : (searchTerms.Length == 1 && searchTerms[0] == "" ? false : true);
            bool searchDate = date.HasValue;

            if (searchWords)
            {
                for (int i = 0; i < searchTerms.Length; i++)
                {
                    if (i == 0)
                    {
                        selectString.AppendFormat("(INSTR(Tag,?st{0}) > 0", i);
                    }
                    else
                    {
                        if (any)
                        {
                            selectString.AppendFormat(" OR INSTR(Tag,?st{0}) > 0", i);
                        }
                        else
                        {
                            selectString.AppendFormat(" AND INSTR(Tag,?st{0}) > 0", i);
                        }
                    }
                }

                selectString.Append(")");
            }

            if (searchDate)
            {
                if (searchWords)
                {
                    selectString.Append(" AND date(?dt0) = date(Stamp)");
                }
                else
                {
                    selectString.Append("date(?dt0) = date(Stamp)");
                }
            }

            if (!searchWords && !searchDate)
            {
                selectString.Append("1"); // Evaluate WHERE clause to true for no search terms
            }

            MySqlConnection connection = dbm.Connection as MySqlConnection;
            MySqlCommand selectCommand = new MySqlCommand(selectString.ToString(), connection);

            if (searchWords)
            {
                for (int i = 0; i < searchTerms.Length; i++)
                {
                    selectCommand.Parameters.AddWithValue(string.Concat("?st", i), searchTerms[i].ToString());
                }
            }

            if (searchDate)
            {
                DateTime dt = date.Value;
                selectCommand.Parameters.AddWithValue("?dt0", dt);
            }

            List<string> distinctTags = new List<string>();
            using (MySqlDataReader Reader = selectCommand.ExecuteReader())
            {
                while (Reader.Read())
                {
                    distinctTags.Add(Reader.GetString(0));
                }
            }

            return distinctTags;
        }

        public List<String> ReadAllTags()
        {
            MySqlConnection connection = dbm.Connection as MySqlConnection;
            MySqlCommand selectCommand = new MySqlCommand("SELECT DISTINCT(Tag) FROM rootdata", connection);
            List<string> distinctTags = new List<string>();
            using (MySqlDataReader Reader = selectCommand.ExecuteReader())
            {
                while (Reader.Read())
                {
                    distinctTags.Add(Reader.GetString(0));
                }
            }

            return distinctTags;
        }

        public bool Connected
        {
            get
            {
                return this.dbm != null && this.dbm.IsOpen;
            }
        }
    }
}
