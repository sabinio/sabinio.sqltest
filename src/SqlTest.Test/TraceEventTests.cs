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

namespace SqlTest.Test
{

    class TraceEventTests
    {
        [Test]
        public void EnsureXMlisParsed()
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            using (var samplexml = XmlReader.Create(Path.Combine(assemblyPath, "EventData.xml")))
            {

                var X = TraceEvent.LoadFromStream(samplexml, TestContext.Out).ToList();

                Assert.That(X.Sum(x => x.cpu_time), Is.EqualTo(1990));
                Assert.That(X.Select(y => y.database_name).FirstOrDefault(), Is.EqualTo("master"));

                Assert.That(X.Select(y => y.eventName), Contains.Item("sql_batch_completed"));
                Assert.That(X, Has.Some.Property("eventName").EqualTo("sql_batch_completed"));

                var foo = X.Aggregate((running, next) => {
                    running.cpu_time += next.cpu_time;
                    running.logical_reads += next.logical_reads;
                    running.duration += next.duration;
                    return running;
                });
                Assert.That((foo.cpu_time,foo.logical_reads,foo.duration), Is.EqualTo((1990, 0,128)));
            }

        }
        [Test]
        public async Task TestXELFileCanBeRead()
        {

            System.Diagnostics.Trace.WriteLine("bugger");

        string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var samplexml = new XEFileEventStreamer(Path.Combine(assemblyPath, "sqlother.xel"));
             


            var CT = new CancellationToken();
            MyDbReader events = new MyDbReader(new string[] { "page_faults","cpu_time","sql_text","duration"});

            var Con = new SqlConnection("data source=.;Trusted_Connection=True;initial catalog=test");
            Con.Open();

            SqlBulkCopy bc = new SqlBulkCopy(Con);
             

            bc.DestinationTableName = "trace";
            bc.EnableStreaming = true;
            bc.BatchSize = 200000;
            
            var foo = Task.Run(async ()=>
            await  samplexml.ReadEventStream(() => Task.CompletedTask
            , (x) =>events.AddAsync(x)
            , CT)) ;

            var bulk= Task.Run(async()=> await bc.WriteToServerAsync(events));
            
            foo.Wait();

            events.finishedLoading = true;
            bulk.Wait();
//            Assert.That(events, Has.Count.EqualTo(2475));
           
            Assert.That(bc.RowsCopied, Is.EqualTo(events.Count));
            Assert.That(bc.RowsCopied, Is.Not.EqualTo(0));


        }

    }
    class MyDbReader : IDataReader
    {
        private int _count=0;
        private ConcurrentQueue<IXEvent> _Q;
//        private List<IXEvent> _list;
        private string[] _fieldList;
        public bool finishedLoading = false;
        IXEvent CurrentItem;
        public MyDbReader(string[] fieldList)
        {
            _fieldList = fieldList;

            //_list = new List<IXEvent>();
            _Q = new ConcurrentQueue<IXEvent>();



        }
        public  async Task AddAsync(IXEvent item)
        {
            _count++;

            _Q.Enqueue(item);
            if (Count / 100 == 0)
            {

                System.Diagnostics.Trace.WriteLine("AddASync");
            }
            if (_Q.Count > 10000)
            {
                await Task.Delay(5);

            }

        }

        int readposition = -1;
        int writeposition = -1;
        
        public int Count { get { return _count; } }
        
        public object this[int i] => throw new NotImplementedException();

        public object this[string name] => throw new NotImplementedException();

        public int Depth { get { return 0; } }
        public bool IsClosed => throw new NotImplementedException();

        public int RecordsAffected => throw new NotImplementedException();

        public int FieldCount { get { return 4 + _fieldList.Length; } }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool GetBoolean(int i)
        {
            throw new NotImplementedException();
        }

        public byte GetByte(int i)
        {
            throw new NotImplementedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            throw new NotImplementedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        public string GetDataTypeName(int i)
        {
            throw new NotImplementedException();
        }

        public DateTime GetDateTime(int i)
        {
            throw new NotImplementedException();
        }

        public decimal GetDecimal(int i)
        {
            throw new NotImplementedException();
        }

        public double GetDouble(int i)
        {
            throw new NotImplementedException();
        }

        public Type GetFieldType(int i)
        {
            throw new NotImplementedException();
        }

        public float GetFloat(int i)
        {
            throw new NotImplementedException();
        }

        public Guid GetGuid(int i)
        {
            throw new NotImplementedException();
        }

        public short GetInt16(int i)
        {
            throw new NotImplementedException();
        }

        public int GetInt32(int i)
        {
            throw new NotImplementedException();
        }

        public long GetInt64(int i)
        {
            throw new NotImplementedException();
        }

        public string GetName(int i)
        {
            throw new NotImplementedException();
        }

        public int GetOrdinal(string name)
        {
            throw new NotImplementedException();
        }

        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public string GetString(int i)
        {
            throw new NotImplementedException();
        }

        public object GetValue(int i)
        {
            switch (i)
            {
                case 1:
                    return CurrentItem.UUID;
                case 2:
                    return CurrentItem.Name;
                case 3:
                    return CurrentItem.Timestamp;
                default:
                    string field = _fieldList[i-4];
                    if (CurrentItem.Fields.ContainsKey(field)) {
                        return CurrentItem.Fields[field];
                    }
                    else if (CurrentItem.Actions.ContainsKey(field)) { 
                        return CurrentItem.Actions[field];
                    }
                    else return null;
            }
        }

        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public bool IsDBNull(int i)
        {
            return false;
//            throw new NotImplementedException();
        }

        public bool NextResult()
        {
            throw new NotImplementedException();
        }


        public bool Read()
        {
            return ReadAsync().Result;
        }
        public async Task<bool> ReadAsync()
        {
            if (readposition/1000 ==0) System.Diagnostics.Trace.WriteLine($"ReadAsync {readposition}");
            while (_Q.Count == 0)// readposition >= this._list.Count-1)
            {
                if (finishedLoading) return false;

                System.Diagnostics.Trace.WriteLine("Waiting for More Events");
                await Task.Delay(1000) ;
            }
            while (!_Q.TryDequeue(out CurrentItem)) ;

                readposition++;
//    CurrentItem =             _list[readposition];
                return true;
        }
    }
}
