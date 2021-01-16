# XEvent App
The xEvent app is for bulk loading extended event trace files (xel) into a SQL Server table.

You can map fields from the file to columns in the target table by using the columns option. 

---
Latest Build Status
[![Build Status](https://dev.azure.com/sabinio/sabin.io%20public/_apis/build/status/sabinio.sabinio.sqltest?branchName=master)](https://dev.azure.com/sabinio/sabin.io%20public/_build/latest?definitionId=263&branchName=master)

Publish Release To GitHub
[![Build Status](https://dev.azure.com/sabinio/sabin.io%20public/_apis/build/status/sabinio.sabinio.sqltest?branchName=master&stageName=PublishToGitHub)](https://dev.azure.com/sabinio/sabin.io%20public/_build/latest?definitionId=263&branchName=master)

---
## Usage


```
xEventApp --tablename string --connection string --filename string --batchsize int --fields string|{constant}[ ..n] --columns string[ ..n] --progress int 

string = a string value
int    = an integer value
|      = choice of different type of arguments
[ ..n] = repeating argument i.e. --fields cpu_time page_faults session_id 
```

|-|option|input|description|default|
|-|-|:-|:-|:-|
|#|--tablename|`string`|Name of table load trace into||
|#|--connection|`string`|Connection string of database||
|#|--filename|`string`|Extended event filename full path or relative to the exe||
||--batchsize|`int`|Size of batches sent for each bulk insert for loading clustered column store tables keep this above `102,400` |`1000000`|
|#|--fields|`string|{constant}[ ..n]`|Names of fields to load from extended events (either fields or actions) or a constant. Constants are defined by wrapping value in {}.<br> The fields are separated by spaces|
||--columns|`string[ ..n]`|names of columns in the target table to load, order should map to the order of the fields specified||
||--logFile|`string`|name of log file||
||--debug|`false\|true`|outputs debug information to the standard out, intenal use only|`false`|
||--logLevel|`-1\|0\|1\|2\|3\|4\|5`|outputs debug information to the standard out||
||--progress|`int`|how many rows to be moved before notifying of progress|`1000000`|
||--help||displays help output||

