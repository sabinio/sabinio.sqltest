using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Text;
using static SabinIO.Sql.Parse;

namespace SabinIO.Sql
{

    public class StatementVisitor: TSqlFragmentVisitor
    {
        public string statement;
        public Dictionary<string,Parameter> Parameters= new Dictionary<String, Parameter>();


        public override void Visit(ExecuteStatement node)
        {

            if (node.ExecuteSpecification.ExecutableEntity is ExecutableProcedureReference p)
            {
                var s = p.ProcedureReference.ProcedureReference.Name.BaseIdentifier.Value;
                if (s == "sp_executesql")
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

                                foreach (var pdef in paramdef.Split(","))
                                {
                                    var pdefParts = pdef.Split(" ");
                                    Parameters.Add(pdefParts[0], new Parameter() { Name = pdefParts[0], Type=pdefParts[1]  } );
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
                    var execParams = node.ExecuteSpecification.ExecutableEntity.Parameters;
                  

                }
                else
                {
                    throw new Exception("should only be for sp_executesql");
                }
            }
            else
            {
                throw new Exception("something wrong");
            }

            base.Visit(node);
        }
    }
}
