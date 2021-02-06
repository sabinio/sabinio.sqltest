
# Sabin.io SQL library tools

The repo maintains a number of tools for interacting with SQL Server.
All code is built using .net core and thus cross platform.

The following tools produce from the repo.

|Tool|Description|Download|
|-|-|-|
|[XEvent App](docs/XEventApp.md)|The xEvent app is for bulk loading extended event trace files (xel) into a SQL Server table.<br>You can map fields from the file to columns in the target table by using the columns option. |![GitHub release by date][sqlbadge]<br>![GitHub release by date including pre-releases)][sqltest.badge]
|[SabinIO.Sql.Parse](docs/SabinIO.Sql.Parse.md)|Parsing SQL to generate workloads|![Nuget][Sql.Parse.Nuget.BadgePre]<br>![Nuget][Sql.Parse.Nuget.Badge]<br>[Nuget][Sql.Parse.Nuget]<br>![Downloads][Sql.Parse.Nuget.down]
|[SabinIO.SqlTest](docs/SabinIO.SqlTest.md)|Utilities to enable testing against SQL Server, capturing workloads|![Nuget][SqlTest.Nuget.BadgePre]<br>![Nuget][SqlTest.Nuget.Badge]<br>[Nuget][SqlTest.Nuget]
|[SabinIO.Sql.NUnitAssert](docs/SabinIO.Sql.NUnitAssert.md)|Assertions for use in data tests|![Nuget][Sql.NUnitAssert.Nuget.BadgePre]<br>![Nuget][Sql.NUnitAssert.Nuget.Badge]<br>[Nuget][Sql.NUnitAssert.Nuget]
|[SabinIO.xEvent.Lib](docs/SabinIO.xEvent.Lib.md)|library for the processing of Extended event data |![Nuget][xEvent.Lib.Nuget.BadgePre]<br>![Nuget][xEvent.Lib.Nuget.Badge]<br>[Nuget][xEvent.Lib.Nuget]


---

## External dependencies
The following are used in the tools
|library|Notes|
|-|-|
|[Microsoft.SqlServer.XEvent.XELite](https://www.nuget.org/packages/Microsoft.SqlServer.XEvent.XELite/)| |
|Dapper||

---
Latest Build Status
[![Build Status](https://dev.azure.com/sabinio/sabin.io%20public/_apis/build/status/sabinio.sabinio.sqltest?branchName=master)](https://dev.azure.com/sabinio/sabin.io%20public/_build/latest?definitionId=263&branchName=master)

Publish Release To GitHub
[![Build Status](https://dev.azure.com/sabinio/sabin.io%20public/_apis/build/status/sabinio.sabinio.sqltest?branchName=master&stageName=PublishToGitHub)](https://dev.azure.com/sabinio/sabin.io%20public/_build/latest?definitionId=263&branchName=master)


[sqltest.badge]:https://img.shields.io/github/v/release/sabinio/sabinio.sqltest&style=for-the-badge
[sqlbadge]:https://img.shields.io/github/v/release/sabinio/sabinio.sqltest&style=for-the-badge

[Sql.Parse.Nuget.down]:https://img.shields.io/nuget/dt/SabinIO.Sql.Parse
[Sql.Parse.Nuget]:https://www.nuget.org/packages/SabinIO.SQL.Parse/
[Sql.Parse.Nuget.Badge]:https://img.shields.io/nuget/v/SabinIO.Sql.Parse?style=for-the-badge
[Sql.Parse.Nuget.BadgePre]:https://img.shields.io/nuget/v/SabinIO.Sql.Parse?include_prereleases&style=for-the-badge

[SqlTest.Nuget]:https://www.nuget.org/packages/SabinIO.SQLTest/
[SqlTest.Nuget.Badge]:https://img.shields.io/nuget/v/SabinIO.SqlTest?style=for-the-badge
[SqlTest.Nuget.BadgePre]:https://img.shields.io/nuget/v/SabinIO.SQLTest?include_prereleases&style=for-the-badge

[Sql.NUnitAssert.Nuget]:https://www.nuget.org/packages/SabinIO.SQLTest/
[Sql.NUnitAssert.Nuget.Badge]:https://img.shields.io/nuget/v/SabinIO.SQLTest?style=for-the-badge
[Sql.NUnitAssert.Nuget.BadgePre]:https://img.shields.io/nuget/v/SabinIO.SQLTest?include_prereleases&style=for-the-badge

[xEvent.Lib.Nuget]:https://www.nuget.org/packages/SabinIO.xEvent.Lib/
[xEvent.Lib.Nuget.Badge]:https://img.shields.io/nuget/v/SabinIO.xEvent.Lib?style=for-the-badge
[xEvent.Lib.Nuget.BadgePre]:https://img.shields.io/nuget/v/SabinIO.xEvent.Lib?include_prereleases&style=for-the-badge
