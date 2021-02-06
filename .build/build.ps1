

Push-Location 
try{

    &dotnet build .\src\SqlTest.sln 2>&1
}
finally{
    pop-location
}