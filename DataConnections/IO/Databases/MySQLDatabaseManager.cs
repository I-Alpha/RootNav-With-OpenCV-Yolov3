using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Reflection;
using System.IO;
using System.IO.IsolatedStorage;
using System.Security.Cryptography;

namespace RootNav.Data.IO.Databases
{
    public class MySQLDatabaseManager : DatabaseManager
    {
        public override bool IsOpen
        {
            get
            {
                MySqlConnection conn = this.connection as MySqlConnection;
                return conn != null && conn.Ping();
            }
        }

        public override bool Open(string connectionString)
        {
            MySqlConnection mysqlConnection = this.connection as MySqlConnection;

            if (mysqlConnection != null && mysqlConnection.Ping())
            {
                // Connection is already open
                return true;
            }

            try
            {
                ConnectAndCreateDatabase(connectionString);
                connection.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        
        public override bool ValidateConnection(string connectionString, out int errorNo)
        {
            try
            {
                ConnectAndCreateDatabase(connectionString);
                errorNo = 0;
                return true;
            }
            catch (MySqlException ex)
            {
                if (ex.Message.Contains("denied"))
                {
                    errorNo = -1;
                }
                else
                {
                    errorNo = ex.Number;
                }
                return false;
            }
            catch
            {
                errorNo = 0;
                return false;
            }
            finally
            {
                connection.Close();
            }
        }
        

        private void ConnectAndCreateDatabase(string connectionString)
        {
            connection = new MySqlConnection(connectionString);
            connection.Open();

            string creationString = "CREATE TABLE IF NOT EXISTS rootdata(" +
                                    "Tag CHAR(64) NOT NULL, " +
                                    "Stamp TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, " +
                                    "RelativeID CHAR(32) NOT NULL, " +
                                    "Start CHAR(32), " +
                                    "End CHAR(32), " +
                                    "RootOrder TINYINT NOT NULL, " +
                                    "Length FLOAT, " +
                                    "Label CHAR(64) DEFAULT NULL, " +
                                    "EmergenceAngle FLOAT, " +
                                    "TipAngle FLOAT, " +
                                    "StartReference BLOB, " +
                                    "StartDistance FLOAT, " +
                                    "Spline MEDIUMBLOB, " +
                                    "HullArea FLOAT, " +
                                    "PrimaryParentRelativeID CHAR(32), " +
                                    "ParentRelativeID CHAR(32), " +
                                    "ChildCount INT NOT NULL, " +
                                    "User CHAR(64) NOT NULL DEFAULT '', " +
                                    "CompleteArchitecture BOOL NOT NULL DEFAULT TRUE, " +
                                    "INDEX usr_index (User), " +
                                    "CONSTRAINT pk_one PRIMARY KEY (Tag, Stamp, RelativeID) " +
                                    ") ENGINE=INNODB ROW_FORMAT=COMPRESSED KEY_BLOCK_SIZE=4;";

            MySqlCommand creationCommand = new MySqlCommand(creationString, connection as MySqlConnection);
            int success = creationCommand.ExecuteNonQuery();


            // Code here to upgrade database
            #region BLOB TO MEDIUMBLOB
            String blobCheckString = "SELECT * FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = '" + connection.Database + "' AND TABLE_NAME = 'rootdata' AND COLUMN_NAME = 'Spline'";
            MySqlCommand blobCheckCommand = new MySqlCommand(blobCheckString, connection as MySqlConnection);
            bool confirmedBlob = false;
            using (MySqlDataReader Reader = blobCheckCommand.ExecuteReader())
            {
                while (Reader.Read())
                {
                    if (Reader["COLUMN_TYPE"] as String == "mediumblob")
                    {
                        confirmedBlob = true;
                    }
                }
            }
            

            if (!confirmedBlob)
            {
                System.Windows.MessageBox.Show("Updating database to new version (Spline data to MEDIUMBLOB size), this may take some time.", "Database updating");
                MySqlCommand command = (new MySqlCommand("ALTER TABLE rootdata MODIFY Spline MEDIUMBLOB", connection as MySqlConnection));
                command.CommandTimeout = 400;
                if (command.ExecuteNonQuery() >= 0)
                {
                    System.Windows.MessageBox.Show("Spline blob increased in size", "Database Updated");
                }
            }
            #endregion

            #region Existence of User Column
            String userCheckString = "SELECT * FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = '" + connection.Database +"' AND TABLE_NAME = 'rootdata' AND COLUMN_NAME = 'User'";
            MySqlCommand userCheckCommand = new MySqlCommand(userCheckString, connection as MySqlConnection);
            bool userColumnExists = true;
            using (MySqlDataReader Reader = userCheckCommand.ExecuteReader())
            {
                if (!Reader.HasRows)
                {
                    userColumnExists = false;
                }
            }

            if (!userColumnExists)
            {
                System.Windows.MessageBox.Show("Updating database to new version - Adding user column, this may take some time.", "Database updating");
                MySqlCommand command = (new MySqlCommand("ALTER TABLE  `rootdata` ADD  `User` CHAR( 64 ) NOT NULL DEFAULT  ''", connection as MySqlConnection));
                command.CommandTimeout = 400;
                if (command.ExecuteNonQuery() >= 0)
                {
                    System.Windows.MessageBox.Show("User column added", "Database Updated");
                }
            }
            #endregion

            #region Existence of Label Column
            String labelCheckString = "SELECT * FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = '" + connection.Database + "' AND TABLE_NAME = 'rootdata' AND COLUMN_NAME = 'Label'";
            MySqlCommand labelCheckCommand = new MySqlCommand(labelCheckString, connection as MySqlConnection);
            bool labelColumnExists = true;
            using (MySqlDataReader Reader = labelCheckCommand.ExecuteReader())
            {
                if (!Reader.HasRows)
                {
                    labelColumnExists = false;
                }
            }
          
            if (!labelColumnExists)
            {
                System.Windows.MessageBox.Show("Updating database to new version (Label column), this may take some time.", "Database updating");
                MySqlCommand command = (new MySqlCommand("ALTER TABLE  `rootdata` ADD  `Label` CHAR(64) DEFAULT NULL AFTER `Length`", connection as MySqlConnection));
                command.CommandTimeout = 400;
                if (command.ExecuteNonQuery() >= 0)
                {
                    System.Windows.MessageBox.Show("Label column added", "Database Updated");
                }
            }
            #endregion

            #region Existence of CompleteArchitecture Column
            String lateralCheckString = "SELECT * FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = '" + connection.Database + "' AND TABLE_NAME = 'rootdata' AND COLUMN_NAME = 'CompleteArchitecture'";
            MySqlCommand lateralCheckCommand = new MySqlCommand(lateralCheckString, connection as MySqlConnection);
            bool lateralColumnExists = true;
            using (MySqlDataReader Reader = lateralCheckCommand.ExecuteReader())
            {
                if (!Reader.HasRows)
                {
                    lateralColumnExists = false;
                }
            }

            if (!lateralColumnExists)
            {
                System.Windows.MessageBox.Show("Updating database to new version (Complete architecture column), this may take some time.", "Database updating");
                MySqlCommand command = (new MySqlCommand("ALTER TABLE  `rootdata` ADD  `CompleteArchitecture` BOOLEAN NOT NULL DEFAULT TRUE", connection as MySqlConnection));
                command.CommandTimeout = 400;
                if (command.ExecuteNonQuery() >= 0)
                {
                    System.Windows.MessageBox.Show("Complete Architecture column added", "Database Updated");
                }
            }
            #endregion

            #region Existence of images table
            String imageTableCheckString = "select TABLE_NAME from information_schema.TABLES WHERE TABLE_SCHEMA = '" + connection.Database + "' AND TABLE_NAME = 'images'";
            MySqlCommand imageTableCheckCommand = new MySqlCommand(imageTableCheckString, connection as MySqlConnection);
            bool imageTableExists = true;
            using (MySqlDataReader Reader = imageTableCheckCommand.ExecuteReader())
            {
                if (!Reader.HasRows)
                {
                    imageTableExists = false;
                }
            }

            if (!imageTableExists)
            {              
                MySqlCommand command = (new MySqlCommand("CREATE TABLE images (Tag CHAR(64) NOT NULL, Stamp TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, Image LONGBLOB, PRIMARY KEY (Tag,Stamp)) ENGINE = InnoDB", connection as MySqlConnection));
                command.CommandTimeout = 400;
                if (command.ExecuteNonQuery() >= 0)
                {
                    System.Windows.MessageBox.Show("Image table added", "Database Updated");
                }
            }
            #endregion


            // Check for successful creation of a table or pre-existing table
            if (success < 0)
            {
                throw new InvalidOperationException("Database table could not be created");
            }

            connection.Close();
        }

        public override void Close()
        {
            if (this.connection != null)
            {
                this.connection.Close();
            }
        }

        public override bool Write(string tag, bool alllaterals, List<Dictionary<string, object>> records, byte[] imageData = null)
        {
            MySqlConnection connection = this.connection as MySqlConnection;

            if (connection == null)
            {
                return false;
            }

            // Query
            string insertString = "INSERT INTO rootdata (" +
                                                        "Tag, Stamp, RelativeID, Start, End, " +
                                                        "RootOrder, Length, Label, EmergenceAngle, TipAngle, StartReference, StartDistance, Spline, HullArea, " +
                                                        "PrimaryParentRelativeID, ParentRelativeID, ChildCount, User, CompleteArchitecture) " +
                                                        "VALUES (" +
                                                        "@Tag, @Stamp, @RelativeID, @Start, @End, " +
                                                        "@RootOrder, @Length, @Label, @EmergenceAngle, @TipAngle, @StartReference, @StartDistance, @Spline, @HullArea, " +
                                                        "@PrimaryParentRelativeID, @ParentRelativeID, @ChildCount, @User, @CompleteArchitecture);";

            MySqlCommand insertCommand = new MySqlCommand(insertString, connection);

            MySqlCommand timestampCommand = new MySqlCommand("SELECT CURRENT_TIMESTAMP", connection);
            object timestamp = timestampCommand.ExecuteScalar();

            MySqlCommand currentUserCommand = new MySqlCommand("SELECT USER()", connection);
            object user = currentUserCommand.ExecuteScalar();

            String username = user as String;
            username = username.Substring(0, username.IndexOf('@'));


            // Parameters
            insertCommand.Parameters.Add("@Tag", MySqlDbType.VarChar, 64);
            insertCommand.Parameters.Add("@Stamp", MySqlDbType.Timestamp);
            insertCommand.Parameters.Add("@RelativeID", MySqlDbType.VarChar, 32);
            insertCommand.Parameters.Add("@Start", MySqlDbType.VarChar, 32);
            insertCommand.Parameters.Add("@End", MySqlDbType.VarChar, 32);
            insertCommand.Parameters.Add("@RootOrder", MySqlDbType.Int32, 4);
            insertCommand.Parameters.Add("@Length", MySqlDbType.Float);
            insertCommand.Parameters.Add("@Label", MySqlDbType.VarChar, 64);
            insertCommand.Parameters.Add("@EmergenceAngle", MySqlDbType.Float);
            insertCommand.Parameters.Add("@TipAngle", MySqlDbType.Float);
            insertCommand.Parameters.Add("@StartReference", MySqlDbType.Binary);
            insertCommand.Parameters.Add("@StartDistance", MySqlDbType.Float);
            insertCommand.Parameters.Add("@Spline", MySqlDbType.Binary);
            insertCommand.Parameters.Add("@HullArea", MySqlDbType.Float);
            insertCommand.Parameters.Add("@PrimaryParentRelativeID", MySqlDbType.VarChar, 32);
            insertCommand.Parameters.Add("@ParentRelativeID", MySqlDbType.VarChar, 32);
            insertCommand.Parameters.Add("@ChildCount", MySqlDbType.Int32, 11);
            insertCommand.Parameters.Add("@User", MySqlDbType.String, 64);
            insertCommand.Parameters.Add("@CompleteArchitecture", MySqlDbType.Int32, 1);

            // Transaction begin
            MySqlTransaction transaction = connection.BeginTransaction();

            try
            {
                foreach (Dictionary<String,object> data in records)
                {
                    // Assign parameters
                    insertCommand.Parameters["@Tag"].Value = tag;
                    insertCommand.Parameters["@Stamp"].Value = timestamp;
                    insertCommand.Parameters["@RelativeID"].Value = data["ID"];
                    insertCommand.Parameters["@Start"].Value = data["Start"];
                    insertCommand.Parameters["@End"].Value = data["End"];
                    insertCommand.Parameters["@RootOrder"].Value = data["Order"];
                    insertCommand.Parameters["@Length"].Value = data["Length"];
                    insertCommand.Parameters["@Label"].Value = (string)data["Label"] == "" ? null : data["Label"];
                    insertCommand.Parameters["@EmergenceAngle"].Value = data["Emergence Angle"];
                    insertCommand.Parameters["@TipAngle"].Value = data["Tip Angle"];
                    insertCommand.Parameters["@StartReference"].Value = data["Start Reference"];
                    insertCommand.Parameters["@StartDistance"].Value = data["Start Distance"];
                    insertCommand.Parameters["@Spline"].Value = data["Spline"];
                    insertCommand.Parameters["@HullArea"].Value = data["Hull Area"];
                    insertCommand.Parameters["@PrimaryParentRelativeID"].Value = data["Primary Parent Relative ID"];
                    insertCommand.Parameters["@ParentRelativeID"].Value = data["Parent Relative ID"];
                    insertCommand.Parameters["@ChildCount"].Value = data["Lateral Count"];
                    insertCommand.Parameters["@User"].Value = username;
                    insertCommand.Parameters["@CompleteArchitecture"].Value = alllaterals ? 1 : 0;
                    
                    insertCommand.ExecuteNonQuery();
                }

                // Insert source image
                if (imageData != null)
                {
                    // Query
                    string imageInsertString = "INSERT INTO images (Tag, Stamp, Image) VALUES (@Tag, @Stamp, @Image)";

                    MySqlCommand imageInsertCommand = new MySqlCommand(imageInsertString, connection);
                    imageInsertCommand.Parameters.Add("@Tag", MySqlDbType.VarChar, 64);
                    imageInsertCommand.Parameters.Add("@Stamp", MySqlDbType.Timestamp);
                    imageInsertCommand.Parameters.Add("@Image", MySqlDbType.LongBlob);

                    imageInsertCommand.Parameters["@Tag"].Value = tag;
                    imageInsertCommand.Parameters["@Stamp"].Value = timestamp;
                    imageInsertCommand.Parameters["@Image"].Value = imageData;

                    imageInsertCommand.ExecuteNonQuery();
                }
            }
            catch
            {
                transaction.Rollback();
                return false;
            }

            // Commit
            transaction.Commit();
            return true;
        }

    }
}
