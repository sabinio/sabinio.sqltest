using Microsoft.SqlServer.XEvent.XELite;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
    }
    class TestXEvent :  Dictionary<string,object>, IXEvent
    {
        
        public string Name => throw new NotImplementedException();

        public Guid UUID => throw new NotImplementedException();

        public DateTimeOffset Timestamp => throw new NotImplementedException();

        public IReadOnlyDictionary<string, object> Fields => this;

        public IReadOnlyDictionary<string, object> Actions => this;
    }
}
