using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Configuration;
using System.Net.Mail;
using MOE.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Timers;
using MOE.Common.Models.Repositories;

namespace DecodeATMSNowLogs
{
    class Program
    {
        private static string CWD;
        private static bool writeToConsole;
        private static bool deleteFiles;
        private static string connectionString;
        private static DateTime earliestAccepted;
        private static int commentTypeId;
        private static string folderPrefix;
        static void Main(string[] args)
        {
            CWD = Properties.Settings.Default.ATMSNowLogsPath;
            writeToConsole = Properties.Settings.Default.WriteToConsole;
            deleteFiles = Properties.Settings.Default.DeleteFiles;
            connectionString = ConfigurationManager.ConnectionStrings["SPM"].ConnectionString;
            earliestAccepted = Properties.Settings.Default.EarliestAcceptableDate;
            commentTypeId = Properties.Settings.Default.CommentTypeID;
            folderPrefix = Properties.Settings.Default.ATMSNowFolderPrefix;

            new Program().SaveEvents();
        }

        private void WriteToConsole(string msg)
        {
            if (!writeToConsole)
                return;
            Console.WriteLine("{0}: {1}", DateTime.Now, msg);
        }

        // standard Cubic filename format such as TRAF_01030_2022_03_21_1100.dat
        // and 60 minutes per file
        private int ExistingRecords(string signal, string file, IControllerEventLogRepository celRepository)
        {
            string[] filename = file.Split('_').Reverse().Take(4).Reverse().ToArray();
            DateTime start;
            try { 
            start = new DateTime(Int32.Parse(filename[0]),
                                        Int32.Parse(filename[1]),
                                        Int32.Parse(filename[2]),
                                        Int32.Parse(filename[3].Substring(0, 2)),
                                        0,
                                        0,
                                        0);
            }
            catch
            {
                WriteToConsole($"Unexpected filename: {file}");
                return -1;
            }
            var end = start + new TimeSpan(0, 1, 59, 59, 999);
            return celRepository.GetRecordCount(signal, start, end);
        }

        // Method to write the decoded log to the database.
        // this is where most of the work is done.

        // The only way we match signalid to the collected logs is by the directory name.
        private void SaveEvents()
        {
            DateTime startTime = new DateTime();
            DateTime endTime = new DateTime();
            TimeSpan elapsedTime = new TimeSpan();
            List<string> dirList = new List<string>();
            List<string> fileList = new List<string>();

            var lastrecords = new Dictionary<string, DateTime>();
            var countrecords = new Dictionary<string, int>();
            IControllerEventLogRepository celRepository = ControllerEventLogRepositoryFactory.Create();
            var commentRrepository = MetricCommentRepositoryFactory.Create();

            var lookup = commentRrepository.GetLatestCommentBySignalForType(commentTypeId);
            var dirToSignalMapping = new Dictionary<string, string>();

            foreach (string s in Directory.GetDirectories(CWD))
            {
                var atmsNowId = s.Split(new char[] { '\\' }).Last().Replace(folderPrefix, string.Empty);
                if (lookup.ContainsValue(atmsNowId))
                {
                    dirToSignalMapping.Add(s, lookup.Where(v => v.Value == atmsNowId).FirstOrDefault().Key);
                }
                else
                {
                    WriteToConsole($"Unable to find a SignalID for {s}");
                    continue;
                }

               // dirList.Add(s);
                var signalID = dirToSignalMapping[s];
                lastrecords.Add(signalID, celRepository.GetMostRecentRecordTimestamp(signalID, DateTime.Now.AddMinutes(60)));
                foreach (var file in Directory.GetFiles(s))
                {
                    countrecords.Add(file, ExistingRecords(signalID, file, celRepository));
                }
            }

            var options = new ParallelOptions { MaxDegreeOfParallelism = Convert.ToInt32(Properties.Settings.Default.MaxThreads) };
            Parallel.ForEach(dirToSignalMapping.Keys, options, dir =>
            {
                //get the name of the directory and casting it to an int
                //This is the only way the program knows the signal number of the controller.
                string[] strsplit = dir.Split(new char[] { '\\' });
                string dirname = strsplit.Last();
                string sigid = dirToSignalMapping[dir];
                var mostRecent = lastrecords[sigid];
                WriteToConsole($"Starting signal {sigid} reading from {dirname}");

                var options1 = new ParallelOptions { MaxDegreeOfParallelism = Convert.ToInt32(Properties.Settings.Default.MaxThreads) };
                foreach (var file in Directory.GetFiles(dir, "*.csv").OrderBy(f => f))
                {
                    // Records per file is lines minus 7 (6 header rows and 1 newline at the end)
                    if (countrecords[file] >= File.ReadAllLines(file).Length - 7)
                    {
                        WriteToConsole(String.Format("Skipping {0}{1}.", (deleteFiles ? " and deleting" : ""), file));
                        if (deleteFiles)
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch (Exception e)
                            {
                                WriteToConsole(String.Format("Unable to delete {0}: {1}", file, e.Message));
                            }
                        }
                        continue;
                    }

                    DataTable elTable = new DataTable();

                    elTable.Columns.Add("SignalId", typeof(String));
                    elTable.Columns.Add("Timestamp", typeof(DateTime));
                    elTable.Columns.Add("EventCode", typeof(Int32));
                    elTable.Columns.Add("EventParam", typeof(Int32));

                    startTime = DateTime.Now;
                    int lineNumber = 0;
                    int skipped = 0;
                    int skippedEarly = 0;

                    foreach (string line in File.ReadAllLines(file).Skip(6))
                    {
                        if (line.Contains(','))
                        {
                            //split the line on commas and assign each split to a var
                            string[] lineSplit = line.Split(new char[] { ',' });
                            DateTime timeStamp = new DateTime();
                            int eventCode = 0;
                            int eventParam = 0;
                            bool lineError = false;
                            lineNumber += 1;
                            //it might happen that the character on the line are not quite right.
                            //the Try/catch stuff is an attempt to deal with that.
                            try
                            {
                                timeStamp = Convert.ToDateTime(lineSplit[0]);
                            }
                            catch
                            {

                                WriteToConsole($"Error converting {lineSplit[0]} to Datetime on line {lineNumber}.");
                                lineError = true;
                            }

                            if (timeStamp <= earliestAccepted)
                            {
                                skippedEarly += 1;
                                continue;
                            }

                            if (timeStamp <= mostRecent)
                            {
                                skipped += 1;
                                continue;
                            }

                            try
                            {
                                eventCode = Convert.ToInt32(lineSplit[1]);
                                eventParam = Convert.ToInt32(lineSplit[2]);
                            }
                            catch
                            {
                                WriteToConsole($"Integer conversion error {lineSplit[1]} or {lineSplit[2]} on line {lineNumber}.");
                                lineError = true;
                            }

                            //If there were no errors on the line, then put the line into the bulk queue
                            if (!lineError)
                            {
                                try
                                {
                                    elTable.Rows.Add(sigid, timeStamp, eventCode, eventParam);
                                }
                                catch (Exception ex)
                                {
                                    WriteToConsole(ex.Message.ToString());
                                }
                            }
                        }
                    }

                    endTime = DateTime.Now;
                    elapsedTime = endTime - startTime;
                    WriteToConsole($"Read {lineNumber} lines from {file} in {elapsedTime.TotalSeconds} seconds.");
                    if (skipped > 0)
                    {
                        WriteToConsole($"Skipped {skipped} existing records");
                    }
                    
                    if (skippedEarly > 0)
                    {
                        WriteToConsole($"Skipped {skippedEarly} records prior to {earliestAccepted}");
                    }

                    MOE.Common.Business.BulkCopyOptions Options = new MOE.Common.Business.BulkCopyOptions(connectionString, Properties.Settings.Default.DestinationTableName,
                        Properties.Settings.Default.WriteToConsole, Properties.Settings.Default.forceNonParallel, Properties.Settings.Default.MaxThreads, false,
                        Properties.Settings.Default.EarliestAcceptableDate, Properties.Settings.Default.BulkCopyBatchSize, Properties.Settings.Default.BulkCopyTimeOut);

                    // The Signal class has a static method to insert the table into the DB.  We are using that.
                    MOE.Common.Business.SignalFtp.BulktoDb(elTable, Options, Properties.Settings.Default.DestinationTableName);

                    if (deleteFiles)
                    {
                        try
                        {
                            File.Delete(file);
                            WriteToConsole($"{file} Deleted");
                        }
                        catch (SystemException sysex)
                        {
                            WriteToConsole($"{sysex} while Deleting {file}, waiting 100 ms before trying again");
                            Thread.Sleep(100);
                            try
                            {
                                File.Delete(file);
                            }
                            catch
                            {
                                WriteToConsole("Failed");
                            }    
                        }
                        catch (Exception ex)
                        {
                            WriteToConsole($"{ex.Message} while deleting file {file}");
                        }
                    }
                } 
            }
             );
        }
    }
}
