using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.XEvent.XELite;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading.Tasks;

namespace SabinIO.xEvent.Lib
{

    public class XEventStream : IDataReader
    {
        private int _count = 0;
        private readonly ConcurrentQueue<IXEvent> _Q;
        //        private List<IXEvent> _list;

        int readposition = -1;
        private string[] _fieldList;
        public string[] fields { get { return _fieldList; } set { _fieldList = value; } }

        public bool finishedLoading = false;
        IXEvent CurrentItem;
        private DateTime lastRead;
        private ILogger _logger;

        public XEventStream(ILogger logger)
        {
            _logger = logger;
            //_list = new List<IXEvent>();
            _Q = new ConcurrentQueue<IXEvent>();
            lastRead = DateTime.Now;

        }
        public async Task AddAsync(IXEvent item)
        {
            _count++;


            _Q.Enqueue(item);
            if (Count % 10000 == 0)
            {

                System.Diagnostics.Trace.WriteLine($"AddASync {_count}");
            }
            if (_Q.Count > 10000)
            {
                if ((DateTime.Now - lastRead).TotalSeconds >10) throw new Exception("No data has been read for 60 seconds aborting");
                await Task.Delay(5);

            }

        }

        public bool Read()
        {
            return ReadAsync().GetAwaiter().GetResult();
        }
        public async Task<bool> ReadAsync()
        {
            lastRead = DateTime.Now;
            if (readposition % 10000 == 0) System.Diagnostics.Trace.WriteLine($"ReadAsync {readposition}");
            while (_Q.Count == 0)// readposition >= this._list.Count-1)
            {
                if (finishedLoading) return false;

                System.Diagnostics.Trace.WriteLine("Waiting for More Events");
                await Task.Delay(1000);
            }
            while (!_Q.TryDequeue(out CurrentItem)) ;

            readposition++;
            //    CurrentItem =             _list[readposition];
            return true;
        }

        public IXEvent Current
        {
            get
            {
                return CurrentItem;
            }
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
                    string field = _fieldList[i - 4];
                    if (CurrentItem.Fields.ContainsKey(field))
                    {
                        return CurrentItem.Fields[field];
                    }
                    else if (CurrentItem.Actions.ContainsKey(field))
                    {
                        return CurrentItem.Actions[field];
                    }
                    else return null;
            }
        }




        public int Count { get { return _count; } }

        public int FieldCount { get { return 4 + _fieldList.Length; } }


        public bool IsDBNull(int i)
        {
            return false;
            //            throw new NotImplementedException();
        }

        public int Depth { get { return 0; } }

        #region NotImplemented
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


        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public bool NextResult()
        {
            throw new NotImplementedException();
        }

        public object this[int i] => throw new NotImplementedException();

        public object this[string name] => throw new NotImplementedException();


        public bool IsClosed => throw new NotImplementedException();

        public int RecordsAffected => throw new NotImplementedException();

        #endregion
    }

}