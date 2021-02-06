using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.XEvent.XELite;
using SabinIO.SqlTest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SabinIO.Sql.AppTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            LiveStreamCapturesTheEvents().ConfigureAwait(false).GetAwaiter().GetResult() ;

        }


        public static async Task LiveStreamCapturesTheEvents()
        {
            string connectionString= "data source=.;initial catalog=trace;trusted_Connection=true";
            using var T = new TracedConnection() { ConnectionStr = connectionString };
            T.Init();
            var XEConnection = new SqlConnectionStringBuilder(connectionString)
                .InitialCatalog("master")
                .ApplicationName("SabinIO.XETrace")
                .ConnectionString;


            var samplexml = new XELiveEventStreamer(XEConnection, T.XEventSessionName);

            var tokenSource2 = new CancellationTokenSource();
            var CT = tokenSource2.Token;
            List<IXEvent> bob = new List<IXEvent>();

            var TraceReader = Task.Run(async () =>
            {
                try
                {
                    await
                    samplexml.ReadEventStream(
                    evt =>
                    {
                        bob.Add(evt);
                        return Task.CompletedTask;
                    }, CT);
                }
                catch
                {

                }
            });

            await Task.Delay(100);


            var result = T.Execute<string>("select 'Simon'");

           
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.ElapsedMilliseconds < 1000)
            {
                await Task.Delay(100);
            }
            sw.Stop();




        }
    }

    public static class ConnectionStringExtensions
    {
        public static SqlConnectionStringBuilder InitialCatalog(this SqlConnectionStringBuilder builder, string InitialCatalog)
        {
            builder.InitialCatalog = InitialCatalog;
            return builder;
        }
        public static SqlConnectionStringBuilder DataSource(this SqlConnectionStringBuilder builder, string DataSource)
        {
            builder.DataSource = DataSource;
            return builder;
        }

        public static SqlConnectionStringBuilder ApplicationName(this SqlConnectionStringBuilder builder, string ApplicationName)
        {
            builder.ApplicationName = ApplicationName;
            return builder;
        }
    }
}
