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
 
        [TestCase]
        public void CanReadStatementsForSession()
        {

            using var TestConnection = new TracedConnection() { ConnectionStr = "data source=.;Trusted_Connection=True" };
            var result = TestConnection.Execute<string>("select 'Simon'");

            //            Task.WaitAll( result);
            Assert.That(() => result == "Simon");

            TestConnection.Connection.Query("select @@version");


            var results = TestConnection.Statements().ToList();

            Assert.That(() => results.Where(_ => _.name == "sql_statement_completed" && _.actions["sql_text"] == "select 'Simon'").Any());

        }

        CancellationToken CT;

        [TestCase]
        public async Task  LiveStreamCapturesTheEvents()
        {
            using var T = new TracedConnection() { ConnectionStr = "data source=.;Trusted_Connection=True" };
            T.Init();
            var samplexml = new XELiveEventStreamer($"{T.ConnectionStr};Application Name=Trace" , T.XEventSessionName);

            var tokenSource2 = new CancellationTokenSource();
            CT = tokenSource2.Token;
            //                var Trace = samplexml.ReadEventStream(foo, CT);
            List<IXEvent> bob = new List<IXEvent>();

            //var Trace = new Thread(async () =>
            //{
            //    try
            //    {
            //        await samplexml.ReadEventStream(
            //       evt =>
            //       {
            //           TestContext.WriteLine($"Logging Event {evt.Name}");
            //           bob.Add(evt);
            //           return Task.CompletedTask;
            //       }, CancellationToken.None);
            //    }
            //    catch (Exception ex)
            //    {
            //       // throw;
            //    }
            //});
            //Trace.Name = "simon";
            //Trace.Priority = ThreadPriority.AboveNormal;           //                var Trace = samplexml.ReadEventStream(x => { return new Task(() => { bob.Add(x); }); }, CT);
            //Trace.Start();
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
            //T.StopTrace();

            Stopwatch sw = new Stopwatch();
            sw.Start();
 //           while (sw.ElapsedMilliseconds < 10000)
   //         {
                await Task.Delay(1000);
     //       }
            sw.Stop();
            Assert.That(bob, Has.Count.EqualTo(4));
          //  tokenSource2.Cancel();
            // Assert.That(() => results.Where(_ => _.name == "sql_statement_completed" && _.actions["sql_text"] == "select 'Simon'").Any());




        }
    }
   
}
