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
        public  string connection;
        public string tableName;
        public int batchsize = 100000;
        public int progress { set { events.progress = value; } }
        readonly XEventStream events;
        private readonly ILogger<XEFileReader> _logger;

        public XEFileReader(ILogger<XEFileReader> logger)
        {
            _logger = logger;
            events = new XEventStream(_logger);
        }
        public async Task<(int rowsread, int rowsinserted)> ReadAndLoad( string[] fields, CancellationToken ct )
        {
            
            using (_logger.BeginScope("Starting ReadAndLoad"))
            {
                events.fields = fields;
                var readerTask = Task<(int rowsread, int rowsinserted)>.Run(async ()=>
                {
                    _logger.LogInformation("Reader started");
                    _logger.LogInformation("Reader {ProcessorId}-{ThreadId}", Thread.GetCurrentProcessorId(), Thread.CurrentThread.ManagedThreadId);
                    return await ReadAsync(ct);
                });

                var bulkLoadTask = Task<int>.Run(async () =>
                {
                    _logger.LogInformation("BulkLoadAsync started");
                    _logger.LogInformation("BulkLoadAsync {ProcessorId}-{ThreadId}", Thread.GetCurrentProcessorId(), Thread.CurrentThread.ManagedThreadId);
                    return await BulkLoadAsync(ct);
                });

                await readerTask.ConfigureAwait(false);
                await bulkLoadTask.ConfigureAwait(false);

                return (readerTask.Result, bulkLoadTask.Result);
            }
        }


        public async Task<int> ReadAsync(CancellationToken ct)
        {
            var samplexml = new XEFileEventStreamer(filename) ;

            try
            {
                await samplexml.ReadEventStream(() => Task.CompletedTask
                , (x) => events.AddAsync(x)
                , ct);
            }
            catch (OperationCanceledException ex)
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
                _logger?.LogInformation("Trace. Starting Task");
                try
                {
                    await bc.WriteToServerAsync(events, ct);
                }
                catch (OperationCanceledException ex)
                {
                    return bc.RowsCopied;
                }
                return bc.RowsCopied;
            }
            catch (Exception ex)
            {

                _logger?.LogError(ex,"error in BulkLoadAsync");
                throw ex;            
            }
        }
    }
}



