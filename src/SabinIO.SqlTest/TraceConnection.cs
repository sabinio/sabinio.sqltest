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
using SabinIO.Sql;
using System.Data;

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
            InitSQL();
        }

        public void Init(string[] events = null, string[] filters = null)
        {
            if (events==null || events.Length == 0)
            {
                events = new string[] { "sql_batch_starting", "sql_statement_completed", "sql_batch_completed"};
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
                var t3 = XEventSession.AddTarget(t.EventFileTargetInfo);
                t3.TargetFields["filename"].Value = XEventSessionName;

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

        public IEnumerable<(int eventid,long query_hash,long query_plan_hash)> GetHashes(List<(string sql, int eventid)> queries,TextWriter log=null)
        {
            //query_post_execution_showplan
            InitSQL(new string[] { "sql_statement_completed", "sp_statement_completed" },
                            new string[] { "query_hash_signed", "plan_handle", "query_plan_hash_signed" ,"context_info"},
                            new string[] { " [sqlserver].[query_hash_signed]<>(0)" }, 
                            log);

            Dictionary<int, Exception> errors = new Dictionary<int, Exception>();
            foreach (var (sql, eventId) in queries)
            {
                try
                {
                    //TestContext.WriteLine(query);
                    this.Connection.Execute("declare @c varbinary(8) = cast(@eventId as varbinary(8)); SET CONTEXT_INFO @c--ignore", new { eventid = eventId });
                    var stmt = Parse.GetSqlCommand(sql);
                    stmt.Connection = Connection;
                    stmt.CommandTimeout = 60;
                    //Load the data to ensure query completes
                    using IDataReader r = stmt.ExecuteReader();
                    new DataTable().Load(r);
                    r.Close();
                }
                catch (Exception ex)
                {
                    //We don't want to fail on executing a query
                    log?.WriteLine(ex.Message);
                    errors.Add(eventId, ex);
                }
            }
            //Allow all statements to be recorded
            System.Threading.Thread.Sleep(10000);

            /*   var t = queries.Select(q2 => q2.eventId).Distinct();*/

            var rawAfterStatements = Statements().Where(c => !c["sql_text"].EndsWith("--ignore") && !c["sql_text"].EndsWith("@@spid")).ToList();
            var afterStatements = rawAfterStatements.Select(c =>
                (
                eventid: BitConverter.ToInt32(ContextInfoByteArray(c["context_info"])),
                query_hash_signed: Convert.ToInt64(c["query_hash_signed"]),
                query_plan_hash_signed: Convert.ToInt64(c["query_plan_hash_signed"])
                )); ;
            StopTrace();

            return afterStatements;
        }



        public void LoadHashes(string TraceConnectionStr, IEnumerable<(int eventid, long query_hash, long query_plan_hash)> hashes)
        {
            var fields = new string[] { "eventid", "query_hash", "query_plan_hash" };

            var f = new DBReader<(int eventid, long query_hash, long query_plan_hash)>(hashes.ToList(),
                 ((int eventid, long query_hash, long query_plan_hash) data, int key) =>
                key switch
                {
                    0 => data.eventid,
                    1=> data.query_hash,
                    2=> data.query_plan_hash,
                    _ => null
                },
                 null);
            
            ;

            using SqlConnection StoreConnection = new SqlConnection(TraceConnectionStr) ;
            using SqlBulkCopy bc = new SqlBulkCopy(StoreConnection) ;
            StoreConnection.Open();
            StoreConnection.Execute("drop table if exists EventQueryHash ");
            StoreConnection.Execute("create table EventQueryHash (eventid int, query_hash bigint, query_plan_hash bigint)");

            bc.DestinationTableName = "EventQueryHash";
                bc.WriteToServer(f);
        }


        public void InitSQL(string[] events =null,string [] actions=null,IList<string> filters=null, TextWriter log=null)
        {
            if (events==null || events.Length == 0)
            {
                events = new string[] { "sql_batch_starting", "sql_statement_completed", "sql_batch_completed" };
            }
            if (actions == null)
                actions = new string[] { "client_app_name", "client_pid", "context_info", "database_id", "sql_text" };

            if (filters == null)
                filters =new List<string>();

            TestingConnection = new SqlConnection($"{ConnectionStr};Application Name=bob");
            TestingConnection.Open();
            var spid = TestingConnection.ServerProcessId;

            filters.Add($"[sqlserver].[session_id]=({spid})");

            using (MonitorConnection = new SqlConnection(ConnectionStr))
            {
                XEventSessionName = $"TestTrace{Guid.NewGuid()}";
                log?.WriteLine($"Trace name = {XEventSessionName}");

                using StringWriter sw = new StringWriter();
                sw.WriteLine($"CREATE EVENT SESSION [{XEventSessionName}] ON SERVER ");
                
                bool first = true;
                foreach (var evt in events)
                {
                    if (!first) { sw.WriteLine(","); }
                    else first = false;

                    sw.Write($@"
ADD EVENT sqlserver.{evt}(
    ACTION(package0.event_sequence");
                    foreach (var action in actions)
                    {
                        if (action.IndexOf('.') > 0) sw.Write($",{action}");
                        else sw.Write($",sqlserver.{action}");

                    }
                    sw.WriteLine($") WHERE ");
                    for (int i = 0; i < filters.Count; i++)
                    {
                        sw.WriteLine($"{(i > 0 ? "AND" : "")} {filters[i]} ");
                    }
                    sw.Write(")");
                }
                sw.WriteLine(@$"
ADD TARGET package0.event_file(SET filename=N'{XEventSessionName}')
--,ADD TARGET package0.ring_buffer
WITH (MAX_MEMORY=50096 KB,EVENT_RETENTION_MODE=NO_EVENT_LOSS,MAX_DISPATCH_LATENCY=1 SECONDS,MAX_EVENT_SIZE=0 KB,MEMORY_PARTITION_MODE=NONE,TRACK_CAUSALITY=OFF,STARTUP_STATE=OFF)
ALTER EVENT SESSION [{XEventSessionName}] ON SERVER STATE=START;");
                sw.Flush();
                MonitorConnection.Execute(sw.ToString());
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
                return GetEventsFromEventFile();
            }
        }

        private IEnumerable<Statement> GetEventsFromEventFile()
        {
            var cmd = MonitorConnection.CreateCommand();
            var events = MonitorConnection.Query<XElement>(@$"
declare @filename varchar(max)= (
SELECT CAST(xet.target_data AS xml).value('(/EventFileTarget/File)[1]/@name','nvarchar(max)')
FROM sys.dm_xe_session_targets AS xet
JOIN sys.dm_xe_sessions AS xe    ON(xe.address = xet.event_session_address)
WHERE xe.name = @SessionName
and xet.target_name = 'event_file')
if @filename is null
  begin
    declare @msg nvarchar(max)=  FORMATMESSAGE(N'Cannot proceed no xEvent File found for session %s', @sessionName);
    throw 510001,@msg,1
    end
else
select event_data from sys.fn_xe_file_target_read_file(@filename,null,null,null)
", new { SessionName = XEventSessionName });
            var x = from ev in events
                    select new Statement(ev.Attribute("name").Value,
                              ev.Elements("action").Union(ev.Elements("data")).ToDictionary(_ => _.Attribute("name").Value, _ => _.Element("value").Value));

            return x;
        }

        private IEnumerable<Statement> GetEventsFromRingBuffer()
        {
            var cmd = MonitorConnection.CreateCommand();
            cmd.CommandText = @$"
SELECT CAST(xet.target_data AS xml)
FROM sys.dm_xe_session_targets AS xet
JOIN sys.dm_xe_sessions AS xe    ON(xe.address = xet.event_session_address)
WHERE xe.name = '{XEventSessionName}'
and xet.target_name != 'ring_buffer'

";
            var s = cmd.ExecuteXmlReader();
            //.GetStream(0);
            XElement eventXml = XElement.Load(s);
            var x = from ev in eventXml.Descendants("event")
                    select new Statement(ev.Attribute("name").Value,
                              ev.Elements("action").Union(ev.Elements("data")).ToDictionary(_ => _.Attribute("name").Value, _ => _.Element("value").Value));

            return x;
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
        public void StopTrace(bool drop= true)
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


        public IEnumerable<(int eventid, int logical, int physical, int cpu, int duration)> RunQueries(List<(string sql, int eventId)> queries,TextWriter log)
        {
            InitSQL(new string[] { "rpc_completed" },log:log);
            Dictionary<int, Exception> errors = new Dictionary<int, Exception>();
            foreach (var (sql, eventId) in queries)
            {
                try
                {
                    //TestContext.WriteLine(query);
                    this.Connection.Execute("declare @c varbinary(8) = cast(@eventId as varbinary(8)); SET CONTEXT_INFO @c--ignore", new { eventid = eventId });
                    var stmt = Parse.GetSqlCommand(sql);
                    stmt.Connection = this.Connection;
                    stmt.CommandTimeout = 60;
                    //Load the data to ensure query completes
                    using IDataReader r = stmt.ExecuteReader();
                    new DataTable().Load(r);
                    r.Close();
                }
                catch (Exception ex)
                {
                    //We don't want to fail on executing a query
                    log?.WriteLine(ex.Message);
                    errors.Add(eventId, ex);
                }
            }
            //Allow all statements to be recorded
            System.Threading.Thread.Sleep(10000);
            var t = queries.Select(q2 => q2.eventId).Distinct();
            var rawAfterStatements = Statements().Where(c => !c["sql_text"].EndsWith("--ignore") && !c["sql_text"].EndsWith("@@spid")).ToList();
            var afterStatements = rawAfterStatements.Select(c =>
                (
                eventid: BitConverter.ToInt32(ContextInfoByteArray(c["context_info"])),
                logical: Convert.ToInt32(c["logical_reads"]),
                physical: Convert.ToInt32(c["physical_reads"]),
                cpu: Convert.ToInt32(c["cpu_time"]),
                duration: Convert.ToInt32(c["duration"])
                )); ;


            StopTrace();
            return afterStatements;
        }
        private static byte[] ContextInfoByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[((NumberChars - i) / 2) - 1] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;

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
