using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace SabinIO.SqlTest.Tests
{
    class DBReaderTests
    {
        [Test]
        public void EnsureReaderReadsTheRightNumberOfRowsForTuple()
        {
            List<(string name, int amount)> foo = new List<(string, int)>();
            foo.Add(("simon", 1));
            foo.Add(("simon1", 2));
            foo.Add(("simon2", 3));

            var reader = new DBReader<(string, int)>(foo,
                ((string name, int amount) data, int key) => key switch {
                    0 => data.name,
                    1 => data.amount,
                    _ => new MissingFieldException("key")
                },
                null);
            ;
            int pos = 0;
            Assert.That(reader.FieldCount, Is.EqualTo(2), "Should have 2 fields derived from the Tuple");
            while (reader.Read())
            {
                Assert.That(foo[pos].name, Is.EqualTo(reader.GetValue(0)));
                Assert.That(foo[pos].amount, Is.EqualTo(reader.GetValue(1)));
                pos++;
            }
            Assert.That(pos, Is.EqualTo(foo.Count));
        }
        struct TestStruct
        {
            public string name;
            public int amount;
            public int disc;
        }

        [Test]
        public void EnsureReaderReadsTheRightNumberOfRowsForStruct()
        {
            List<TestStruct> foo = new()
            {
                new() { name = "simon", amount = 1 },
                new() { name = "simon2", amount = 2 },
                new() { name = "simon3", amount = 3 },
            };

            var reader = new DBReader<TestStruct>(foo,
                (TestStruct data, int key) => key switch {
                    0 => data.name,
                    1 => data.amount,
                    _ => new MissingFieldException("key")
                },
                null);
            ;
            int pos = 0;
            Assert.That(reader.FieldCount, Is.EqualTo(3), "Should have 2 fields derived from the Tuple");
            while (reader.Read())
            {
                Assert.That(foo[pos].name, Is.EqualTo(reader.GetValue(0)));
                Assert.That(foo[pos].amount, Is.EqualTo(reader.GetValue(1)));
                pos++;
            }
            Assert.That(pos, Is.EqualTo(foo.Count));
        }
    }
}
