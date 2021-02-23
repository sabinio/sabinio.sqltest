
param ($rootpath, $outpath, $apikey)

Push-Location $rootpath
try{

    foreach($lib in "SabinIO.Sql.Parse","SabinIO.SqlTest","SabinIO.xEvent.Lib"){
      #  &dotnet pack "src\$lib" --no-build  -o "out\$lib" 2>&1
        &dotnet nuget push .\out\$lib\$lib.$($env:version).nupkg -k $apikey -s nuget.org      
    }
}
finally{
    pop-location
}