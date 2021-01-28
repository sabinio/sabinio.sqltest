using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SabinIO.Sql
{
    public class TVPParameter
    {
        public string Name;
        public List<string> Types = new List<string>();
        public DataTable RowValues = new DataTable();
    }
}
