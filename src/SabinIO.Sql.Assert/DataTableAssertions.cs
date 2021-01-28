using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace SabinIO.Sql.NUnitAssert
{
    public class DataTableAssertions
    {
        public static void AssertTableRecordsAreEqual(DataTable expectedTable, DataTable actualTable)
        
        {
            var empty = new List<string>();
            AssertTableRecordsAreEqual("", new string[] { }, expectedTable, actualTable, empty, empty, null, null);
        }

            public static void AssertTableRecordsAreEqual(string tableName, string[] pks, DataTable expectedTable, DataTable actualTable, List<string> ColumnsToSkip, List<string> ColumnsToBeDifferent,
            Func<string, DataRow, DataRow, bool>[] IgnoreRowRule
            , Func<IgnoreColumnRuleType, bool>[] IgnoreColumnRule)
        {
            JsonSerializerOptions opt = new JsonSerializerOptions();
            opt.WriteIndented = true;

            Assert.Multiple(() =>
            {
                Assert.IsNotNull(actualTable, "Table is empty");
                Assert.AreEqual(expectedTable.Rows.Count, actualTable.Rows.Count, "Number of records in actual and expected tables are different");

                int[] columnSize = new int[expectedTable.Columns.Count];//
                for (int i = 0; i < columnSize.Length; i++)
                {
                    columnSize[i] = Math.Min(20, Math.Max(expectedTable.Columns[i].ColumnName.Length, Math.Max(expectedTable.AsEnumerable().Max(r => r[i].ToString().Length), actualTable.AsEnumerable().Max(r => r[i].ToString().Length))));
                }

                for (int i = 0; i <= expectedTable.Rows.Count - 1; i++)
                {
                    if (IgnoreRowRule != null && IgnoreRowRule.Any(r => r(expectedTable.TableName, expectedTable.Rows[i], actualTable.Rows[i])))
                    {
                        TestContext.WriteLine("Ignoring row {i}");
                    }
                    else
                    {
                        foreach (var item in expectedTable.Columns.Cast<DataColumn>().Where(c => !ColumnsToSkip.Contains(c.ColumnName)))
                        {
                            if (IgnoreColumnRule != null && IgnoreColumnRule.Any(r => r(new IgnoreColumnRuleType() { objectName = tableName, expectedRow = expectedTable.Rows[i], actualRow = actualTable.Rows[i], column = item.ColumnName })))
                            {
                                TestContext.WriteLine("Ignoring row {i}");
                            }
                            else
                            {
                                if (ColumnsToBeDifferent.Contains(item.ColumnName))
                                    Assert.AreNotEqual(expectedTable.Rows[i][item.ColumnName], actualTable.Rows[i][item.ColumnName], $"Row {i} Column {item.ColumnName} don't match");
                                else
                                {
                                    bool isEqual = true;
                                    var expected = expectedTable.Rows[i][item.ColumnName];
                                    var actual = actualTable.Rows[i][item.ColumnName];
                                    switch (item.DataType.ToString())
                                    {
                                        case "System.Byte[]":
                                            var arrayExpected = (byte[])expected;
                                            var arrayActual = (byte[])actual;

                                            for (int byteIndex = 0; byteIndex < arrayExpected.Length | !isEqual; byteIndex++)
                                            {
                                                isEqual = arrayExpected[byteIndex] == arrayActual[byteIndex];
                                            }
                                            break;
                                        case "System.DateTime":
                                            DateTime expectedDateTime = (DateTime)expected;
                                            DateTime actualDateTime = (DateTime)actual;
                                            isEqual = Math.Abs(expectedDateTime.Ticks - actualDateTime.Ticks) <= 3000;
                                            break;
                                        default:
                                            isEqual = actual.Equals(expected);
                                            break;
                                    }
                                    if (!isEqual)
                                    {

                                        var ColumnList = String.Join(",", expectedTable.Columns.Cast<DataColumn>().Select(c => $"{c.ColumnName,20}"));

                                        var compare = from c in expectedTable.Columns.Cast<DataColumn>()
                                                      select new { c.ColumnName, values = new { expected = expectedTable.Rows[i][c.ColumnName], actual = actualTable.Rows[i][c.ColumnName] } };

                                        TestContext.WriteLine(JsonSerializer.Serialize(compare, opt));
                                        Assert.Fail($"Row {i}-{String.Join(",", pks.Select(pk => expectedTable.Rows[i][pk]))} Column {item.ColumnName} don't match\n\tExpected = {expectedTable.Rows[i][item.ColumnName]}\n\tActual     = {actualTable.Rows[i][item.ColumnName]}");
                                    }
                                }
                            }
                        }
                    }

                }

                for (int i = expectedTable.Rows.Count; i <= actualTable.Rows.Count - 1; i++)
                {
                    var compare = from c in actualTable.Columns.Cast<DataColumn>()
                                  select new { c.ColumnName, values = new { actual = actualTable.Rows[i][c.ColumnName] } };

                    TestContext.WriteLine(JsonSerializer.Serialize(compare, opt));
                    Assert.Fail("Extra Rows found in actual rows");
                }
            });

        }

        public static List<(int pk, bool expectedMissing, bool actualMissing, int column, object expectedValue, object actualValue)>
            AssertTableRecordsAreEqualStream(DbDataReader expectedTable, DbDataReader actualTable, string pk, List<string> ColumnsToSkip, List<string> ColumnsToBeDifferent)
        {

            Assert.IsNotNull(actualTable, "Table is empty");
            Assert.AreEqual(expectedTable.FieldCount, actualTable.FieldCount, "Number of columns in actual and expected tables are different");

            for (int fieldIndex = 0; fieldIndex < expectedTable.FieldCount; fieldIndex++)
            {

                Assert.AreEqual(expectedTable.GetName(fieldIndex), actualTable.GetName(fieldIndex), $"column with index {fieldIndex} name deosn't match");
            }
            Assert.IsFalse(expectedTable.GetSchemaTable().Columns.Cast<DataColumn>().Any(dc => !actualTable.GetSchemaTable().Columns.Contains(dc.ColumnName)), "Table column names are different");


            int[] columnSize = new int[expectedTable.FieldCount];//
            object[] expectedColumns = new object[columnSize.Length];
            object[] actualColumns = new object[columnSize.Length];

            bool read = expectedTable.Read() & actualTable.Read();

            for (int i = 0; i < columnSize.Length; i++)
            {
                columnSize[i] = Math.Min(20, expectedTable.GetName(i).Length);
                expectedColumns[i] = expectedTable[i];
                actualColumns[i] = actualTable[i];
            }
            int pkOrdinal = expectedTable.GetOrdinal(pk);

            List<(int pk, bool expectedMissing, bool actualMissing, int column, object expectedValue, object actualValue)> MisMatch = new List<(int, bool, bool, int, object, object)>();

            while (read)
            {
                var expectedPkValue = expectedTable.GetInt32(pkOrdinal);
                var actualPkValue = actualTable.GetInt32(pkOrdinal);
                //if match check rows
                if (expectedPkValue == actualPkValue)
                {

                    for (int i = 0; i < columnSize.Length; i++)
                    {
                        if (!expectedTable[i].Equals(actualTable[i]))
                        {
                            MisMatch.Add((expectedPkValue, true, true, i, expectedTable[i], actualTable[i]));
                        }
                    }
                    read = expectedTable.Read() & actualTable.Read();
                }
                else if (expectedPkValue < actualPkValue)
                {
                    read = expectedTable.Read();

                    MisMatch.Add((expectedPkValue, false, true, -1, null, null));
                }
                else if (expectedPkValue > actualPkValue)
                {
                    read = actualTable.Read();

                    MisMatch.Add((actualPkValue, true, false, -1, null, null));
                }
                else
                {
                   
                    Assert.Fail("Shouldn't be here");
                }
                //read each table


            }
            return MisMatch;
            /*   if (MisMatch.Count > 0)
               {
                   string MissingRows = String.Join("\n", MisMatch.Where(s => s.column == -1).Select(s => $"pk={s.pk} Missing row on expected {s.expected} actual {s.actual}"));
                   string missingColumns =MisMatch.Where(s => s.column > -1).Select(s => $"pk={s.pk} Column mismatch expected {s.expectedValue} actual {s.actualValue}"));

               }*/
        }

        private void AssertTableSchemasAreEqual(DataTable expectedTable, DataTable actualTable
            , List<string> ColumnsToSkip
            , List<string> ColumnsToBeDifferent
            , Func<string, bool> ColumnFilter)
        {
            Assert.Multiple(() =>
            {
                Assert.IsNotNull(actualTable, "Table is empty");
                Assert.AreEqual(expectedTable.Columns.Count, actualTable.Columns.Count, "Number of columns in actual and expected tables are different");

                var ColumnsNotInActual = expectedTable.Columns.Cast<DataColumn>().Where(c => !actualTable.Columns.Contains(c.ColumnName)).Select(c => c.ColumnName);
                var ColumnsNotInExpected = actualTable.Columns.Cast<DataColumn>().Where(c => !expectedTable.Columns.Contains(c.ColumnName)).Select(c => c.ColumnName);
                Assert.AreEqual(0, ColumnsNotInActual.Count(), $"Columns not in Actual results = {String.Join(",", ColumnsNotInActual)}");
                Assert.AreEqual(0, ColumnsNotInExpected.Count(), $"Columns not in Expected results = {String.Join(",", ColumnsNotInExpected)}");

                Assert.IsFalse(expectedTable.Columns.Cast<DataColumn>().Any(dc => !actualTable.Columns.Contains(dc.ColumnName)), "Table column names are different");

                Assert.Multiple(() =>
                {
                    foreach (DataColumn expectedColumn in expectedTable.Columns)
                    {
                        var actualColumn = actualTable.Columns[expectedColumn.ColumnName];
                        Assert.AreEqual(expectedColumn.DataType, actualColumn.DataType, $"Data Types should match for column {expectedColumn.ColumnName}");
                        Assert.AreEqual(expectedColumn.AutoIncrement, actualColumn.AutoIncrement, $"Auto increment should match for column {expectedColumn.ColumnName}");
                        Assert.AreEqual(expectedColumn.MaxLength, actualColumn.MaxLength, $"Max Length should match for column {expectedColumn.ColumnName}");
                        Assert.AreEqual(expectedColumn.Ordinal, actualColumn.Ordinal, $"Ordinal Position should match for column {expectedColumn.ColumnName}");

                    }
                });
            });
        }

    }
}
