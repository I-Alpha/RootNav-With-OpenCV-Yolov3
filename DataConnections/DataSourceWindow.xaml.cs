using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using RootNav.Data;
using System.ComponentModel;
using RootNav.Data.IO;
using RootNav.Data.IO.Databases;

namespace RootNav.Data
{
    /// <summary>
    /// Interaction logic for DataSourceWindow.xaml
    /// </summary>
    public partial class DataSourceWindow : Window
    {
        public ConnectionParams ConnectionInfo { get; set; }

        public DataSourceWindow()
        {
            this.InheritanceBehavior = InheritanceBehavior.SkipAllNow;

            InitializeComponent();

            // Attempt to read encrypted storage
            string xmlData = EncryptedStorage.ReadEncryptedString("C_DATA");

            if (xmlData != null && xmlData != "")
            {
                ConnectionInfo = ConnectionParams.FromXML(xmlData);
                
                if (ConnectionInfo == null)
                {
                    return;
                }

                // Populate text boxes
                serverBox.Text = ConnectionInfo.Server;
                portBox.Text = ConnectionInfo.Port.ToString();
                databaseBox.Text = ConnectionInfo.Database;
                userBox.Text = ConnectionInfo.Username;
                passwordBox.Password = ConnectionInfo.Password;
                directoryBox.Text = ConnectionInfo.Directory;

                // Correct source selection
                if (ConnectionInfo.Source == ConnectionSource.RSMLDirectory)
                {
                    rsmlRadioButton.IsChecked = true;
                }
            }
            else
            {
                ConnectionInfo = null;
            }
        }

        private void databaseRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized)
            {
                serverLabel.IsEnabled = true;
                serverBox.IsEnabled = true;
                portLabel.IsEnabled = true;
                portBox.IsEnabled = true;
                databaseLabel.IsEnabled = true;
                databaseBox.IsEnabled = true;
                userLabel.IsEnabled = true;
                userBox.IsEnabled = true;
                passwordLabel.IsEnabled = true;
                passwordBox.IsEnabled = true;

                directoryLabel.IsEnabled = false;
                directoryBox.IsEnabled = false;
                this.findDirectoryButton.IsEnabled = false;
            }
        }

        private void rsmlRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized)
            {
                serverLabel.IsEnabled = false;
                serverBox.IsEnabled = false;
                portLabel.IsEnabled = false;
                portBox.IsEnabled = false;
                databaseLabel.IsEnabled = false;
                databaseBox.IsEnabled = false;
                userLabel.IsEnabled = false;
                userBox.IsEnabled = false;
                passwordLabel.IsEnabled = false;
                passwordBox.IsEnabled = false;


                directoryLabel.IsEnabled = true;
                directoryBox.IsEnabled = true;
                this.findDirectoryButton.IsEnabled = true;
            }
        }

        System.Windows.Forms.FolderBrowserDialog fbdl = new System.Windows.Forms.FolderBrowserDialog() { RootFolder = Environment.SpecialFolder.Desktop };
        private void findDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (fbdl.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.directoryBox.Text = fbdl.SelectedPath;
            }

        }

        private void acceptButton_Click(object sender, RoutedEventArgs e)
        {
            // Update connection params
            this.ConnectionInfo = new ConnectionParams() { Server = serverBox.Text, Port = portBox.Text, Database = databaseBox.Text, Directory = directoryBox.Text, Password = passwordBox.Password, Username = userBox.Text };
            this.ConnectionInfo.Source = (bool)this.databaseRadioButton.IsChecked ? ConnectionSource.MySQLDatabase : ConnectionSource.RSMLDirectory;

            // Save connection params
            EncryptedStorage.SaveEncryptedString("C_DATA", this.ConnectionInfo.ToXML());

            this.DialogResult = true;
            this.Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Do not save connection data
            this.DialogResult = false;
            this.Close();
        }

        private void testButton_Click(object sender, RoutedEventArgs e)
        {
            // Update connection params
            this.ConnectionInfo = new ConnectionParams() { Server = serverBox.Text, Port = portBox.Text, Database = databaseBox.Text, Directory = directoryBox.Text, Password = passwordBox.Password, Username = userBox.Text };
            this.ConnectionInfo.Source = (bool)this.databaseRadioButton.IsChecked ? ConnectionSource.MySQLDatabase : ConnectionSource.RSMLDirectory;
            
            if (this.ConnectionInfo.Source == ConnectionSource.MySQLDatabase)
            {
                this.notConnectedBorder.Visibility = System.Windows.Visibility.Hidden;
                this.connectingBorder.Visibility = System.Windows.Visibility.Visible;
                this.failedConnectionBorder.Visibility = System.Windows.Visibility.Hidden;
                this.ConnectionSuccessful = false;

                BackgroundWorker bw = new BackgroundWorker() { WorkerReportsProgress = false };
                bw.DoWork += bw_DoWork;
                bw.RunWorkerCompleted += bw_RunWorkerCompleted;
                bw.RunWorkerAsync();
            }
            else
            {
                // File connection, directory must exist
                bool exists = System.IO.Directory.Exists(this.ConnectionInfo.Directory);
                if (!exists)
                {
                    this.failedConnectionLabel.Content = "Directory Not Found"; 
                    this.failedConnectionBorder.Visibility = System.Windows.Visibility.Visible;
                    this.notConnectedBorder.Visibility = System.Windows.Visibility.Hidden;
                    this.connectingBorder.Visibility = System.Windows.Visibility.Hidden;
                    this.connectedBorder.Visibility = System.Windows.Visibility.Hidden;
                }
                else
                {
                    HashSet<string> validExtensions = new HashSet<string>() { ".rsml", ".RSML" };
                    int count = 0;
                    foreach (var file in System.IO.Directory.EnumerateFiles(this.ConnectionInfo.Directory))
                    {
                        if (validExtensions.Contains(System.IO.Path.GetExtension(file)))
                        {
                            count++;
                        }
                    }

                    this.connectedLabel.Content = count == 1 ? "1 file found" : count.ToString() + " files found";
                    this.failedConnectionBorder.Visibility = System.Windows.Visibility.Hidden;
                    this.notConnectedBorder.Visibility = System.Windows.Visibility.Hidden;
                    this.connectingBorder.Visibility = System.Windows.Visibility.Hidden;
                    this.connectedBorder.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        private bool ConnectionSuccessful { get; set; }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (errorCode == -1)
            {
                this.failedConnectionLabel.Content = "Access Denied";
            }
            else
            {
                this.failedConnectionLabel.Content = "No Connection";
            }

            if (!this.ConnectionSuccessful)
            {
                // UI changes
                this.connectingBorder.Visibility = Visibility.Hidden;
                this.failedConnectionBorder.Visibility = Visibility.Visible;
            }
            else
            {
                // UI changes
                this.connectedLabel.Content = "Success";
                this.connectingBorder.Visibility = Visibility.Hidden;
                this.connectedBorder.Visibility = Visibility.Visible;
            }
        }

        private int errorCode = 0;

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            string connection = this.ConnectionInfo.ToMySQLConnectionString();
            ConnectionSuccessful = new MySQLDatabaseManager().ValidateConnection(connection, out this.errorCode);
        }
    }
}
