using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SabinIO.Sql
{
    public class Parse
    {
        public static SqlCommand GetSqlCommand(string sql)
        {
            var TVPs = GetTVP(sql);
            var stmt = GetStatement(sql);
            var c = new SqlCommand() { CommandText = stmt.statement };
            if (stmt.isProc)
            {
                c.CommandType = CommandType.StoredProcedure;
            }
            foreach (var SqlParam in stmt.parameters.Values)
            {

                SqlParameter p = new SqlParameter
                {
                    ParameterName = SqlParam.Name,
                    Size = SqlParam.length,
                    Scale = SqlParam.Scale
                };
                c.Parameters.Add(p);
                if (SqlParam.Value == null)
                {
                    p.Value = DBNull.Value;
                }
                else { p.Value = SqlParam.Value; }
                switch (SqlParam.Type?.ToLower())
                {
                    case "varchar":
                    case "nvarchar":

                    case "datetime":
                    case "tinyint":
                    case "smallint":
                    case "int":
                    case "bigint":
                    case "decimal":
                    case "float":
                        p.SqlDbType = Enum.Parse<SqlDbType>(SqlParam.Type, true);
                        break;
                    case "numeric":
                        p.SqlDbType = SqlDbType.Decimal;
                        break;
                    case "bit":
                        p.SqlDbType = SqlDbType.Bit;
                        p.Value = SqlParam.Value == "1";
                        break;
                    case "varbinary":
                    case "binary":
                        p.SqlDbType = Enum.Parse<SqlDbType>(SqlParam.Type, true);
                        p.Value = StringToByteArray(SqlParam.Value);
                        break;
                    case "variable":
                        //This is when we have output parameters
                        //Assume int until we load proc parameters types from DB
                        if (TVPs.ContainsKey(SqlParam.Value))
                        {
                            p.SqlDbType = SqlDbType.Structured;
                            p.TypeName = TVPs[SqlParam.Value].Type;
                            p.Value = TVPs[SqlParam.Value].RowValues;
                        }
                        else
                        {
                            p.SqlDbType = SqlDbType.Int;
                        }
                        break;
                    case "default":
                        //This is how you have the proc use a default value
                        p.Value = null;
                        break;
                    default:
                        if (SqlParam.Value == null)
                        {
                            //Assume an int  
                            if (SqlParam.Name == "@ExternalReference" && stmt.statement == "dbo.p_InsertPendingDocument")
                            {
                                p.SqlDbType = SqlDbType.UniqueIdentifier;
                            }
                            else if (SqlParam.Name.EndsWith("Date") && stmt.statement == "dbo.p_InsertPendingDocument")
                            {
                                p.SqlDbType = SqlDbType.DateTime;
                            }
                            else p.SqlDbType = SqlDbType.Int;
                            p.Value = DBNull.Value;
                        }
                        else if (TVPs.ContainsKey(SqlParam.Value))
                        {
                            p.SqlDbType = SqlDbType.Structured;
                            p.TypeName = TVPs[SqlParam.Value].Type;
                            p.Value = TVPs[SqlParam.Value].RowValues;
                        }
                        else
                        {
                            throw new NotSupportedException($"{SqlParam.Name}-{SqlParam.Type}");
                        }
                        break;
                }
                if (SqlParam.isOutput)
                {
                    p.Direction = ParameterDirection.Output;
                    //this is defaulted to int but not all output params will be int
                    p.SqlDbType = SqlDbType.Int;
                }



            }
            return c;
        }


        private  static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length-2;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i/2] = Convert.ToByte(hex.Substring(i+2, 2), 16);
            return bytes;

        }
        public static Dictionary<string,TVPParameter> GetTVP(string sql)
        {

            using var rdr = new StringReader(sql);
            IList<ParseError> errors = new List<ParseError>();
            var parser = new TSql150Parser(true, SqlEngineType.All);
            var tree = parser.Parse(rdr, out errors);


            if (errors.Count > 0)
            {
                throw new AggregateException(errors.Select(e => new SyntaxErrorException($"{e.Message} Line {e.Line},{e.Column}")));
            }
            var p = new ParamVisitor();
            tree.Accept(p);
            return p.Parameters;


        }

        public static Batch GetStatement(string query)
        {
            using var rdr = new StringReader(query);
            IList<ParseError> errors = new List<ParseError>();
            var parser = new TSql150Parser(true, SqlEngineType.All);
            var tree = parser.Parse(rdr, out errors);


            if (errors.Count > 0)
            {
                throw ParseException.CreateSingleOrAggregate(errors);
            }
            var p = new StatementVisitor(parser);
            tree.Accept(p);
            return new Batch() { statement = p.statement, isProc = p.isProc, parameters = p.Parameters };
        }
    }
      

        public class ParseException:SyntaxErrorException{
        public ParseException(ParseError error):base($"{error.Message} Line {error.Line},{error.Column}") { }
        public static Exception CreateSingleOrAggregate(IList<ParseError> errors)
        {
            if (errors.Count > 1)
            {
                return new AggregateException(errors.Select(e => new ParseException(e)));
            }
            else
            {
                return new ParseException(errors[0]);
            }
        }
    }

}
