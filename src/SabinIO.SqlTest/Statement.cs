using System;
using System.Collections.Generic;
using System.Text;

namespace SabinIO.SqlTest
{
    public class Statement
    {
        public string name;
        public readonly IDictionary<string, string> Fields = new Dictionary<string, string>();
        public  IDictionary<string, string> fields { get { return Fields; } }
        public DateTime Timestamp;

        public Statement (string name, DateTime timestamp, IDictionary<string,string> fields)
        {
            this.name = name;
            Fields = fields;
            Timestamp = timestamp;
        }
        public int Length => Fields.Count;

        public string this[string  key] // Indexer declaration
        {
            get { return Fields.TryGetValue(key, out string value) ? value : ""; }
            set { Fields[key] = value; }
            // get and set accessors
        }
        public void Add(string key, string value) // Indexer declaration
        {
            Fields.Add(key, value);        }
    }
}
