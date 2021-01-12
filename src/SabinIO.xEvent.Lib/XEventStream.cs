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
        private readonly ILogger _logger;
        public int progress = 100000;
        public int maxQueueSize = 10000;

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
            if (Count % progress == 0)
            {

                _logger?.LogInformation($"AddASync {_count}");
            }
            if (_Q.Count > maxQueueSize)
            {
                if ((DateTime.Now - lastRead).TotalSeconds > 10) throw new Exception("No data has been read for 60 seconds aborting");
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
            if (readposition % progress == 0) _logger?.LogInformation($"ReadAsync {readposition}");
            while (_Q.Count == 0)// readposition >= this._list.Count-1)
            {
                if (finishedLoading) return false;

                _logger.LogInformation("Waiting for More Events");
                await Task.Delay(1000);
            }
            while (!_Q.TryDequeue(out CurrentItem)) ;

            readposition++;
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
        }

        public int Depth { get { return 0; } }

        #region NotImplemented
        public void Close()
        {
            _logger.LogInformation("XEventStrem called close - not implemented");
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _logger.LogInformation("XEventStrem called dispose - not implemented");
            throw new NotImplementedException();
        }

        public bool GetBoolean(int i)
        {
            _logger.LogInformation("XEventStrem called getBoolean - not implemented");

            throw new NotImplementedException();
        }

        public byte GetByte(int i)
        {
            _logger.LogInformation("XEventStrem called GetByte - not implemented");

            throw new NotImplementedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            _logger.LogInformation("XEventStrem called getBytes - not implemented");

            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            _logger.LogInformation("XEventStrem called getChar - not implemented");

            throw new NotImplementedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            _logger.LogInformation("XEventStrem called getChars - not implemented");

            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            _logger.LogInformation("XEventStrem called getData - not implemented");

            throw new NotImplementedException();
        }

        public string GetDataTypeName(int i)
        {
            _logger.LogInformation("XEventStrem called getDateTypeName - not implemented");

            throw new NotImplementedException();
        }

        public DateTime GetDateTime(int i)
        {
            _logger.LogInformation("XEventStrem called GetDateTime - not implemented");

            throw new NotImplementedException();
        }

        public decimal GetDecimal(int i)
        {
            _logger.LogInformation("XEventStrem called GetDecimal - not implemented");

            throw new NotImplementedException();
        }

        public double GetDouble(int i)
        {
            _logger.LogInformation("XEventStrem called getDouble - not implemented");

            throw new NotImplementedException();
        }

        public Type GetFieldType(int i)
        {
            _logger.LogInformation("XEventStrem called GetFieldType - not implemented");

            throw new NotImplementedException();
        }

        public float GetFloat(int i)
        {
            _logger.LogInformation("XEventStrem called getFloat - not implemented");

            throw new NotImplementedException();
        }

        public Guid GetGuid(int i)
        {
            _logger.LogInformation("XEventStrem called GetGuid - not implemented");

            throw new NotImplementedException();
        }

        public short GetInt16(int i)
        {
            _logger.LogInformation("XEventStrem called GetInt16 - not implemented");

            throw new NotImplementedException();
        }

        public int GetInt32(int i)
        {
            _logger.LogInformation("XEventStrem called GetInt32 - not implemented");

            throw new NotImplementedException();
        }

        public long GetInt64(int i)
        {
            _logger.LogInformation("XEventStrem called GetInt64 - not implemented");

            throw new NotImplementedException();
        }

        public string GetName(int i)
        {
            _logger.LogInformation("XEventStrem called GetName - not implemented");

            throw new NotImplementedException();
        }

        public int GetOrdinal(string name)
        {
            _logger.LogInformation("XEventStrem called GetOrdinal - not implemented");

            throw new NotImplementedException();
        }

        public DataTable GetSchemaTable()
        {
            _logger.LogInformation("XEventStrem called GetSchemaTable - not implemented");

            throw new NotImplementedException();
        }

        public string GetString(int i)
        {
            _logger.LogInformation("XEventStrem called GetString - not implemented");

            throw new NotImplementedException();
        }


        public int GetValues(object[] values)
        {
            _logger.LogInformation("XEventStrem called GetValues - not implemented");

            throw new NotImplementedException();
        }

        public bool NextResult()
        {
            _logger.LogInformation("XEventStrem called NextResult - not implemented");

            throw new NotImplementedException();
        }

        public object this[int i] =>            throw new NotImplementedException();
    
        public object this[string name] => throw new NotImplementedException();


        public bool IsClosed => throw new NotImplementedException();

        public int RecordsAffected => throw new NotImplementedException();

        #endregion
    }

}