
param ($rootpath, $outpath, $configuration="debug")

Push-Location $rootpath
try{

    foreach($lib in "SabinIO.Sql.Parse","SabinIO.SqlTest","SabinIO.xEvent.Lib"){
        &dotnet pack "src\$lib" --configuration $configuration --no-build  -o (join-path $outpath $lib) 2>&1
    }
}
finally{
    pop-location
}