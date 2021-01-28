using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using SabinIO.Sql.NUnitAssert;

namespace SabinIO.Sql.Tests
{
    public class ParseTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ParsingCommandResultsInArrayOfValues()
        {
            var query = @"
declare @p3 dbo.TestType
insert into @p3 values(100,N'simon')
,(99,'Fred')
,(22,'able')
";
            DataTable dt = new DataTable();
            dt.Columns.Add().DataType = typeof(int);
            dt.Columns.Add().DataType = typeof(string);
            dt.Rows.Add(100, "simon");
            dt.Rows.Add(99, "Fred");
            dt.Rows.Add(22, "able");

            var Parameters = Parse.GetTVP(query);


            Assert.That(Parameters.Values.Select(p => p.Name), Is.EquivalentTo(new string[] { "@p3" }));
            Assert.That(Parameters.Values.Select(p => p.Types), Is.EquivalentTo(new List<string[]>() { new string[] { "Integer", "String" } }));

            DataTableAssertions.AssertTableRecordsAreEqual(dt, Parameters["@p3"].RowValues);


        }

        [Test]
        public void ParsingCommandWithMultipleTVPsReturnsMultipleArrayOfValues()
        {
            var query = @"
declare @p3 dbo.TestType
insert into @p3 values(100,N'simon')
insert into @p3 values(99,N'Fred')

declare @p4 dbo.TestType
insert into @p4 values(100,N'simon')
insert into @p4 values(999,N'smith')

exec sp_executesql N'select * from @p2 union select * from @p4',N'@p2 [TestType] READONLY,@p4 [TestType] READONLY',@p2=@p3,@p4=@p4
";
            var Parameters = Parse.GetTVP(query);


            Assert.That(Parameters.Count, Is.EqualTo(2), "Should find 2 parameters");
            Assert.That(Parameters.Values.Select(p => p.Name).ToArray(), Is.EquivalentTo(new string[] { "@p3", "@p4" }));

            Assert.That(Parameters["@p3"].Types, Is.EquivalentTo(new string[] { "Integer", "String" }));
            Assert.That(Parameters["@p4"].Types, Is.EquivalentTo(new string[] { "Integer", "String" }));

            DataTable dt = new DataTable();
            dt.Columns.Add().DataType = typeof(int);
            dt.Columns.Add().DataType = typeof(string);
            dt.Rows.Add(100, "simon");
            dt.Rows.Add(99, "Fred");

            DataTable dt2 = new DataTable();
            dt2.Columns.Add().DataType = typeof(int);
            dt2.Columns.Add().DataType = typeof(string);
            dt2.Rows.Add(100, "simon");
            dt2.Rows.Add(999, "smith");

            var x = dt.AsEnumerable().Intersect(Parameters["@p3"].RowValues.AsEnumerable(),DataRowComparer.Default);

            NUnitAssert.DataTableAssertions.AssertTableRecordsAreEqual(Parameters["@p3"].RowValues, dt);
            NUnitAssert.DataTableAssertions.AssertTableRecordsAreEqual(Parameters["@p4"].RowValues, dt2);

        }

        [Test]
        public void ParsingCommandGetSpExecuteSql()
        {
            var query = @"
declare @p4 dbo.TestType
insert into @p4 values(100,N'simon')

declare @p6 dbo.TestType
insert into @p6 values(100,N'simon')

exec sp_executesql N'select * from @p2 union select * from @p4 where @p3 = @p3 and @p1 = @p1',N'@p1 int,@p2 [TestType] READONLY,@p3 int,@p4 [TestType] READONLY',@p1=100,@p2=@p4,@p3=3333,@p4=@p6";
            var stmt = Parse.GetStatement(query);
            Assert.That(stmt.statement, Is.EqualTo("select * from @p2 union select * from @p4 where @p3 = @p3 and @p1 = @p1"));
            Assert.That(stmt.parameters.Keys, Is.EquivalentTo(new string[] { "@p1", "@p2","@p3", "@p4" }));
            Assert.That(stmt.parameters.Values.Select(v=>v.Type), Is.EquivalentTo(new string[] { "int", "[TestType]", "int", "[TestType]" }));
            Assert.That(stmt.parameters.Values.Select(v => v.Value), Is.EquivalentTo(new string[] { "100", "@p4", "3333", "@p6" }));
        }

        [Test]
        public void ParsingCommandGetSqlCommand()
        {
            var query = @"
declare @p4 dbo.TestType
insert into @p4 values(100,N'simon')
insert into @p4 values(100,N'simon')
insert into @p4 values(100,N'simon')

declare @p6 dbo.TestType
insert into @p6 values(100,N'simon')

exec sp_executesql N'select * from @p2 union select * from @p4 where @p3 = @p3 and @p1 = @p1',N'@p1 int,@p2 [TestType] READONLY,@p3 int,@p4 [TestType] READONLY',@p1=100,@p2=@p4,@p3=3333,@p4=@p6";
            using SqlConnection c = new SqlConnection("data source=.;initial catalog=tempdb;trusted_connection=true");
            c.Query("drop type if exists TestType ");
            c.Query("create type TestType as table (intColumn int,varcharColumn varchar(100))");
            try
            {
                var stmt = Parse.GetSqlCommand(query);
                stmt.Connection = c;
                c.Open();
                stmt.ExecuteNonQuery();

            }
            finally
            {
            }

        }



        [Test]
        public void RunATVPCommandToGetTheSample()
        {

            using SqlConnection c = new SqlConnection("data source=.;initial catalog=tempdb;trusted_connection=true");
            try
            {
                c.Query("drop type if exists TestType ");
                c.Query("create type TestType as table (intColumn int,varcharColumn varchar(100))");

                DataTable dt = new DataTable();
                dt.Columns.Add("intColumn", typeof(int));
                dt.Columns.Add("varcharColumn", typeof(string));
                dt.Rows.Add( 100, "simon");
                dt.Rows.Add(100, "simon");
                dt.Rows.Add(100, "simon");
                
                c.Query("select * from @p2", new { p1 = 100,p2 = dt.AsTableValuedParameter("TestType") ,p3=3333} );;
            }
            catch (Exception ex)
            {
                throw ex;
            }
           
            finally
            {
            }
        }

        [Test]
        public void RunCommandWithMultipleTVPsToGetTheSample()
        {

            using SqlConnection c = new SqlConnection("data source=.;initial catalog=tempdb;trusted_connection=true");
            try
            {

                c.Query("drop type if exists TestType ");
                c.Query("create type TestType as table (intColumn int,varcharColumn varchar(100))");

                DataTable dt = new DataTable();
                dt.Columns.Add("intColumn", typeof(int));
                dt.Columns.Add("varcharColumn", typeof(string));
                dt.Rows.Add(100, "simon");
                c.Query("select * from @p2 union select * from @p4 where @p3 = @p3 and @p1 = @p1", new { p1 = 100, p2 = dt.AsTableValuedParameter("TestType"), p3 = 3333 , p4 = dt.AsTableValuedParameter("TestType") }); 
            }
            catch (Exception ex)
            {
                throw ex;
            }

            finally
            {
               

            }
        }
    }
    public class SameRowValuesConstraint : NUnit.Framework.Constraints.Constraint
    {
        public SameRowValuesConstraint(DataTable expected)
        {

        }

        public override ConstraintResult ApplyTo<TActual>(TActual actual) 
        {
            var x = DataRowComparer.Default;

            var t = new ConstraintResult(this, actual, true);
            return t;
        }
    }
    public static class CustomConstraintExtensions
    {
        public static SameRowValuesConstraint SameRowValues(this ConstraintExpression expression, DataTable expected)
        {
            var constraint = new SameRowValuesConstraint(expected);
            expression.Append(constraint);
            return constraint;
        }
    }
}