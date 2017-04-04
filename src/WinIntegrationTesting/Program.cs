using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinIntegrationTesting
{
    public class Program
    {
        internal const string CleanupTempWebFolderCommand = "CleanupTempWebFolder";
        internal const string DeleteLocalDbDatabaseCommand = "DeleteLocalDbDatabase";

        static void Main(string[] args)
        {
            // A limited set of commands can be processed by WinIntegrationTesting
            // to allow for cleanup tasks to complete independent of the original
            // test process which will frequently be terminated early as part of
            // the debugging process.

            // Commands:
            //
            // CleanupTempWebFolder [tempWebFolder] [iisExpressProcId]
            //    Purpose: Delete temp web folder after IIS Express exits.

            if (args == null || args.Length == 0)
            {
                return;
            }

            string command = args[0];
            switch (command)
            {
                case CleanupTempWebFolderCommand:
                    {
                        string tempWebFolder = args[1];
                        int iisExpressProcId = Int32.Parse(args[2]);

                        try
                        {
                            var iisExpressProc = Process.GetProcessById(iisExpressProcId);
                            iisExpressProc.WaitForExit();
                        }
                        catch
                        {
                            // Process must have already ended.
                        }

                        IISExpressTestLauncher.DeleteTempWebFolder(tempWebFolder);
                        break;
                    }

                case DeleteLocalDbDatabaseCommand:
                    {
                       
                        string masterConnectionString = args[1];
                        string dbName = args[2];
                        string dbPath = args[3];
                        int waitProcId = Int32.Parse(args[4]);

                        if (waitProcId != -1)
                        {
                            try
                            {
                                var waitProc = Process.GetProcessById(waitProcId);
                                waitProc.WaitForExit();
                            }
                            catch
                            {
                                // Process must have already ended.
                            }
                        }

                        SqlLocalDbDatabase.DeleteDatabase(masterConnectionString, dbName, dbPath);
                        break;
                    }
            }
        }
    }
}
