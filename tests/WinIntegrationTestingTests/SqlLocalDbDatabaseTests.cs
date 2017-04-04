using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinIntegrationTesting;

namespace WinIntegrationTestingTests
{
    [TestClass]
    public class SqlLocalDbDatabaseTests
    {
        [TestMethod]
        public void TestNewDatabase()
        {
            string dbName = "MyTestDatabase" + Guid.NewGuid().ToString();

            var localDb = SqlLocalDbDatabase.Create(new SqlLocalDbCreateDatabaseOptions
            {
                DatabaseName = dbName,

                // Deleting LocalDB databases can be time consuming. DeleteAfterThisProcessExits will 
                // launch WinIntegrationTesting.exe as a separate process that monitors the current process
                // and deletes the database after the test process exits. This can significantly reduce time
                // to run a suite of single-threaded database tests.
                DeleteAfterThisProcessExits = true
            });

            using (var connection = new SqlConnection(localDb.ConnectionString))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT db_name();";

                    object result = cmd.ExecuteScalar();

                    Assert.AreEqual(dbName, result.ToString());
                }
            }
        }
    }
}
