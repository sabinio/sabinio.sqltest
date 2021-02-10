using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.XEvent.XELite;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SabinIO.xEvent.Lib
{

    public class XEventStream : IDataReader
    {
        private int _count = 0;
        private readonly BlockingCollection<IXEvent> _Q;

        public CancellationToken CancelToken;
        int readposition = -1;
        private string[] _fieldList;
        public string[] fields { get { return _fieldList; } 
            set {
                _fieldList = new string[value.Length];
                _compressedfield = new bool[value.Length];
                for (int i = 0; i < value.Length; i++)
                {
                    var field = value[i];
                   
                    if (field.EndsWith(".compressed"))
                    {
                        _fieldList[i] = field[..field.LastIndexOf('.')];
                        _compressedfield[i] = true;
                    }
                    else
                    {
                        _fieldList[i] = field;
                        _compressedfield[i] = false;
                    }
                }
} 
        }
        private bool[] _compressedfield;

        public bool finishedLoading { set { _Q.CompleteAdding(); } }
        IXEvent CurrentItem;
        private readonly ILogger _logger;
        public int progress = 100000;
        public int maxQueueSize = 10000;

        public XEventStream(ILogger logger)
        {
            _logger = logger;

            _Q = new BlockingCollection<IXEvent>(maxQueueSize);
        }
        public void Add(IXEvent item)
        {
            _count++;

            _Q.Add(item);
            if (Count % progress == 0)
            {

                _logger?.LogInformation($"AddASync {_count}");
            }
        }

        public bool Read()
        {

            if (readposition % progress == 0) _logger?.LogInformation($"ReadAsync {readposition}");
            if (_Q.IsCompleted)
            {
                return false;
            }
            else {
                CurrentItem = _Q.Take(CancelToken);
                readposition++;
                return true;
            }
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
            object result = GetRawFieldValue(i);

            if (_compressedfield[i])
            {
                return CompressMyValue((string)result);
            }
            else return result;
        }

        private object GetRawFieldValue(int i)
        {
            return _fieldList[i] switch
            {
                "uuid" => CurrentItem.UUID,
                "event_name" => CurrentItem.Name,
                "timestamp" => CurrentItem.Timestamp,
                string field when CurrentItem.Fields.ContainsKey(field) => CurrentItem.Fields[field],
                string field when CurrentItem.Actions.ContainsKey(field) => CurrentItem.Actions[field],
                string field when (field[0] == '{') => field[1..^1],
                _ => throw new InvalidFieldException(_fieldList[i])

            };
        }



        private static byte[] CompressMyValue(string value)
        {
            using (MemoryStream streamOut = new MemoryStream())
            {
                using (GZipStream compressingStream = new GZipStream(streamOut, CompressionMode.Compress))
                using (MemoryStream zipStream = new MemoryStream(Encoding.Unicode.GetBytes(value)))
                {
                    zipStream.CopyTo(compressingStream);
                }
               return  streamOut.ToArray();
            }

        }

        public int Count { get { return _count; } }

        public int FieldCount { get { return  _fieldList.Length; } }


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
            
            for (int i = 0; i < _fieldList.Length; i++)
            {
                if (_fieldList[i] == name) return i;
            }

            throw new Exception ($"Field {name} not found in --fields {String.Join(",",_fieldList)}")  ;
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