

Push-Location 
try{

    foreach($lib in "SabinIO.Sql.Parse.Tests","SabinIO.SqlTest.Tests","SabinIO.xEvent.Lib.Tests"){
        &dotnet test .\src\$lib  -s .\src\test.runsettings --no-build  -r .\out\results 2>&1
    }
    
}
finally{
    pop-location
}