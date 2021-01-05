using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Dapper;
using System.Linq;
namespace SqlTest.Test
{
    [TestFixture]
        public class Class1
    {

        [TestCase]
        public void Runtest()
        {

            using (var T = new SqlTest.Lib.Trace() { connectionStr = "data source=.;user=sa;password=ABCabc123456!" })
            {
                var result = T.Execute<string>("select 'Simn'");

                //            Task.WaitAll( result);
                Assert.That(() => result == "Simn");
                T.Connection.Query("select @@version");

                Assert.That(() => T.Statements().Where(_=>_.name=="sql_statement_completed" && _.actions["sql_text"]== "select 'Simn'").Any());
            }
        }
    }
}
