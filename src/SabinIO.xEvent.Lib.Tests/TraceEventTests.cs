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

            System.Diagnostics.Trace.WriteLine("bugger");

            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var samplexmlfile = Path.Combine(assemblyPath, "sqlother.xel");


            XEFileReader eventStream = provider.GetRequiredService<XEFileReader>();
            //new XEFileReader( new string[] { "page_faults", "cpu_time", "sql_text", "duration" });
            eventStream.filename = samplexmlfile;
            eventStream.connection = "data source=.;Trusted_Connection=True;initial catalog=test";
            eventStream.tableName = "trace";
            
            var (rowsread, rowsinserted) = await eventStream.ReadAndLoad(new string[] { "page_faults", "cpu_time", "sql_text", "duration" });

            Assert.That(rowsread, Is.EqualTo(rowsinserted));
            Assert.That(rowsread, Is.Not.EqualTo(0));
            TestContext.Write($"rows read        {rowsread}");
            TestContext.Write($"rows bulk loaded {rowsinserted}");
        }

        [Test]
        public async Task IncompleteFileDoesntError()
        {
            System.Diagnostics.Trace.WriteLine("bugger");

            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var samplexmlfile = Path.Combine(assemblyPath, "truncated.xel");


            XEFileReader eventStream = provider.GetRequiredService<XEFileReader>();
            eventStream.filename = samplexmlfile;
            eventStream.connection = "data source=.;Trusted_Connection=True;initial catalog=test";
            eventStream.tableName = "trace";
            
            var rowsread = await eventStream.ReadAsync();
            var events = eventStream.ReadEvents().ToList();


            Assert.That(rowsread, Is.Not.EqualTo(0));
            Assert.That(rowsread, Is.EqualTo(40));
            TestContext.Write($"rows read        {rowsread}");
        }

    }
}
