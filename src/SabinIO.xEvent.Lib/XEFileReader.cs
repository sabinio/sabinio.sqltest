using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.XEvent.XELite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SabinIO.xEvent.Lib
{
    public class XEFileReader
    {
        public string filename;
        public string connection;
        public string tableName;
        public int batchsize = 100000;
        public int progress { set { events.progress = value; } }
        readonly XEventStream events;
        string[] _fields;
        string[] _columns;

        private readonly ILogger<XEFileReader> _logger;

        public XEFileReader(ILogger<XEFileReader> logger)
        {
            _logger = logger;
            events = new XEventStream(_logger);
        }
        public async Task<(int rowsread, int rowsinserted)> ReadAndLoad(string[] fields, string[] columns, CancellationToken ct)
        {

            using (_logger.BeginScope("Starting ReadAndLoad"))
            {
                events.fields = fields;
                _columns = columns;
                _fields = fields;
                CancellationTokenSource cts = new CancellationTokenSource();
                ct.Register(() => cts.Cancel());

                var readerTask = Task<(int rowsread, int rowsinserted)>.Run(async () =>
                    {
                        _logger.LogInformation("Reader started");
                        _logger.LogInformation("Reader {ProcessorId}-{ThreadId}", Thread.GetCurrentProcessorId(), Thread.CurrentThread.ManagedThreadId);
                        return await ReadAsync(cts.Token);
                    });

                var bulkLoadTask = Task<int>.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("BulkLoadAsync started");
                        _logger.LogInformation("BulkLoadAsync {ProcessorId}-{ThreadId}", Thread.GetCurrentProcessorId(), Thread.CurrentThread.ManagedThreadId);
                        return await BulkLoadAsync(cts.Token);
                    }
                    catch { cts.Cancel(); throw; }
                });

                await Task.WhenAll(readerTask, bulkLoadTask);
                
                //return (1, 1);
                return (readerTask.Result, bulkLoadTask.Result);
            }
        }


        public async Task<int> ReadAsync(CancellationToken ct)
        {
            var samplexml = new XEFileEventStreamer(filename);

            try
            {
                await samplexml.ReadEventStream(() => Task.CompletedTask
                , (x) => Task.Run(() => events.Add(x))
                , ct);
            }
            catch (OperationCanceledException)
            {
                return events.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error reading the xel trace file");
            }
            finally
            {
                events.finishedLoading = true;
            }
            return events.Count;
        }

        public IEnumerable<IXEvent> ReadEvents()
        {
            while (events.Read())
            {
                yield return events.Current;
            }
        }

        public async Task<int> BulkLoadAsync(CancellationToken ct)
        {
            ;
            try
            {
                _logger?.LogInformation("Starting Bulk Load task");
                _logger?.LogInformation("Trace. tarting Bulk Load task");
                using var Con = new SqlConnection(connection);
                Con.Open();
                SqlBulkCopy bc = new SqlBulkCopy(Con)
                {
                    DestinationTableName = tableName,
                    EnableStreaming = true,
                    BatchSize = batchsize,

                };
                
                LoadColumMappings(bc.ColumnMappings, _columns, _fields);

                _logger?.LogInformation("Trace. Starting Task");
                try
                {
                    await bc.WriteToServerAsync(events, ct);
                }
                catch (OperationCanceledException)
                {
                    return bc.RowsCopied;
                }
                return bc.RowsCopied;
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message == "The given ColumnMapping does not match up with any column in the source or destination.")
                {
                    if (_columns.Length == 0)
                    {
                        _logger.LogError("The number fields specified doesnt match the target table\n{xeventfields}", String.Join(",", _fields));
                    }
                    else
                        _logger.LogError("Column mappings don't match target table \nTable columns are matched case sensitive, check the columns passed in below are correct\nxEvent fields {xeventfields}\nTable columns {tablecolumns}", String.Join(",", _fields), String.Join(",", _columns));
                }
                throw;
            }
            catch (Exception ex)
            {

                _logger?.LogError(ex, "error in BulkLoadAsync");
                throw ;
            }
        }
        void LoadColumMappings(SqlBulkCopyColumnMappingCollection collection, string[] columns, string[] fields)
        {
            if (columns.Length > 0)
            {
                for (int colIndex = 0; colIndex < columns.Length; colIndex++)
                {
                    collection.Add( fields[colIndex],columns[colIndex]);
                }
                //add the built in mappings
            }
        }

    }
}



