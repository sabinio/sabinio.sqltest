using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static SabinIO.Sql.Parse;

namespace SabinIO.Sql
{

    public class StatementVisitor: TSqlFragmentVisitor
    {
        public string statement;
        public Dictionary<string,Parameter> Parameters= new Dictionary<String, Parameter>();
        public bool isProc = false;
        readonly TSqlParser _parser;
        public StatementVisitor(TSqlParser parser)
        {
            _parser = parser;
        }
        public override void Visit(ExecuteStatement node)
        {

            if (node.ExecuteSpecification.ExecutableEntity is ExecutableProcedureReference p)
            {
                var s = p.ProcedureReference.ProcedureReference.Name.BaseIdentifier.Value;
                if (s == "sp_executesql")
                {
                    GetSpExecuteSQL(node);

                }
                else
                {
                    isProc = true;
                    GetProcExecute(node);
                }
            }
            else
            {
                throw new Exception("something wrong");
            }

            base.Visit(node);
        }

        private void GetSpExecuteSQL(ExecuteStatement node)
        {
            for (int i = 0; i < node.ExecuteSpecification.ExecutableEntity.Parameters.Count; i++)
            {
                var execparam = node.ExecuteSpecification.ExecutableEntity.Parameters[i];
                switch (i)
                {
                    case 0:
                        statement = (execparam.ParameterValue as StringLiteral).Value;
                        break;
                    case 1:
                        var paramdef = (execparam.ParameterValue as StringLiteral).Value;

                        foreach (var pdef in paramdef.Split(",@"))
                        {
                            var pdefParts = pdef.Split(" ");
                            string sname = pdefParts[0];
                            if (sname[0] != '@') { sname = "@" + sname; }
                            Parameters.Add(sname, new Parameter() { Name =sname, FullType = pdefParts[1] });

                            
                            IList<ParseError> errors= new List<ParseError>();
                            var paramDataType = _parser.ParseScalarDataType(new StringReader(pdefParts[1]),out errors);
                            if (errors.Count > 0)
                            {
                                throw ParseException.CreateSingleOrAggregate(errors);
                            }
                            else
                            {
                                switch (paramDataType)
                                {
                                    case SqlDataTypeReference dt:
                                        Parameters[sname].Type = dt.Name.Identifiers[0].Value;
                                        if(dt.Parameters.Count > 0)
                                        {
                                            Parameters[sname].length = dt.Parameters[0].Value switch
                                            {
                                                "max" => -1,
                                                _ => Int32.Parse(dt.Parameters[0].Value),
                                            };
                                        }
                                        if (dt.Parameters.Count > 1) Parameters[sname].Scale = byte.Parse(dt.Parameters[1].Value);

                                        break;
                                    case UserDataTypeReference udt:
                                        Parameters[sname].Type = udt.Name.BaseIdentifier.Value;
                                        break;
                                }
                            }
                        }
                        break;
                    default:
                        switch (execparam.ParameterValue)
                        {
                            case StringLiteral stringvalue:
                                Parameters[execparam.Variable.Name].Value = stringvalue.Value;
                                break;
                            case IntegerLiteral intValue:
                                Parameters[execparam.Variable.Name].Value = intValue.Value;
                                break;
                            case VariableReference exp:
                                Parameters[execparam.Variable.Name].Value = exp.Name;
                                break;
                            default:
                                break;
                        }
                        break;
                }
            }
        }

        private void GetProcExecute(ExecuteStatement node)
        {
            var proc = (node.ExecuteSpecification.ExecutableEntity as ExecutableProcedureReference);
            statement = String.Join('.', proc.ProcedureReference.ProcedureReference.Name.Identifiers.Select(c=>c.Value));
            for (int i = 0; i < node.ExecuteSpecification.ExecutableEntity.Parameters.Count; i++)
            {
                var execparam = node.ExecuteSpecification.ExecutableEntity.Parameters[i];
                switch (i)
                {

                    default:
                        Parameters.Add(execparam.Variable.Name, new Parameter() { Name = execparam.Variable.Name, isOutput = execparam.IsOutput });
                        var param = Parameters[execparam.Variable.Name];
                        SetParam(execparam.ParameterValue, param);
                        break;


                }


            }
        }

        private static void SetParam(ScalarExpression scalarExp, Parameter param)
        {
            switch (scalarExp)
            {
                case StringLiteral stringvalue:
                    param.Value = stringvalue.Value;
                    param.Type = "varchar";
                    param.length = 200;
                    //need to look up type from the database
                    //need to check for dates
                    break;
                case IntegerLiteral intValue:
                    param.Value = intValue.Value;
                    param.Type = "int";
                    break;
                case VariableReference exp:
                    param.Value = exp.Name;
                    param.Type = "variable";
                    break;
                case NullLiteral _:
                    param.Type = "null";
                    break;
                case BinaryLiteral exp:
                    param.Value = exp.Value;
                    param.Type = "binary";
                    break;
                case NumericLiteral exp:
                    param.Value = exp.Value;
                    param.Type = "numeric";
                    break;
                case UnaryExpression exp:
                    
                    SetParam(exp.Expression, param);
                    //     param.Value = exp.Value;
                    param.Value = (exp.UnaryExpressionType == UnaryExpressionType.Negative ? "-" : "") + param.Value;
                    break;
                case DefaultLiteral def:
                    param.Type = "default";
                        break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
