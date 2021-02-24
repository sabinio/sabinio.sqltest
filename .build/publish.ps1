
param ($rootpath, $outpath, $apikey)

Push-Location $rootpath
try{

    foreach($lib in "SabinIO.Sql.Parse","SabinIO.SqlTest","SabinIO.xEvent.Lib"){
      #  &dotnet pack "src\$lib" --no-build  -o "out\$lib" 2>&1
        $package = (join-path $outpath $lib "$lib.$($env:version).nupkg")
        Write-Host "pushing package $package"
        &dotnet nuget push $package -s "nuget.org" -k $apikey     
    }
}
finally{
    pop-location
}