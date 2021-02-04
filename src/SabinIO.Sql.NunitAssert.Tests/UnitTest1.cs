using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using SabinIO.Sql.NUnitAssert;
using System.Linq;

namespace SabinIO.Sql.NUnitAssert.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }


        [Test]
        public void EnsureAssertTableRecordsAreEqualStreamWorks()
        {

            DataTable expected = new DataTable("Source");
            expected.Columns.Add("pk", typeof(int));

            expected.PrimaryKey = new DataColumn[] { expected.Columns[0] };
            expected.Columns.Add("col1", typeof(int));
            expected.LoadDataRow(new object[] { 1, 1 }, true);
            expected.LoadDataRow(new object[] { 3, 1 }, true);
            expected.LoadDataRow(new object[] { 4, 1 }, true);
            expected.LoadDataRow(new object[] { 6, 1 }, true);

            DataTable actual = new DataTable("target");

            actual.Columns.Add("pk", typeof(int));
            actual.PrimaryKey = new DataColumn[] { actual.Columns[0] };
            actual.Columns.Add("col1", typeof(int));
            actual.LoadDataRow(new object[] { 1, 1 }, true);
            actual.LoadDataRow(new object[] { 2, 1 }, true);
            actual.LoadDataRow(new object[] { 3, 2 }, true);
            actual.LoadDataRow(new object[] { 5, 1 }, true);

            var MisMatch = DataTableAssertions.AssertTableRecordsAreEqualStream(expected.CreateDataReader(), actual.CreateDataReader(), "pk", new List<string>(), new List<string>());

            string missingRows = String.Join("\n", MisMatch.Where(s => s.column == -1).Select(s => $"pk={s.pk} Missing row on expected {s.expectedMissing} actual {s.actualMissing}"));
            string missingColumns = String.Join("\n", MisMatch.Where(s => s.column > -1).Select(s => $"pk={s.pk} Column mismatch expected {s.expectedValue} actual {s.actualValue}"));
            TestContext.WriteLine(missingRows);
            TestContext.WriteLine(missingColumns);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(MisMatch.Where(m => m.column == -1 & m.pk == 2).Count() == 1, "Should find missing row for PK 2");
                Assert.IsTrue(MisMatch.Where(m => m.column == -1 & m.pk == 2 & m.expectedMissing & !m.actualMissing).Count() == 1, "Should find missing row for PK 2 with expected missing");
                Assert.IsTrue(MisMatch.Where(m => m.column == -1 & m.pk == 4).Count() == 1, "Should find missing row for PK 4");
                Assert.IsTrue(MisMatch.Where(m => m.column == -1 & m.pk == 4 & !m.expectedMissing & m.actualMissing).Count() == 1, "Should find missing row for PK 4 with actual missing");
                Assert.AreEqual(1, MisMatch.Count(m => m.column > -1), "Should only find 1 row is wrong");

                Assert.IsTrue(MisMatch.Any(m => m.column > -1 & m.pk == 3), "Should find row is wrong for PK 3");
                var wrongrow = MisMatch.Where(m => m.column > -1 & m.pk == 3);
                Assert.AreEqual(1, wrongrow.Count(), "PK Row 3 should have one column wrong");
                Assert.AreEqual(1, wrongrow.SingleOrDefault().column, "PK Row 3 should have column 1 wrong");

                Assert.AreEqual(1, (int)wrongrow.SingleOrDefault().expectedValue, "PK Row 3 should have expectedValue as 1");
                Assert.AreEqual(2, (int)wrongrow.SingleOrDefault().actualValue, "PK Row 3 should have actualValue as 2");
            }
            );
        }
    }
}