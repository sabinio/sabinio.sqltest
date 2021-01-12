using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SabinIO.xEvent.Lib;
using Serilog;
using Serilog.Events;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SabinIO.xEvent.App
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {

            try
            {

                //Needed to capture anything before the full logging is implemented
                var config = new LoggerConfiguration().WriteTo.Console(outputTemplate: "{Message}\n");

                // Create a root command with some options
                var rootCommand = new RootCommand()
                {
                    Handler = Handler
                };
                rootCommand.AddOption(new Option<string>("--tablename", description: "Tablename to load trace into"));
                rootCommand.AddOption(new Option<string>("--connection", description: "Connection string"));
                rootCommand.AddOption(new Option<FileInfo>("--filename", description: "Extended event filename"));
                rootCommand.AddOption(new Option<int>("--batchsize", getDefaultValue: () => 1000000, description: "Size of batches sent to bulk copy"));
                rootCommand.AddOption(new Option<string>("--fields", description: "names of fields to load from extended events"));
                rootCommand.AddOption(new Option<string>("--logFile", description: "name of log file"));
                rootCommand.AddOption(new Option<bool>("--debug", getDefaultValue: () => false, description: "outputs debug information to the standard out"));
                rootCommand.AddOption(new Option<int>("--logLevel", getDefaultValue: () => -1, description: "outputs debug information to the standard out"));
                rootCommand.AddOption(new Option<int>("--progress", getDefaultValue: () => 1000000, description: "how many rows to be moved before notifying of progress"));

                var p = rootCommand.Parse(args);
                var logFilePath = "";
                if (p.HasOption("--logFile"))
                {
                    logFilePath = p.ValueForOption<FileInfo>("--logFile").FullName;
                }
                Log.Logger = config.CreateLogger();

                rootCommand.Description = "Extended event bulk loader ";


                var t = new CommandLineBuilder(rootCommand)
                                    .UseDefaults()
                                    .CancelOnProcessTermination()
                                    .UseHost(Host.CreateDefaultBuilder,
                                    host =>
                                        host
                                        .ConfigureServices(services =>
                                        services
                                            .AddTransient<XEFileReader>()
                                            .AddOptions<XEventAppOptions>().BindCommandLine()

                                        ).UseSerilog()
                                       .UseConsoleLifetime(c => c.SuppressStatusMessages = true)
                                        )
                                    .UseMiddleware(i => SetupStaticLogger(i.ParseResult))
                                  ;

                var b = t.Build();

                Log.CloseAndFlush();
                return await b.InvokeAsync(args).ConfigureAwait(false);
            }
            catch (Exception ex)
            {

                Log.Fatal(ex, "An unhandled exception occurred.");
                return -1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void SetupStaticLogger(ParseResult ThisCmd)
        {

            var config = new LoggerConfiguration().Enrich.FromLogContext();
            var o = ThisCmd.FindResultFor(new Option("--logLevel"));

            var logLevel = ThisCmd.CommandResult.OptionResult("--logLevel");//
            //.ValueForOption<int>("--logLevel");
            ThisCmd.HasOption("--logLevel");

            config.WriteTo.Console( outputTemplate: "{Message}\n");

            var logFile = ThisCmd.ValueForOption<FileInfo>("--logFile");
            if (logFile != null) config.WriteTo.File(logFile.FullName);

            config.WriteTo.Debug();

            Log.Logger = config.CreateLogger();
            var listener = new SerilogTraceListener.SerilogTraceListener();
            Trace.Listeners.Add(listener);

        }


        internal static ICommandHandler Handler { get; } = CommandHandler.Create(
     async (IConsole console, IHost host, CancellationToken cancelToken) =>
     {

         var Option = host.Services.GetRequiredService<IOptions<XEventAppOptions>>().Value;
         var (batchsize, tablename, connection, fields, filename, debug, logLevel, progress) = Option; 

         Log.Information("The value for --batchsize is: {batchsize}", batchsize);
         Log.Information($"    --filename is: {filename?.FullName ?? "null"}");
            Log.Information($"    --connection is: {connection}");
            Log.Information($"    --table is: {tablename}");
            Log.Information($"    --fields is: {fields}");
            Log.Information($"    --batchsize is: {batchsize}");
            Log.Information($"    --progress is: {progress}");
         Log.Information($"    --logLevel is: {logLevel}");
         
         XEFileReader eventStream = host.Services.GetRequiredService<XEFileReader>();

         eventStream.filename = filename?.FullName;
         eventStream.connection = connection;
         eventStream.tableName = tablename;
         eventStream.batchsize = batchsize;
         eventStream.progress = progress;

         try
         {
             Stopwatch sw = new Stopwatch();
             sw.Start();
             var (rowsread, rowsinserted) = await eventStream.ReadAndLoad(fields?.Split(","), cancelToken);
             sw.Stop();
             if (cancelToken.IsCancellationRequested) { Log.Information("Processing aborted due to cancellation request, Numbers below are rows processed so far"); }
             Log.Information($"rows read        {rowsread}");
             Log.Information($"rows bulk loaded {rowsinserted}");
             Log.Information($"rows/ms          {rowsinserted/sw.ElapsedMilliseconds}");

         }
         catch (Exception ex)
         {
             
            Log.Error(ex,$"Error occurred processing the file {filename.FullName}\n {ex.Message}");

         }
     }
     );

    }


    public class XEventAppOptions
    {

#pragma warning disable IDE1006 // Naming Styles
        public int batchsize { get; set; }
        public string tablename { get; set; }
        public string connection { get; set; }
        public string fields { get; set; }
        public FileInfo filename { get; set; }
        public bool debug { get; set; }
        public int logLevel { get; set; }
        public int progress { get; set; }

#pragma warning restore IDE1006 // Naming Styles

        public void Deconstruct(out int batchsize, out string tablename, out string connection, out string fields, out FileInfo filename, out bool debug, out int logLevel, out int progress)
        { batchsize = this.batchsize; tablename = this.tablename; connection = this.connection; fields = this.fields; filename = this.filename; debug = this.debug; logLevel = this.logLevel; progress = this.progress; }
    }



    public class ConsoleWriterEventArgs : EventArgs
    {
        public string Value { get; private set; }
        public ConsoleWriterEventArgs(string value)
        {
            Value = value;
        }
    }




    //XEFileReader eventStream = new XEventStream;// _classThatLogs;
    //eventStream.filename = filename.FullName;
    //eventStream.connection = connection;
    //eventStream.tableName = tablename;

    //try
    //{
    //    var (rowsread, rowsinserted) = await eventStream.ReadAndLoad(fields?.Split(","));

    //    Console.WriteLine($"rows read        {rowsread}");
    //    Console.WriteLine($"rows bulk loaded {rowsinserted}");
    //}
    //catch (Exception ex)
    //{
    //    Console.Error.Write(ex.Message);
    //    throw ex;
    //}
}
