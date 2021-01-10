using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SabinIO.xEvent.App;
using SabinIO.xEvent.Lib;
using Serilog;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SabinIO.xEvent.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SetupStaticLogger();

            try
            {

                Log.Information("Starting");
                var foo = CreateHostBuilder(args);
                var host = foo.Build();
                host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An unhandled exception occurred.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void SetupStaticLogger()
        {
           /* var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
           */
            Log.Logger = new LoggerConfiguration()
                .CreateLogger();
            Log.Information("Hello Simon {bobby}", "bobby value");
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            new HostBuilder()
                .ConfigureLogging((hostContext, logging) =>
                {
                    logging.AddSerilog();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        // Setup Dependency Injection container.
                        .AddTransient<XEFileReader>()
                        // Specify the class that is the app/service that should be ran.
                        .AddHostedService<HostedApp>(s=>new HostedApp(args,s.GetService<XEFileReader>(),Log.Logger));
                    
                }          
            );
    }

}
public class HostedApp : IHostedService
{


    XEFileReader _classThatLogs;
    ILogger _log;

    string[] _args; 

    public HostedApp(string[] args, XEFileReader classThatLogs, ILogger log)
    {
        _classThatLogs = classThatLogs ?? throw new ArgumentNullException(nameof(classThatLogs));
        _args = args;
        _log = log;
    }

    public Task StartAsync( CancellationToken cancellationToken)
    {

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.Debug()
            .ReadFrom.KeyValuePairs(new List<KeyValuePair<string, string>>())
            .CreateLogger();
        Log.Information("Hello Simon {bobby}", "bobby value");

        // Create a root command with some options
        var rootCommand = new RootCommand();
        rootCommand.AddOption(new Option<string>("--tablename", description: "Tablename to load trace into"));
        rootCommand.AddOption(new Option<string>("--connection", description: "Connection string"));
        rootCommand.AddOption(new Option<FileInfo>("--filename", description: "Extended event filename"));
        rootCommand.AddOption(new Option<int>("--batchsize", getDefaultValue: () => 1000000, description: "Size of batches sent to bulk copy"));
        rootCommand.AddOption(new Option<string>("--fields", description: "names of fields to load from extended events"));
        rootCommand.AddOption(new Option<bool>("--debug", getDefaultValue: () => false, description: "outputs debug information to the standard out"));

        rootCommand.Description = "Extended event bulk loader ";
        Log.Information("boo");
        


        // Note that the parameters of the handler method are matched according to the names of the options
        rootCommand.Handler = CommandHandler.Create<int, string, string, string, FileInfo, bool>(
            async (batchsize, tablename, connection, fields, filename, debug) =>
            {
                if (debug)
                {
                    ConsoleTraceListener consoleTracer = new ConsoleTraceListener();
                    Trace.Listeners.Add(consoleTracer);
                }
                Log.Information("In handler");
                Console.WriteLine($"The value for --batchsize is: {batchsize}");
                Console.WriteLine($"The value for --filename is: {filename?.FullName ?? "null"}");
                Console.WriteLine($"The value for --connection is: {connection}");
                Console.WriteLine($"The value for --table is: {tablename}");
                Console.WriteLine($"The value for --fields is: {fields}");



                XEFileReader eventStream = _classThatLogs;
                eventStream.filename = filename.FullName;
                    eventStream.connection = connection;
                    eventStream.tableName = tablename;
                
                try
                {
                    var (rowsread, rowsinserted) = await eventStream.ReadAndLoad(fields?.Split(","));

                    Console.WriteLine($"rows read        {rowsread}");
                    Console.WriteLine($"rows bulk loaded {rowsinserted}");
                }
                catch (Exception ex)
                {
                    Console.Error.Write(ex.Message);
                    throw ex;
                }

            });
        // Parse the incoming args and invoke the handler


        return rootCommand.InvokeAsync(_args);
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;//throw new NotImplementedException();
    }

}
