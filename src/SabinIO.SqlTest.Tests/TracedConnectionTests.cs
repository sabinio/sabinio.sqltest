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

            for (int i = 0; i < 1000000; i++)
            {

                TestConnection.Connection.Query("select @@version");

            }

            var results = TestConnection.Statements().ToList();
            
            Assert.That(results.Where(_ => _.name == "sql_statement_completed").Select(_=> _.fields["sql_text"]),Is.SupersetOf(new string[] { "select 'Simon'" }));

        }

        CancellationToken CT;


        [Test]
        public void foo()
        {
            using var T = new TracedConnection() { ConnectionStr = connectionString };
            T.GetSessions();
        }


        [TestCase]
        public async Task  LiveStreamCapturesTheEvents()
        {
            using var T = new TracedConnection() { ConnectionStr = connectionString };
            T.Init();
            var samplexml = new XELiveEventStreamer($"{T.ConnectionStr};Application Name=Trace" , T.XEventSessionName);

            var tokenSource2 = new CancellationTokenSource();
            CT = tokenSource2.Token;
            List<IXEvent> bob = new List<IXEvent>();

           Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId }");

            var TraceReader = Task.Run(async ()=> await samplexml.ReadEventStream(
                 evt =>
                 {
                     Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId }");
                     TestContext.WriteLine($"Logging Event {evt.Name}");
                     bob.Add(evt);
                     return Task.CompletedTask;
                 }, CT)).ConfigureAwait(false);

            await Task.Delay(100);

            
            var result = T.Execute<string>("select 'Simon'");
            
            Assert.That(() => result == "Simon");
            T.Connection.Query("select @@version");
            
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.ElapsedMilliseconds < 10000)
            {
                await Task.Delay(1000);
            }
            sw.Stop();
            Assert.That(bob, Has.Count.EqualTo(6));
            Assert.That(() => bob.Where(_ => _.Name == "sql_statement_completed" && ((string)_.Actions["sql_text"]).StartsWith("select 'Simon'")).Any());




        }
    }
   
}
