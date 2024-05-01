using Microsoft.SqlServer.XEvent.XELite;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Text;

namespace SabinIO.xEvent.Lib.Tests
{
    class XEventStreamTests
    {
        [Test]
        public void GetValueReturnsConstant()
        {
            var xs = new XEventStream(null);
            var xEvent = new TestXEvent();
            xs.Add(xEvent);
            xs.fields = new string[] { "{100}" };
            Assert.That(xs.Read(),Is.True);

            Assert.That(xs.GetValue(0), Is.EqualTo("100"));
            
        }

        [Test]
        public void CompressedExpressionReturnsByteArray()
        {
            var xs = new XEventStream(null);

            xs.fields = new string[] { "sql_text.compressed" };
            var xEvent = new TestXEvent();
            xEvent.Add("sql_text", "compressthis");
            //var compressed = Gzip
            xs.Add(xEvent);
            Assert.That(xs.Read(), Is.True);

            
            using (MemoryStream zipStream = new MemoryStream((byte[])xs.GetValue(0)))
            using (MemoryStream streamOut = new MemoryStream())
            {
                GZipStream decompressingStream = new GZipStream(zipStream, CompressionMode.Decompress);
                decompressingStream.CopyTo(streamOut);
                var stringBytes = streamOut.ToArray();
                string decoded = Encoding.Unicode.GetString(stringBytes);

                Assert.That(decoded, Is.EquivalentTo("compressthis"));
            }
        }
    }
    class TestXEvent :  Dictionary<string,object>, IXEvent
    {

        public string Name => throw new NotImplementedException();

        public Guid UUID => throw new NotImplementedException();

        public DateTimeOffset Timestamp => throw new NotImplementedException();

        public IReadOnlyDictionary<string, object> Fields => this;

        public IReadOnlyDictionary<string, object> Actions => this;

        public long XEventStartOffsetInBytes => throw new NotImplementedException();

        public long XEventEndOffsetInBytes => throw new NotImplementedException();

        public long XEventSizeInBytes => throw new NotImplementedException();
    }
}
