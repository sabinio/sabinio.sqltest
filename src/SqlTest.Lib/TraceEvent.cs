using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.IO;

namespace SqlTest.Lib
{
    public class TraceEvent
    {
#pragma warning disable IDE1006 // Naming Styles
        public long cpu_time { get; set; }
        public long database_id { get; set; }


        public int source_database_id { get; set; }
        public int object_id { get; set; }
        public int object_type { get; set; }//enum
        public int state { get; set; } //enum
        public int nest_level { get; set; }
        public int line_number { get; set; }
        public int offset { get; set; }
        public int offset_end { get; set; }
        public string object_name { get; set; }
        public string statement { get; set; }
        public string database_name { get; set; }
        public long num_response_rows { get; set; }
        public string plan_handle { get; set; }
        public long query_hash { get; set; }
        public long query_plan_hash { get; set; }
        public string server_instance_name { get; set; }
        public int session_id { get; set; }
        public string batch_text { get; set; }
        public string sql_text { get; set; }
        public string context_info { get; set; }
        public long duration { get; set; }
        public long page_server_reads { get; set; }
        public long physical_reads { get; set; }
        public long logical_reads { get; set; }
        public long writes { get; set; }
        public long spills { get; set; }
        public long row_count { get; set; }
        public long last_row_count { get; set; }
        public string parameterized_plan_handle { get; set; }
        public int result { get; set; } //enum
        public int cpu_id { get; set; }

        public long task_time { get; set; }
        public long transaction_sequence { get; set; }



        public Guid attach_activity_guid;
        public int attach_activity_sequence;

        public string eventName { get; set; }
#pragma warning restore IDE1006 // Naming Styles

        public static IEnumerable<TraceEvent> LoadFromStream(XmlReader eventList, TextWriter Log)
        {
            var props = typeof(TraceEvent).GetProperties().ToDictionary(p=>p.Name);

            
            XElement eventXml = XElement.Load(eventList);
            
            foreach (var eventEntry in eventXml.Descendants("event"))
            {
                var thisEvent = new TraceEvent() { eventName = eventEntry.Attribute("name").Value };
                
                foreach (var action in eventEntry.Elements())
                {
                    string ActionName = action.Attribute("name").Value;
                    var actionValue = action.Element("value").Value;
                    switch (ActionName)
                    {
                        case "attach_activity_id":

                            thisEvent.attach_activity_guid = new Guid(actionValue[0..36]);
                            thisEvent.attach_activity_sequence = int.Parse(actionValue[37..]);
                            break;
                        default:
                            if (props.ContainsKey(ActionName))
                            {

                                switch (props[ActionName].PropertyType.Name)
                                {
                                    case nameof(Int64):
                                        props[ActionName].SetValue(thisEvent, Int64.Parse(actionValue), null);
                                        break;
                                    case nameof(Int32):
                                        props[ActionName].SetValue(thisEvent, Int32.Parse(actionValue), null);
                                        break;
                                    default:
                                        props[ActionName].SetValue(thisEvent, actionValue, null);
                                        break;


                                }

                            }
                            else
                            {
                                if (Log != null)
                                {
                                    Log.WriteLine("public {1} {0} {{get;set;}}",ActionName,action.Element("type").Attribute("name").Value);
                                }
                            }
                            break;
                    }
                        
                }
                yield return thisEvent;
            }
    
        }
    }

}
