using NUnit.Framework;
using SqlTest.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Linq;
using NUnit.Framework.Constraints;
using System.Linq.Expressions;
using Microsoft.SqlServer.XEvent.XELite;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Data.SqlClient;
using System.Data.Common;
using System.Data;
using Microsoft.ApplicationInsights;
using System.Collections;
using System.Collections.Concurrent;
using sabin.io.xevent;

namespace SqlTest.Test
{

    class TraceEventTests
    {
        [Test]
        public void EnsureXMlisParsed()
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            using var samplexml = XmlReader.Create(Path.Combine(assemblyPath, "EventData.xml"));
            var X = TraceEvent.LoadFromStream(samplexml, TestContext.Out).ToList();

            Assert.That(X.Sum(x => x.cpu_time), Is.EqualTo(1990));
            Assert.That(X.Select(y => y.database_name).FirstOrDefault(), Is.EqualTo("master"));

            Assert.That(X.Select(y => y.eventName), Contains.Item("sql_batch_completed"));
            Assert.That(X, Has.Some.Property("eventName").EqualTo("sql_batch_completed"));

            var foo = X.Aggregate((running, next) =>
            {
                running.cpu_time += next.cpu_time;
                running.logical_reads += next.logical_reads;
                running.duration += next.duration;
                return running;
            });
            Assert.That((foo.cpu_time, foo.logical_reads, foo.duration), Is.EqualTo((1990, 0, 128)));

        }
        [Test]
        public async Task TestXELFileCanBeRead()
        {

            System.Diagnostics.Trace.WriteLine("bugger");

            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var samplexmlfile = Path.Combine(assemblyPath, "sqlother.xel");

            XEFileReader eventStream = new XEFileReader(new string[] { "page_faults", "cpu_time", "sql_text", "duration" })
            {
                filename = samplexmlfile,
                connection = "data source=.;Trusted_Connection=True;initial catalog=test",
                tableName = "trace"
            };
            var (rowsread, rowsinserted) = await eventStream.ReadAndLoad();

            Assert.That(rowsread, Is.EqualTo(rowsinserted));
            Assert.That(rowsread, Is.Not.EqualTo(0));
            TestContext.Write($"rows read        {rowsread}");
            TestContext.Write($"rows bulk loaded {rowsinserted}");
        }


    }
}
