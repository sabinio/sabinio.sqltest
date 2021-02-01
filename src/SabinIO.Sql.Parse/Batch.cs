using System;
using System.Collections.Generic;
using System.Text;

namespace SabinIO.Sql
{
    public class Batch
    {
        public string statement;
        public bool isProc=false;
        public Dictionary<string, Parameter> parameters;
    }
}
