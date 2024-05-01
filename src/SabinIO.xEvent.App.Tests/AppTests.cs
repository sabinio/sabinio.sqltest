using Microsoft.Data.SqlClient;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using SabinIO.xEvent.App;
using Dapper;

namespace XEvent.App.Tests
{
    public class AppTests
    {

        public string connectionString { get { return TestContext.Parameters["TraceConnectionString"]; } }


        [SetUp]
        public void Setup()
        {
        }

        [Test]
        [Category("Integration")]
        public void TestAppExecutes()
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var samplexmlfile = Path.Combine(assemblyPath, "sql_large.xel");
            using var ErrorStream = new MemoryStream();
            using var sw = new StreamWriter(ErrorStream);
            Console.SetError(sw);

            new SqlConnection(connectionString).Query("drop table if exists TestAppExecutes");
            new SqlConnection(connectionString).Query("create table TestAppExecutes (TraceId int, sql nvarchar(max))");

            var result = Program.Main(new string[] {
                    "--batchsize","10000",
                    "--tablename","TestAppExecutes",
                    "--connection", connectionString,
                    "--filename", samplexmlfile,
                    "--fields",  "{100}","sql_text",
                    "--includedEvents","rpc_completed",
                    "--columns","TraceId","sql"}).GetAwaiter().GetResult();
            sw.Flush();

            Console.SetError(new StreamWriter(Console.OpenStandardError()));

            ErrorStream.Position = 0;
            using var r = new StreamReader(ErrorStream);
            var xr = r.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(xr))
                throw new Exception(xr);

            new SqlConnection(connectionString).Query("drop table if exists TestAppExecutes");

        }
        [Test]
        [Category("Integration")]

        public async Task AppThrowsExceptionWhenTableDoesntExists()
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var samplexmlfile = Path.Combine(assemblyPath, "sql_large.xel");
            using var ErrorStream = new MemoryStream();
            using var sw = new StreamWriter(ErrorStream);
            Console.SetError(sw);

            var RunExe = Program.Main(new string[] {
                    "--batchsize","10000",
                    "--tablename","Foo",
                    "--connection", connectionString,
                    "--filename", samplexmlfile,
                    "--fields","page_faults", "cpu_time", "sql_text", "duration"  });

            await RunExe;
    
            sw.Flush();
            Console.SetError(new StreamWriter(Console.OpenStandardError()));
            string ErrorMessage = "";
            if (ErrorStream.Position > 0)
            {
                ErrorStream.Position = 0;
                using var r = new StreamReader(ErrorStream);
                ErrorMessage = r.ReadToEnd();
            }
            
            Assert.That(ErrorMessage, Does.Contain("Cannot access destination table 'Foo'."));


        }

    }
}