using System;
using System.Collections.Generic;
using System.Text;

namespace SabinIO.xEvent.Lib
{
    public class InvalidFieldException : Exception
    {
     public InvalidFieldException(string fieldname):base($"The field {fieldname} specified doesn't exist in the extended event file")
        {
        } 
    }
}
