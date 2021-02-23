
param ($rootpath, $outpath, $configuration)

Push-Location $rootpath
try{

    foreach($lib in "SabinIO.Sql.Parse","SabinIO.SqlTest","SabinIO.xEvent.Lib"){
        &dotnet pack "src\$lib" --configuration $configuration --no-build  -o "out\$lib" 2>&1
    }
}
finally{
    pop-location
}