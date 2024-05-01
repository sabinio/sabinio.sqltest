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
        string TraceConnectionString;
        [SetUp]
        public void Setup()
        {
            if  (TestContext.Parameters.Names.Contains("TraceConnectionString")){
                TraceConnectionString = TestContext.Parameters["TraceConnectionString"];
            }
            else
            {
                TraceConnectionString = "";// throw new FileNotFoundException("need to set the run settings file");
            }
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
            Assert.That(Parameters["@p3"].Type, Is.EqualTo("dbo.TestType"));
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
            Assert.That(stmt.parameters.Values.Select(v=>v.FullType), Is.EquivalentTo(new string[] { "int", "[TestType]", "int", "[TestType]" }));
            Assert.That(stmt.parameters.Values.Select(v => v.Value), Is.EquivalentTo(new string[] { "100", "@p4", "3333", "@p6" }));
        }

        [Test]
        [Category("Integration")]

        public void ParsingQueryWithTVPandExecSqlCommand()
        {
            var query = @"
declare @p4 dbo.TestType
insert into @p4 values(100,N'simon')
insert into @p4 values(100,N'simon')
insert into @p4 values(100,N'simon')

declare @p6 dbo.TestType
insert into @p6 values(100,N'simon')

exec sp_executesql N'select * from @p2 union select * from @p4 where @p3 = @p3 and @p1 = @p1',N'@p1 int,@p2 [TestType] READONLY,@p3 int,@p4 [TestType] READONLY',@p1=100,@p2=@p4,@p3=3333,@p4=@p6";
            using SqlConnection c = new SqlConnection(TraceConnectionString);
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
        [Category("Integration")]
        public void RunATVPCommandToGetTheSample()
        {

            using SqlConnection c = new(TraceConnectionString);
            c.Query("drop type if exists TestType ");
            c.Query("create type TestType as table (intColumn int,varcharColumn varchar(100))");

            DataTable dt = new DataTable();
            dt.Columns.Add("intColumn", typeof(int));
            dt.Columns.Add("varcharColumn", typeof(string));
            dt.Rows.Add(100, "simon");
            dt.Rows.Add(100, "simon");
            dt.Rows.Add(100, "simon");

            c.Query("select * from @p2", new { p1 = 100, p2 = dt.AsTableValuedParameter("TestType"), p3 = 3333 }); ;

        }

        [Test]
        [Category("Integration")]

        public void RunCommandWithMultipleTVPsToGetTheSample()
        {
            using SqlConnection c = new(TraceConnectionString);

            c.Query("drop type if exists TestType ");
            c.Query("create type TestType as table (intColumn int,varcharColumn varchar(100))");

            DataTable dt = new DataTable();
            dt.Columns.Add("intColumn", typeof(int));
            dt.Columns.Add("varcharColumn", typeof(string));
            dt.Rows.Add(100, "simon");
            c.Query("select * from @p2 union select * from @p4 where @p3 = @p3 and @p1 = @p1", new { p1 = 100, p2 = dt.AsTableValuedParameter("TestType"), p3 = 3333, p4 = dt.AsTableValuedParameter("TestType") });

        }
        [Test]
        public void ParsingStringParams()
        {
            var query = @"exec dbo.SomeProc @User='bob'";

            var stmt = Parse.GetStatement(query);
            Assert.That(stmt.statement, Is.EqualTo("dbo.SomeProc"));
            Assert.That(stmt.parameters.Keys, Is.EquivalentTo(new string[] { "@User"}));
            Assert.That(stmt.parameters["@User"].Type, Is.EqualTo("varchar"));

            var cmd = Parse.GetSqlCommand(query);
            Assert.That(cmd.CommandText, Is.EqualTo("dbo.SomeProc"));
            Assert.That(cmd.Parameters.Contains("@User") , Is.True,"User parameter exists");
            Assert.That(cmd.Parameters["@User"].SqlDbType, Is.EqualTo(SqlDbType.VarChar));

        }

        
        [Test]
        public void ParsingExecWithOutParams()
        {
            var query = @"exec dbo.SomeProc";

            var stmt = Parse.GetStatement(query);
            Assert.That(stmt.statement, Is.EqualTo("dbo.SomeProc"));
            Assert.That(stmt.parameters.Count, Is.EqualTo(0));

            var cmd = Parse.GetSqlCommand(query);
            Assert.That(cmd.CommandText, Is.EqualTo("dbo.SomeProc"));

        }
        [Test]
        [TestCase("bit", SqlDbType.Bit, 0, 0)]
        [TestCase("tinyint", SqlDbType.TinyInt, 0, 0)]
        [TestCase("smallint", SqlDbType.SmallInt, 0, 0)]
        [TestCase("int", SqlDbType.Int, 0, 0)]
        [TestCase("bigint", SqlDbType.BigInt, 0, 0)]
        [TestCase("varchar", SqlDbType.VarChar, 0, 0)]
        [TestCase("varchar(20)", SqlDbType.VarChar, 20, 0)]
        [TestCase("varchar(max)", SqlDbType.VarChar, -1, 0)]
        [TestCase("nvarchar", SqlDbType.NVarChar, 0, 0)]
        [TestCase("nvarchar(20)", SqlDbType.NVarChar, 20, 0)]
        [TestCase("nvarchar(max)", SqlDbType.NVarChar, -1, 0)]
        [TestCase("decimal", SqlDbType.Decimal, 0, 0)]
        [TestCase("decimal(1)", SqlDbType.Decimal, 1, 0)]
        [TestCase("decimal(10,2)", SqlDbType.Decimal, 10, 2)]
        [TestCase("numeric", SqlDbType.Decimal, 0, 0)]
        [TestCase("numeric(1)", SqlDbType.Decimal, 1, 0)]
        [TestCase("numeric(10,2)", SqlDbType.Decimal, 10, 2)]
        [TestCase("float", SqlDbType.Float, 0, 0)]
        [TestCase("varbinary", SqlDbType.VarBinary, 0, 0)]
        [TestCase("varbinary(max)",SqlDbType.VarBinary,-1,0)]
        public void EnsureCanParseSp_executeWithIntParam(string sqlTypeString,SqlDbType SqlType, int size, int scale)
        {
            var query = @$"
exec sp_executesql N'select @p1',N'@p1 {sqlTypeString}',NULL
";
            var stmt = Parse.GetStatement(query);
            Assert.That(stmt.statement, Is.EqualTo("select @p1"));
            Assert.That(stmt.parameters.Keys, Is.EquivalentTo(new string[] { "@p1" }));
            Assert.That(stmt.parameters["@p1"].FullType, Is.EqualTo(sqlTypeString));
            Assert.That(stmt.parameters["@p1"].length, Is.EqualTo(size));
            Assert.That(stmt.parameters["@p1"].Scale, Is.EqualTo(scale));


            var cmd = Parse.GetSqlCommand(query);
            Assert.That(cmd.CommandText, Is.EqualTo("select @p1"));
            Assert.That(cmd.Parameters.Contains("@p1"), Is.True, "User parameter exists");
            Assert.That(cmd.Parameters["@p1"].SqlDbType, Is.EqualTo(SqlType));
            Assert.That(cmd.Parameters["@p1"].Size, Is.EqualTo(size));
            Assert.That(cmd.Parameters["@p1"].Scale, Is.EqualTo(scale));

        }
        [Test]
        public void ParsingTVPEnsureSQLCommandParams()
        {
            var query = @"
declare @p2 dbo.TestType
insert into @p2 values(100,N'simon')

exec myProc @p1 = @p2
";
            var cmd = Parse.GetSqlCommand(query);
            Assert.That(cmd.CommandText, Is.EqualTo("myProc"));
            Assert.That(cmd.Parameters.Contains("@p1"), Is.True, "User parameter exists");
            Assert.That(cmd.Parameters["@p1"].TypeName, Is.EqualTo("dbo.TestType"));

        }

        [Test]
        public void ParsingOutputParams()
        {
            var query = @"
declare @p10 int  set @p10=30396189  declare @p11 int  set @p11=1  exec dbo.SomeProc @info=NULL,@User='bob',@Id=@p10 output,@Ver=@p11 output  select @p10, @p11
"; 

            var stmt = Parse.GetStatement(query);
            Assert.That(stmt.statement, Is.EqualTo("dbo.SomeProc"));
            Assert.That(stmt.parameters.Keys, Is.EquivalentTo(new string[] { "@info", "@User", "@Id", "@Ver" }));
            Assert.That(stmt.parameters.Values.Where(p=>p.isOutput).Select(s=>s.Name), Is.EquivalentTo(new string[] {"@Id", "@Ver" }));
         }
        [Test]
        [Category("Integration")]

        public void GetOutputStatement()
        {

            using SqlConnection c = new(TraceConnectionString);
            var p = new DynamicParameters();
            p.Add("p1", dbType: DbType.Int32, value: 50, direction: ParameterDirection.InputOutput) ;
            c.Execute(@"set @p1 = @p1*2",p);
            Assert.That(p.Get<int>("p1"),Is.EqualTo(100));
                       
        }
    }
    public class SameRowValuesConstraint : NUnit.Framework.Constraints.Constraint
    {
        public SameRowValuesConstraint(DataTable expected)
        {

        }

        public override string Description => "Some unknown description";

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