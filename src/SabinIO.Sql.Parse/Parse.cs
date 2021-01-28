using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
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
            var c = new SqlCommand();
            c.CommandText = stmt.statement;

            foreach (var SqlParam in stmt.parameters.Values)
            {
                switch (SqlParam.Type.ToLower())
                {
                    case "int":
                        c.Parameters.Add(SqlParam.Name, SqlDbType.Int).Value = SqlParam.Value;
                        break;
                    case "varchar":
                        throw new Exception("not supported");
                    default:
                        var tvp = c.CreateParameter();
                        tvp.ParameterName = SqlParam.Name;
                        tvp.SqlDbType = SqlDbType.Structured;
                        tvp.TypeName = $"dbo.{SqlParam.Type}";
                        tvp.Value = TVPs[SqlParam.Value].RowValues;


                        c.Parameters.Add(tvp);
                        //assume TVP
                        break;
                }
            }
            return c;
        }


        public static Dictionary<string,TVPParameter> GetTVP(string sql)
        {

            using (var rdr = new StringReader(sql))
            {
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


        }

        public static (string statement, Dictionary<string,Parameter> parameters) GetStatement(string query)
        {
            using (var rdr = new StringReader(query))
            {
                IList<ParseError> errors = new List<ParseError>();
                var parser = new TSql150Parser(true, SqlEngineType.All);
                var tree = parser.Parse(rdr, out errors);


                if (errors.Count > 0)
                {
                    throw new AggregateException(errors.Select(e => new SyntaxErrorException($"{e.Message} Line {e.Line},{e.Column}")));
                }
                var p = new StatementVisitor();
                tree.Accept(p);
                return (p.statement,p.Parameters);
            }
        }
    }
      

        public class ParseException:SyntaxErrorException{
            
        }

}
