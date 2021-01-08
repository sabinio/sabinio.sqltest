using Microsoft.Data.SqlClient;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace XEvent.App.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestAppExecutes()
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var samplexmlfile = Path.Combine(assemblyPath, "sql_large.xel");
            using var ErrorStream = new MemoryStream();
            using var sw = new StreamWriter(ErrorStream);
            Console.SetError(sw);

            var result = XEvent.App.Program.Main(new string[] {
                    "--batchsize","10000",
                    "--tablename","Trace",
                    "--connection", "data source=.;Trusted_Connection=True;initial catalog=test",
                    "--filename", samplexmlfile,
                    "--fields",string.Join(",",new string[] { "page_faults", "cpu_time", "sql_text", "duration" }) });

            Console.SetError(new StreamWriter(Console.OpenStandardError()));

            ErrorStream.Position = 0;
            using var r = new StreamReader(ErrorStream);
            var xr = r.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(xr))
                throw new Exception(xr);


        }
        [Test]
        public void TestExecutionWhenTableDoesntExists()
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Assert.Throws(typeof(Exception),() =>
            {
                var samplexmlfile = Path.Combine(assemblyPath, "sql_large.xel");
                using var ErrorStream = new MemoryStream();
                using var sw = new StreamWriter(ErrorStream);
                
                var result = XEvent.App.Program.Main(new string[] {
                    "--batchsize","10000",
                    "--tablename","Foo",
                    "--connection", "data source=.;Trusted_Connection=True;initial catalog=test",
                    "--filename", samplexmlfile,
                    "--fields",string.Join(",",new string[] { "page_faults", "cpu_time", "sql_text", "duration" }) });

                Console.SetError(new StreamWriter(Console.OpenStandardError()));

                sw.Flush();
                if (ErrorStream.Position > 0)
                {
                    ErrorStream.Position = 0;
                    using var r = new StreamReader(ErrorStream);
                    var xr = r.ReadToEnd();
                    throw new Exception(xr);
                }
            });


        }

    }
}