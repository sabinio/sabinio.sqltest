

param ($rootpath, $outpath, $configuration="debug")

Push-Location 
try{

    &dotnet build .\src\SqlTest.sln --configuration $configuration 2>&1
}
finally{
    pop-location
}