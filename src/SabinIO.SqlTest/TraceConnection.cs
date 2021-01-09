using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Xml;

namespace SabinIO.SqlTest
{
    public class TracedConnection : IDisposable
    {
        public string ConnectionStr { get; set;}
        SqlConnection TestingConnection;
        SqlConnection MonitorConnection;
        public string XEventSessionName;


        public void Init()
        {
            TestingConnection = new SqlConnection($"{ConnectionStr};Application Name=bob");

            var spid = TestingConnection.QuerySingle<int>("select @@spid");
            using (MonitorConnection = new SqlConnection(ConnectionStr))
            {
                XEventSessionName = $"TestTrace{Guid.NewGuid()}";

                var t = new XEStore(new SqlStoreConnection(MonitorConnection));

                Session XEventSession = t.CreateSession(XEventSessionName);
                var t2 = XEventSession.AddTarget(t.RingBufferTargetInfo);
                XEventSession.EventRetentionMode = Session.EventRetentionModeEnum.NoEventLoss;
                XEventSession.MaxDispatchLatency = 1;
                

                var packageEvents = new List<(string package, List<string> events)>(){
                            ("sqlserver", new List<string> { "sql_batch_starting", "sql_statement_completed" })
                        };

                var packageActions = new List<(string package, List<string> actions)>
                        {
                            ("sqlserver", new List<string> { "client_app_name", "client_pid", "database_id", "sql_text" }),
                            ("package0", new List<string> { "event_sequence" })
                        };


                var sessionColumn = t.Packages.Where(p => p.Name == "sqlserver").SelectMany(p => p.PredSourceInfoSet).Where(s => s.Name == "session_id").FirstOrDefault();

                var SessionPredicate = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, new PredOperand(sessionColumn), new PredValue(spid));

                packageEvents.ForEach(p => p.events.ForEach(
                    e =>
                    {
                        var evnt = XEventSession.AddEvent($"{p.package}.{e}");
                        packageActions.ForEach(pa => pa.actions.ForEach(paEvent =>
                        {
                            evnt.AddAction($"{pa.package}.{paEvent}");
                        }));
                        evnt.Predicate = SessionPredicate;
                    }
                    )
                );
                XEventSession.Create();
                XEventSession.Start();
            }
        }

        public SqlConnection Connection

        {
            get
            {
                if (TestingConnection == null)
                {
                    Init();
                    
                }
                return TestingConnection;
            }
        }
       

        public  T Execute<T>(string cmd)
        {
             return Connection.QuerySingle<T>(cmd);

        }
        public IEnumerable<(string name ,Dictionary<string,string> actions)> Statements()
        {
            // Stream s= new MemoryStream();
            using (MonitorConnection = new SqlConnection(ConnectionStr))
            {

                MonitorConnection.Open();
                var cmd = MonitorConnection.CreateCommand();
                cmd.CommandText = @$"
SELECT CAST(xet.target_data AS xml)
FROM sys.dm_xe_session_targets AS xet
JOIN sys.dm_xe_sessions AS xe    ON(xe.address = xet.event_session_address)
WHERE xe.name = '{XEventSessionName}'
";

                var s = cmd.ExecuteXmlReader();
                //.GetStream(0);

                XElement eventXml = XElement.Load(s);
                var x = from ev in eventXml.Descendants("event")
                        select (name: ev.Attribute("name").Value,
                                actions: ev.Elements("action").Select(act => (name: act.Attribute("name").Value, value: act.Element("value").Value)).ToDictionary(_ => _.name, _ => _.value)
                                  );

                return x;
            }
        }
        public void StopTrace()
        {

            using (MonitorConnection = new SqlConnection(ConnectionStr))
            {

                MonitorConnection.Open();


                var t = new XEStore(new SqlStoreConnection(MonitorConnection));

                Session XEventSession = t.Sessions.Where(s => s.Name == XEventSessionName).FirstOrDefault();
                if (XEventSession != null)
                {
                    XEventSession.Stop();
                    XEventSession.Drop();
                }
            }
        }

        public void Dispose()
        {
            if (TestingConnection != null)
            {
                if (TestingConnection.State== System.Data.ConnectionState.Open)
                {
               
                }
                TestingConnection.Dispose();
            }

            StopTrace();
        }
    }
}
