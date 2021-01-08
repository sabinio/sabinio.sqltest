using sabin.io.xevent;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;

namespace XEvent.App
{
    public class Program
    {
        public static int Main(string[] args)
        {

            

            // Create a root command with some options
            var rootCommand = new RootCommand();
            rootCommand.AddOption(new Option<int>(
                    "--batchsize",
                    getDefaultValue: () => 1000000,
                    description: "Size of batches sent to bulk copy"));
            rootCommand.AddOption(
                   new Option<string>(
                    "--tablename",
                    description: "Tablename to load trace into"));
            rootCommand.AddOption(new Option<string>(
                    "--connection",
                    description: "Connection string")); 
            rootCommand.AddOption(new Option<FileInfo>(
                    "--filename",
                    description: "Extended event filename"));
            rootCommand.AddOption(new Option<string>(
                    "--fields",
                    description: "names of fields to load from extended events"
                    ));
            rootCommand.AddOption(new Option<bool>(
                    "--debug",
                    getDefaultValue: () => false,
                    description: "outputs debug information to the standard out"
                    ));

            rootCommand.Description = "Extended event bulk loader ";
          
            try
            {
                // Note that the parameters of the handler method are matched according to the names of the options
                rootCommand.Handler = CommandHandler.Create<int, string, string, string, FileInfo,bool>(
                    async (batchsize, tablename, connection, fields, filename,debug) =>
                 {
                     if (debug)
                     {
                         // Set the name of the trace listener, which helps identify this
                         // particular instance within the trace listener collection.
                         ConsoleTraceListener consoleTracer = new ConsoleTraceListener();

                         // Add the new console trace listener to
                         // the collection of trace listeners.
                         Trace.Listeners.Add(consoleTracer);
                     }

                     Console.WriteLine($"The value for --batchsize is: {batchsize}");
                     Console.WriteLine($"The value for --filename is: {filename?.FullName ?? "null"}");
                     Console.WriteLine($"The value for --connection is: {connection}");
                     Console.WriteLine($"The value for --table is: {tablename}");
                     Console.WriteLine($"The value for --fields is: {fields}");



                     XEFileReader eventStream = new XEFileReader(fields.Split(","))
                     {
                         filename = filename.FullName,
                         connection = connection,
                         tableName = tablename
                     };

                     var (rowsread, rowsinserted) = await eventStream.ReadAndLoad();

                     Console.WriteLine($"rows read        {rowsread}");
                     Console.WriteLine($"rows bulk loaded {rowsinserted}");

                 });
                // Parse the incoming args and invoke the handler
                var T = rootCommand.InvokeAsync(args);
                T.Wait();

                return 99;// T.Result;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                throw ex;
                
            }
        }
    }
}
