using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Serilog;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Dapper;
using SabinIO.xEvent.Lib;
using System.Diagnostics;

namespace SabinIO.xEvent.Lib.Tests
{

    class TraceEventTests
    {
        IServiceCollection services;
        IServiceProvider provider;

        [OneTimeSetUp]
        public void Setup()
        {
            Log.Logger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .CreateLogger();
            
            services = new ServiceCollection()
                    .AddLogging(loggingBuilder =>
                    loggingBuilder.AddSerilog(dispose: true))
                    .AddTransient<XEFileReader>(s => new XEFileReader(s.GetService<Microsoft.Extensions.Logging.ILogger<XEFileReader>>()));

            provider = services.BuildServiceProvider();

        }
           

        [Test]
        public void EnsureXMlisParsed()
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            using var samplexml = XmlReader.Create(Path.Combine(assemblyPath, "EventData.xml"));
            //TODO:Pass log
            var X = TraceEvent.LoadFromStream(samplexml,null).ToList();

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
            var (rowsread, rowsinserted) = await
                   SetupFileReader("TestXELFileCanBeRead", tableColumns: "event_name varchar(200), uuid uniqueidentifier")
                     .ReadAndLoad(new string[] { "event_name","uuid" }, new string[] { }, new System.Threading.CancellationToken())
                    ;
                 
            Assert.That(rowsread, Is.EqualTo(rowsinserted));
            Assert.That(rowsread, Is.Not.EqualTo(0));
            TestContext.Write($"rows read        {rowsread}");
            TestContext.Write($"rows bulk loaded {rowsinserted}");
        }

        [Test]
        public async Task TestErrorNotThrownIfColumnNumbersMatchTargetTable()
        {

            Assert.Throws<InvalidOperationException>(() =>
                     SetupFileReader("TestErrorNotThrownIfColumnNumbersMatchTargetTable", tableColumns: "uuid uniqueidentifier null")
                      .ReadAndLoad(new string[] { "uuid" ,"cpu_time"}, new string[] { }, new System.Threading.CancellationToken())
                      .GetAwaiter().GetResult()
                  );
        }

        [Test]
        public async Task MappingColumnsWorks()
        {
            var (rowsread, rowsinserted) = await SetupFileReader("MappingColumnsWorks", tableColumns: "id uniqueidentifier null,event varchar(100)")
                      .ReadAndLoad(new string[] { "event_name", "uuid" }, new string[] { "event", "id" }, new System.Threading.CancellationToken());
            Assert.That(rowsread, Is.EqualTo(rowsinserted));
            Assert.That(rowsread, Is.Not.EqualTo(0));
            TestContext.Write($"rows read        {rowsread}");
            TestContext.Write($"rows bulk loaded {rowsinserted}");
        }

        [Test]
        public void TestErrorThrownIfFieldNotInXELFile()
        {
            Assert.Throws<InvalidFieldException>(() =>
               SetupFileReader("TestErrorNotThrownIfColumnNumbersDontMatchTargetTable",tableColumns: "uuid uniqueidentifier null, event_name varchar(100)")
                .ReadAndLoad(new string[] { "id" }, new string[] { }, new System.Threading.CancellationToken())
                .GetAwaiter().GetResult()
            );
        }


        private XEFileReader SetupFileReader(string tablename, string filename= "sql_large.xel", string tableColumns ="")
        {
            string connection = "data source=.;Trusted_Connection=True;initial catalog=test";

            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var samplexmlfile = Path.Combine(assemblyPath, filename);

            XEFileReader eventStream = provider.GetRequiredService<XEFileReader>();
            eventStream.filename = samplexmlfile;
            eventStream.connection = connection;
            eventStream.tableName = tablename;

            var Connection = new SqlConnection(connection);
            Connection.Query($"drop table if exists {tablename}");
            Connection.Query($"create table {tablename} ({tableColumns})");
            return eventStream;
        }

        [Test]
        public void TestErrorThrownIfInvalidFieldIsUsed()
        {

            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var samplexmlfile = Path.Combine(assemblyPath, "sql_large.xel");
            string connection = "data source=.;Trusted_Connection=True;initial catalog=test";

            XEFileReader eventStream = provider.GetRequiredService<XEFileReader>();
            eventStream.filename = samplexmlfile;
            eventStream.connection = connection;
            eventStream.tableName = "TestErrorNotThrownIfColumnNumbersDontMatchTargetTable";

            var Connection = new SqlConnection(connection);
            Connection.Query("drop table if exists TestErrorNotThrownIfColumnNumbersDontMatchTargetTable");
            Connection.Query("create table TestErrorNotThrownIfColumnNumbersDontMatchTargetTable (uuid uniqueidentifier null, event_name varchar(100))");

            Assert.Throws<InvalidFieldException>(() =>
            {
                eventStream.ReadAndLoad(new string[] { "id" }, new string[] { }, new System.Threading.CancellationToken()).GetAwaiter().GetResult();
            }
            );
        }


        [Test]
        public async Task IncompleteFileDoesntError()
        {
            
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var samplexmlfile = Path.Combine(assemblyPath, "truncated.xel");


            XEFileReader eventStream = provider.GetRequiredService<XEFileReader>();
            eventStream.filename = samplexmlfile;
            eventStream.connection = "data source=.;Trusted_Connection=True;initial catalog=test";
            eventStream.tableName = "trace";
            
            var rowsread = await eventStream.ReadAsync(new System.Threading.CancellationToken());
            var events = eventStream.ReadEvents().ToList();


            Assert.That(rowsread, Is.EqualTo(events.Count));
            Assert.That(rowsread, Is.EqualTo(40));
            TestContext.Write($"rows read        {rowsread}");
        }

    }
}
