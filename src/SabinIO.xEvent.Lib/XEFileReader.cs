using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.XEvent.XELite;
using System;
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
        readonly XEventStream events;
        private readonly ILogger<XEFileReader> _logger;

        public XEFileReader(ILogger<XEFileReader> logger)
        {
            _logger = logger;
            events = new XEventStream();
        }
        public async Task<(int rowsread, int rowsinserted)> ReadAndLoad( string[] fields)
        {
            
            using (_logger.BeginScope("Starting ReadAndLoad"))
            {
                events.fields = fields;
                var readerTask = Task<(int rowsread, int rowsinserted)>.Run(async ()=>
                {
                    _logger.LogInformation("Reader started");
                    _logger.LogInformation("Reader {ProcessorId}-{ThreadId}", Thread.GetCurrentProcessorId(), Thread.CurrentThread.ManagedThreadId);
                    return await ReadAsync();
                });

                var bulkLoadTask = Task<int>.Run(async () =>
                {
                    _logger.LogInformation("BulkLoadAsync started");
                    _logger.LogInformation("BulkLoadAsync {ProcessorId}-{ThreadId}", Thread.GetCurrentProcessorId(), Thread.CurrentThread.ManagedThreadId);
                    return await BulkLoadAsync();
                });

                await readerTask.ConfigureAwait(false);
                await bulkLoadTask.ConfigureAwait(false);

                return (readerTask.Result, bulkLoadTask.Result);
            }
        }


        public async Task<int> ReadAsync()
        {
            var samplexml = new XEFileEventStreamer(filename);

            var CT = new CancellationToken();

            await samplexml.ReadEventStream(() => Task.CompletedTask
            , (x) => events.AddAsync(x)
            , CT);

            events.finishedLoading = true;
            return events.Count;
        }

        public async Task<int> BulkLoadAsync()
        {
            
            try
            {
                Debug.WriteLine("Starting Bulk Load task");
                Trace.WriteLine("Trace. tarting Bulk Load task");
                using var Con = new SqlConnection(connection);
                Con.Open();

                SqlBulkCopy bc = new SqlBulkCopy(Con)
                {
                    DestinationTableName = tableName,
                    EnableStreaming = true,
                    BatchSize = batchsize,
                    
                };
                Trace.WriteLine("Trace. Starting Task");
                await bc.WriteToServerAsync(events);
                return bc.RowsCopied;
            }
            catch (Exception ex)
            {
                Trace.Write(ex.Message);
                throw ex;

             
            }
        }
    }
}



