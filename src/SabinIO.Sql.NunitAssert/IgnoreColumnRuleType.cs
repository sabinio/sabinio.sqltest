using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SabinIO.Sql.NUnitAssert
{
    public struct IgnoreColumnRuleType
    {
        public string objectName; public DataRow expectedRow; public DataRow actualRow; public string column;
    }

}