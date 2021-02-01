using Microsoft.Data.SqlClient.Server;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using static SabinIO.Sql.Parse;

namespace SabinIO.Sql
{

    public class ParamVisitor: TSqlFragmentVisitor
    {
        public readonly Dictionary<string,TVPParameter> Parameters = new Dictionary<String, TVPParameter>();

        public override void Visit(TSqlStatement node)
        {

            DeclareTableVariableStatement t;
            switch (node)
            {
                case InsertStatement insStmt:
                    var x = insStmt.InsertSpecification;
                    if (x.Target is VariableTableReference y)
                    {
                        string VariableName = y.Variable.Name;

                        if (!Parameters.ContainsKey(VariableName))
                        {
                            Parameters.Add(VariableName, new TVPParameter() { Name = VariableName });
                        }
                        if (x.InsertSource is ValuesInsertSource insertStatement)
                        {
                            //only work out types on the first pass
                            if (Parameters[VariableName].Types.Count == 0)
                            {

                                foreach (var c in insertStatement.RowValues[0].ColumnValues)
                                {
                                    switch (c)
                                    {
                                        case StringLiteral L:
                                            Parameters[VariableName].Types.Add(L.LiteralType.ToString());
                                            Parameters[VariableName].RowValues.Columns.Add().DataType = typeof(string);
                                            break;
                                        case IntegerLiteral L:
                                            Parameters[VariableName].Types.Add(L.LiteralType.ToString());
                                            Parameters[VariableName].RowValues.Columns.Add().DataType = typeof(int);
                                            break;
                                        case BinaryLiteral L:
                                            Parameters[VariableName].Types.Add(L.LiteralType.ToString());
                                            Parameters[VariableName].RowValues.Columns.Add().DataType = typeof(byte);
                                            break;
                                        default:
                                            Parameters[VariableName].Types.Add("unknown");
                                            Parameters[VariableName].RowValues.Columns.Add().DataType = typeof(object);
                                            break;

                                    }
                                }
                            }

                            foreach (var row in insertStatement.RowValues)
                            {

                                var tableRow = Parameters[VariableName].RowValues.NewRow();

                                for (int i = 0; i < row.ColumnValues.Count; i++)
                                {
                                    tableRow[i] = (row.ColumnValues[i] as Literal).Value;
                                }
                                Parameters[VariableName].RowValues.Rows.Add(tableRow);
                            }
                        }
                    }
                    break;
                case DeclareVariableStatement decl:
                    foreach (var d in decl.Declarations)
                    {
                        //only TVPs have a schema identifier???
                        if (d.DataType.Name.SchemaIdentifier!=null)
                        {
                            var vname = d.VariableName.Value;
                            if (!Parameters.ContainsKey(vname))
                            {
                                Parameters.Add(vname, new TVPParameter() { Name = vname });
                            }
                            Parameters[vname].Type = String.Join('.', d.DataType.Name.Identifiers.Select(s => s.Value));
                        }
                    }
                    break;
            }
            base.Visit(node);
        }
    }
}
