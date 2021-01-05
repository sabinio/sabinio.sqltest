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

namespace SqlTest.Lib
{
    public class Trace : IDisposable
    {
        public string connectionStr { get; set; }
        SqlConnection S;
        SqlConnection Monitor;
        string XEventSessionName;
        public SqlConnection Connection

        {
            get
            {
                if (S == null)
                {
                    S = new SqlConnection($"{connectionStr};Application Name=bob");
                    

                    var spid = S.QuerySingle<int>("select @@spid");
                    using (Monitor = new SqlConnection(connectionStr))
                    {
                        XEventSessionName = $"TestTrace{Guid.NewGuid()}";

                        var t = new XEStore(new SqlStoreConnection(Monitor));

                        Session XEventSession = t.CreateSession(XEventSessionName);
                        var t2 = XEventSession.AddTarget(t.RingBufferTargetInfo);
                        XEventSession.EventRetentionMode = Session.EventRetentionModeEnum.NoEventLoss;
                        XEventSession.MaxDispatchLatency = 0;
                            
                        var packageEvents = new List<(string package, List<string> events)>();
                        packageEvents.Add(("sqlserver", new List<string> { "sql_batch_starting", "sql_statement_completed" }));

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
                    Monitor = new SqlConnection(connectionStr);

                    Monitor.Open();
                }
                return S;
            }
        }


        public Trace ()
        {

        }
        public  T Execute<T>(string cmd)
        {
             return Connection.QuerySingle<T>(cmd);

        }
        public IEnumerable<(string name ,Dictionary<string,string> actions)> Statements()
        {
            // Stream s= new MemoryStream();

            var cmd = Monitor.CreateCommand();
            cmd.CommandText= @$"
SELECT CAST(xet.target_data AS xml)
FROM sys.dm_xe_session_targets AS xet
JOIN sys.dm_xe_sessions AS xe    ON(xe.address = xet.event_session_address)
WHERE xe.name = '{XEventSessionName}'
";

            var s = cmd.ExecuteXmlReader();
            //.GetStream(0);

            XElement eventXml = XElement.Load(s);
            var x = from ev in eventXml.Descendants("event")
                     select (name:ev.Attribute("name").Value,
                             actions:ev.Elements("action").Select(act=>(name: act.Attribute("name").Value,value: act.Element("value").Value)).ToDictionary(_=>_.name,_=>_.value)
                               );

            return x;
        }


        public void Dispose()
        {
            if (S != null)
            {
                if (S.State== System.Data.ConnectionState.Open)
                {
               
                }
                S.Dispose();
            }
            if (Monitor != null)
            {
                if (Monitor.State == System.Data.ConnectionState.Open)
                {
                    var t = new XEStore(new SqlStoreConnection(Monitor));

                    Session XEventSession = t.Sessions.Where(s=>s.Name== XEventSessionName).FirstOrDefault();
                    if (XEventSession != null)
                    {
                        XEventSession.Stop();
                        XEventSession.Drop();
                    }
                }
                Monitor.Dispose();
            }
        }
    }
}
