using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Dapper;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.XEvent.XELite;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using SabinIO.SqlTest;

namespace SabinIO.SqlTest.Tests
{
    [TestFixture]
        public class TracedConnectionTests
    {
 public string connectionString { get { return TestContext.Parameters["TraceConnectionString"]; } }

        [TestCase]
        public void CanReadStatementsForSession()
        {

            using var TestConnection = new TracedConnection() { ConnectionStr = connectionString };
            var result = TestConnection.Execute<string>("select 'Simon'");

            Assert.That(() => result == "Simon");

            for (int i = 0; i < 10; i++)
            {

                TestConnection.Connection.Query("select @@version");

            }

            var results = TestConnection.Statements().ToList();
            
            Assert.That(results.Where(_ => _.name == "sql_statement_completed").Select(_=> _.fields["sql_text"]),Is.SupersetOf(new string[] { "select 'Simon'" }));

        }

        CancellationToken CT;


        [Test]
        public void TestGetSessions()
        {
            using var T = new TracedConnection() { ConnectionStr = connectionString };
            T.GetSessions();
        }


        [Test]
       // [Ignore("Not working")]
        public async Task  LiveStreamCapturesTheEvents()
        {
            using var T = new TracedConnection() { ConnectionStr = connectionString };
            T.Init();

            var XEConnection = new SqlConnectionStringBuilder(connectionString)
                .InitialCatalog("master")
                .ApplicationName("SabinIO.XETrace")
                .ConnectionString;


            var samplexml = new XELiveEventStreamer(XEConnection , T.XEventSessionName);
            
            var tokenSource2 = new CancellationTokenSource();
            CT = tokenSource2.Token;
            List<IXEvent> bob = new List<IXEvent>();

            var TraceReader = Task.Run(async () =>
            { 
               try
               {
                   await
                   samplexml.ReadEventStream(
                   evt =>
                   {
                       Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId }");
                       TestContext.WriteLine($"Logging Event {evt.Name}");
                       bob.Add(evt);
                       return Task.CompletedTask;
                   }, CT);
               }
               catch
               {

               }
           });

            //this is needed to wait for the background thread to start
            await Task.Delay(100);

            
            var result = T.Execute<string>("select 'Simon'");
            
            Assert.That(() => result == "Simon");
            T.Connection.Query("select @@version");
            
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.ElapsedMilliseconds < 1000)
            {
                await Task.Delay(100);
            }
            sw.Stop();
            Assert.That(bob, Has.Count.EqualTo(6));
            Assert.That(() => bob.Where(_ => _.Name == "sql_statement_completed" && ((string)_.Actions["sql_text"]).StartsWith("select 'Simon'")).Any());




        }
    }
   public static class ConnectionStringExtensions
    {
        public static SqlConnectionStringBuilder InitialCatalog(this SqlConnectionStringBuilder builder,string InitialCatalog) {
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
