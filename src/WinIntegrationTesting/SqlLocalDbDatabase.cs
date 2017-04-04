using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WinIntegrationTesting
{
    /// <summary>
    /// Helper class for spinning up test copies of LocalDb databases.
    /// </summary>
    public class SqlLocalDbDatabase
    {
        private const string DefaultMasterConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Master;Integrated Security=True";

        private string masterConnectionString;
        private string connectionString;
        private string databaseName;
        private string databasePath;

        private SqlLocalDbDatabase()
        {
        }

        private SqlLocalDbDatabase(string masterConnectionString, string databaseName, string databasePath)
        {
            this.masterConnectionString = masterConnectionString;
            this.databaseName = databaseName;
            this.databasePath = databasePath;

            // Sample Connection String: @"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;MultipleActiveResultSets=True;AttachDbFileName=" + this.databasePath;
            // We assume this is localdb master connection string, so we will remove initial catalog and add AttachDbFilename.
            var connStrBuilder = new SqlConnectionStringBuilder(masterConnectionString);
            connStrBuilder["Initial Catalog"] = null;
            connStrBuilder.AttachDBFilename = databasePath;

            this.connectionString = connStrBuilder.ToString();
        }

        public string DatabaseName
        {
            get { return this.databaseName; }
        }

        public string DatabasePath
        {
            get { return this.databasePath; }
        }

        public string ConnectionString
        {
            get { return this.connectionString; }
        }

        /// <summary>
        /// Attempt to delete the database and wait for operation to complete. This will be performed
        /// within the current process.
        /// </summary>
        public void DeleteAndWait()
        {
            DeleteDatabase(this.masterConnectionString, this.databaseName, this.databasePath);
        }

        /// <summary>
        /// TryDelete will attempt to delete the database but will not return any errors if cleanup fails.
        /// This will perform the deletion from a separate process.
        /// </summary>
        /// <param name="afterProcessIdExits">Wait for process with this id to exit before deleting database (use -1 to delete immediately).</param>
        public void TryDelete(int afterProcessIdExits = -1)
        {
            try
            {
                // Using -1 for procId will try to delete the database immediately. By spinning up a separate process
                // to handle deletion, the 
                DeleteDatabaseAfterProcExits(this.masterConnectionString, this.databaseName, this.databasePath, afterProcessIdExits);
            }
            catch
            {
            }
        }
        
        public static SqlLocalDbDatabase AttachToFile(string databasePath)
        {
            if (!File.Exists(databasePath))
            {
                throw new Exception("File not found: " + databasePath);
            }

            // Sample Connection String: @"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;MultipleActiveResultSets=True;AttachDbFileName=" + this.databasePath;
            // We assume this is localdb master connection string, so we will remove initial catalog and add AttachDbFilename.
            var connStrBuilder = new SqlConnectionStringBuilder(DefaultMasterConnectionString);
            connStrBuilder["Initial Catalog"] = null;
            connStrBuilder.AttachDBFilename = databasePath;

            string connectionString = connStrBuilder.ToString();

            var localDbDatabase = new SqlLocalDbDatabase()
            {
                // TODO: Determine associated database name by querying the database itself.
                databaseName = Path.GetFileName(databasePath),
                databasePath = databasePath,
                masterConnectionString = DefaultMasterConnectionString,
                connectionString = connectionString
            };

            return localDbDatabase;
        }

        public static SqlLocalDbDatabase Create(SqlLocalDbCreateDatabaseOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // Determine folder to use.
            string databaseFolderToUse = null;

            if (options.DatabaseFolderPath == null)
            {
                string folderName = Path.GetTempPath();
                string tempDbFolderName = Path.Combine(folderName, "TempDbs");
                if (!Directory.Exists(tempDbFolderName))
                {
                    Directory.CreateDirectory(tempDbFolderName);
                    databaseFolderToUse = tempDbFolderName;
                }
            }
            else
            {
                if (!Directory.Exists(options.DatabaseFolderPath))
                {
                    string parentDirectory = Path.GetDirectoryName(options.DatabaseFolderPath);
                    if (!Directory.Exists(parentDirectory))
                    {
                        throw new Exception($"Path and parent path not found: {options.DatabaseFolderPath}");
                    }
                    else
                    {
                        // We'll create the folder as long as parent path exists.
                        Directory.CreateDirectory(options.DatabaseFolderPath);
                    }
                }

                databaseFolderToUse = options.DatabaseFolderPath;
            }

            string databaseName = options.DatabaseName;
            if (databaseName == null)
            {
                // TODO: Check for invalid database names.
                throw new Exception("options.DatabaseName not set.");
            }

            string databasePath = Path.Combine(databaseFolderToUse, databaseName);

            if (File.Exists(databasePath))
            {
                if (options.DeleteExistingDatabaseAtSamePath)
                {
                    var existingDb = SqlLocalDbDatabase.AttachToFile(databasePath);
                    existingDb.DeleteAndWait();
                }
                else
                {
                    throw new Exception($"Database file already exists: {databasePath}");
                }
            }

            string masterConnectionStringToUse = options.MasterConnectionString ?? DefaultMasterConnectionString;

            CreateDatabase(
                masterConnectionStringToUse,
                databaseName: databaseName, 
                databaseFullPath: databasePath, 
                maxSizeMB: options.MaxSizeMB);            

            var localDbDatabase = new SqlLocalDbDatabase(masterConnectionStringToUse, databaseName, databasePath);

            if (options.DeleteAfterThisProcessExits)
            {
                int currentProcessId = Process.GetCurrentProcess().Id;
                localDbDatabase.TryDelete(afterProcessIdExits: currentProcessId);
            }

            return localDbDatabase;
        }


        private static void CreateDatabase(string masterConnectionString, string databaseName, string databaseFullPath, int maxSizeMB)
        {
            if (databaseName.Contains("'"))
            {
                throw new Exception("dbName contains single quote");
            }
            if (databaseFullPath.Contains("'"))
            {
                throw new Exception("dbFullPath contains single quote");
            }

            using (var connection = new SqlConnection(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Master;Integrated Security=True"))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        IF EXISTS(SELECT * FROM sys.databases WHERE name = @dbName)
                        BEGIN
                            BEGIN TRY 
	                            ALTER DATABASE [" + databaseName + @"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                                EXEC sp_detach_db @dbName;
                            END TRY
                            BEGIN CATCH
	                            DROP DATABASE [" + databaseName + @"]
                            END CATCH
                        END
             
                        EXEC ('CREATE DATABASE [' + @dbName + '] ON PRIMARY 
	                        (NAME = [' + @dbName + '], 
	                        FILENAME =''' + @filename + ''', 
	                        SIZE = 25MB, 
	                        MAXSIZE = " + maxSizeMB + @"MB, 
	                        FILEGROWTH = 5MB )')";

                    cmd.Parameters.AddWithValue("dbName", databaseName);
                    cmd.Parameters.AddWithValue("filename", databaseFullPath);

                    cmd.ExecuteNonQuery();
                }
            }

            if (!File.Exists(databaseFullPath))
            {
                throw new Exception("Database not created.");
            }            
        }


        private static bool TryDetachDatabase(string masterConnectionString, string dbName)
        {
            if (dbName.Contains("'"))
            {
                throw new Exception("dbName contains single quote");
            }

            try
            {                
                using (var connection = new SqlConnection(masterConnectionString))
                {
                    connection.Open();
                    SqlCommand cmd = connection.CreateCommand();
                    cmd.CommandText = @"
ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;                            
exec sp_detach_db '" + dbName + @"';
";
                    cmd.ExecuteNonQuery();

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void DeleteDatabase(string masterConnectionString, string databaseName, string databasePath)
        {            
            if (File.Exists(databasePath))
            {
                try
                {
                    File.Delete(databasePath);
                }
                catch
                {
                    // Process probably still has an open connection, so try to detech and then delete again.
                    TryDetachDatabase(masterConnectionString, databaseName);
                    File.Delete(databasePath);
                }                               
            }

            string logFilePath = databasePath + "_log.ldf";
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }
        }


        private static void DeleteDatabaseAfterProcExits(
            string masterConnectionString, 
            string databaseName,
            string databasePath, 
            int procId = -1)
        {
            Assembly assembly = typeof(SqlLocalDbDatabase).Assembly;
            string processPath = assembly.Location;

            if (!File.Exists(processPath))
            {
                throw new Exception("Command assembly not found on disk: " + processPath);
            }

            if (File.Exists(databasePath))
            {
                string escapedArgs = Program.DeleteLocalDbDatabaseCommand 
                    + " \"" + masterConnectionString.Replace("\\", "\\\\") + "\""
                    + " \"" + databaseName.Replace("\\", "\\\\") + "\""
                    + " \"" + databasePath.Replace("\\", "\\\\") + "\""
                    + " " + procId;

                var commandStartInfo = new ProcessStartInfo(processPath, escapedArgs)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(commandStartInfo);
            }
        }
    }


    public class SqlLocalDbCreateDatabaseOptions
    {
        /// <summary>
        /// Connection string to the master database for creating a database.
        /// </summary>
        public string MasterConnectionString { get; set; }


        public string DatabaseName { get; set; }

        public string DatabaseFolderPath { get; set; }


        /// <summary>
        /// If database already exists and desired path, delete it first.
        /// </summary>
        public bool DeleteExistingDatabaseAtSamePath { get; set; }


        /// <summary>
        /// If true, a watcher process will be launched to try to delete this database after the current process exits.
        /// </summary>
        public bool DeleteAfterThisProcessExits { get; set; }


        public int MaxSizeMB { get; set; } = 500;
    }
}
