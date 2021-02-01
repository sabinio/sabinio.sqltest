using System;
using System.Collections.Generic;
using System.Text;

namespace SabinIO.Sql
{
    public class Parameter
    {
        public string Name;
        public string FullType;
        public string Type;
        public string Value;
        public int length;
        public byte Scale;
        public bool isOutput;
    }
}
