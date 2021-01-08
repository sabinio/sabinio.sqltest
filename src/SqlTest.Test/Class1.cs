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

namespace SqlTest.Test
{
    [TestFixture]
        public class Class1
    {
 
        [TestCase]
        public void Runtest()
        {

            using (var TestConnection = new Lib.Trace() { ConnectionStr = "data source=.;Trusted_Connection=True" })
            {
                var result = TestConnection.Execute<string>("select 'Simon'");

                //            Task.WaitAll( result);
                Assert.That(() => result == "Simon");

                TestConnection.Connection.Query("select @@version");


                var results = TestConnection.Statements().ToList();                                    

                Assert.That(() => results.Where(_ => _.name == "sql_statement_completed" && _.actions["sql_text"] == "select 'Simon'").Any());

            }
        
        }
        List<IXEvent> bob = new List<IXEvent>();
        CancellationToken CT;

        [TestCase]
        public void Runtest2()
        {
            using (var T = new Lib.Trace() { ConnectionStr = "data source=.;Trusted_Connection=True" })
            {
                T.init();
                var samplexml = new XELiveEventStreamer(T.ConnectionStr, T.XEventSessionName);

                var tokenSource2 = new CancellationTokenSource();
                CT = tokenSource2.Token;
                //                var Trace = samplexml.ReadEventStream(foo, CT);
                var Trace = samplexml.ReadEventStream(
                    evt=>
                    {
                        bob.Add(evt);
                        return Task.CompletedTask;
                    }, CT);
                //                var Trace = samplexml.ReadEventStream(x => { return new Task(() => { bob.Add(x); }); }, CT);

                var result = T.Execute<string>("select 'Simon'");
                //            Task.WaitAll( result);
                Assert.That(() => result == "Simon");
                T.Connection.Query("select @@version");
                //T.StopTrace();

                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (sw.ElapsedMilliseconds < 10000 )
                {
                    Trace.Wait(1000);
                }
                sw.Stop();
                Assert.That(bob, Has.Count.EqualTo(4));
                // Assert.That(() => results.Where(_ => _.name == "sql_statement_completed" && _.actions["sql_text"] == "select 'Simon'").Any());


            }


        }
    }
   
}
