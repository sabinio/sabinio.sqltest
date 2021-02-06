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
using System.Diagnostics;

namespace SabinIO.SqlTest
{
    public class TracedConnection : IDisposable
    {
        public string ConnectionStr { get; set;}
        SqlConnection TestingConnection;
        SqlConnection MonitorConnection;
        public string XEventSessionName;

        public string target;

        public void Init() {
            InitSQL(new string[] { });
        }

        public void Init(string[] events)
        {
            if (events.Length == 0)
            {
                events = new string[] { "sql_batch_starting", "sql_statement_completed", "sql_batch_completed" };
            }
            TestingConnection = new SqlConnection($"{ConnectionStr};Application Name=bob");
            TestingConnection.Open();
            var spid = TestingConnection.ServerProcessId;
            using (MonitorConnection = new SqlConnection(ConnectionStr))
            {
                XEventSessionName = $"TestTrace{Guid.NewGuid()}";
                var sw = new Stopwatch();
                sw.Start();
                var t = new XEStore(new SqlStoreConnection(MonitorConnection));
                Console.WriteLine($"XEStore {sw.ElapsedMilliseconds}");
                sw.Restart();
                Session XEventSession = t.CreateSession(XEventSessionName);
                //   var t3 = XEventSession.AddTarget(t.EventFileTargetInfo);
                //   t3.TargetFields["filename"].Value = XEventSessionName;

                Console.WriteLine($"XeventSession {sw.ElapsedMilliseconds}");
                sw.Restart();
                var t2 = XEventSession.AddTarget(t.RingBufferTargetInfo);
                XEventSession.EventRetentionMode = Session.EventRetentionModeEnum.NoEventLoss;
                XEventSession.MaxDispatchLatency = 1;

                Console.WriteLine($"AddTarget {sw.ElapsedMilliseconds}");

                var packageEvents = new List<(string package, List<string> events)>(){
                            ("sqlserver", new List<string> (events))
                        };

                var packageActions = new List<(string package, List<string> actions)>
                        {
                            ("sqlserver", new List<string> { "client_app_name", "client_pid", "database_id", "sql_text","context_info" }),
                            ("package0", new List<string> { "event_sequence" })
                        };
//attach_activity_id is a local id associated with the worker.This is incremented using ++ as each event is produced.
//attach_activity_id_xfer

               //capture lightweight profile
               //ADD EVENT sqlserver.query_post_execution_plan_profile(
               //                ACTION(sqlos.scheduler_id, sqlserver.database_id, sqlserver.is_system, sqlserver.plan_handle, sqlserver.query_hash_signed, sqlserver.query_plan_hash_signed, sqlserver.server_instance_name, sqlserver.session_id, sqlserver.session_nt_username, sqlserver.sql_text))
               sw.Restart();
                var sessionColumn = t.Packages.Where(p => p.Name == "sqlserver").SelectMany(p => p.PredSourceInfoSet).Where(s => s.Name == "session_id").FirstOrDefault();

                Console.WriteLine($"Packages {sw.ElapsedMilliseconds}");
                sw.Restart();
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

                Console.WriteLine($"Predicates{sw.ElapsedMilliseconds}");
                sw.Restart();
                XEventSession.Create();
                XEventSession.Start();

                Console.WriteLine($"Create and Start{sw.ElapsedMilliseconds}");
            }
        }
    public void InitSQL(string[] events)
        {
            TestingConnection = new SqlConnection($"{ConnectionStr};Application Name=bob");
            TestingConnection.Open();
            var spid = TestingConnection.ServerProcessId;
            using (MonitorConnection = new SqlConnection(ConnectionStr))
            {
                XEventSessionName = $"TestTrace{Guid.NewGuid()}";

 var sql=               @$"
CREATE EVENT SESSION [{XEventSessionName}] ON SERVER 
ADD EVENT sqlserver.sql_batch_completed(
    ACTION(package0.event_sequence,sqlserver.client_app_name,sqlserver.client_pid,sqlserver.context_info,sqlserver.database_id,sqlserver.sql_text)
    WHERE ([sqlserver].[session_id]=({spid}))),
ADD EVENT sqlserver.sql_batch_starting(
    ACTION(package0.event_sequence,sqlserver.client_app_name,sqlserver.client_pid,sqlserver.context_info,sqlserver.database_id,sqlserver.sql_text)
    WHERE ([sqlserver].[session_id]=({spid}))),
ADD EVENT sqlserver.sql_statement_completed(
    ACTION(package0.event_sequence,sqlserver.client_app_name,sqlserver.client_pid,sqlserver.context_info,sqlserver.database_id,sqlserver.sql_text)
    WHERE ([sqlserver].[session_id]=({spid})))
ADD TARGET package0.ring_buffer
WITH (EVENT_RETENTION_MODE=NO_EVENT_LOSS,MAX_DISPATCH_LATENCY=1 SECONDS);
ALTER EVENT SESSION [{XEventSessionName}] ON SERVER STATE=START;

";
                MonitorConnection.Execute(sql);
            }

        }
        public SqlConnection Connection

        {
            get
            {
                if (TestingConnection == null)
                {
                    Init(new string[] { });
                    
                }
                return TestingConnection;
            }
        }
       

        public  T Execute<T>(string cmd)
        {
             return Connection.QuerySingle<T>(cmd);

        }
        public IEnumerable<Statement> Statements()
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
and xet.target_name = 'ring_buffer'

";
                var s = cmd.ExecuteXmlReader();
                //.GetStream(0);
                XElement eventXml = XElement.Load(s);
                var x = from ev in eventXml.Descendants("event")
                        select new Statement(ev.Attribute("name").Value,
                                  ev.Elements("action").Union(ev.Elements("data")).ToDictionary(_=>_.Attribute("name").Value, _=>_.Element("value").Value));

                return x;
            }
        }

        public void GetSessions()
        {
            using (MonitorConnection = new SqlConnection(ConnectionStr))
            {

                MonitorConnection.Open();


                var t = new XEStore(new SqlStoreConnection(MonitorConnection));

                var sessions = t.Sessions.ToList() ;

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
