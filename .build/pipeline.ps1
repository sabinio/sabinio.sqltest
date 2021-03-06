[CmdletBinding()]
param
($Configuration="debug"
,$apiKey
,$outpath = "./out")
DynamicParam {          
    $RuntimeParamDic = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary
    $AttribColl = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
    $ParamAttrib = New-Object System.Management.Automation.ParameterAttribute
    $ParamAttrib.ParameterSetName = '__AllParameterSets'
    $AttribColl.Add($ParamAttrib)
    
    $files = Get-ChildItem $psscriptroot -Filter *.ps1 
    
    foreach ($file in $files) { 
        $switchName = $file.BaseName
        $RuntimeParam = New-Object System.Management.Automation.RuntimeDefinedParameter($switchname, [switch], $AttribColl)
        $RuntimeParamDic.Add($switchname, $RuntimeParam)
    }
    return  $RuntimeParamDic
}
process {

    if ($psboundparameters["build"]) { & $psscriptroot\build.ps1  -Configuration $Configuration }

    if ($psboundparameters["package"]){ & $psscriptroot\package.ps1 -Configuration $Configuration    -outpath $outpath }

    if ($psboundparameters["test"]){ & $psscriptroot\test.ps1   }

    if ($psboundparameters["publish"]){ & $psscriptroot\publish.ps1 -Configuration $Configuration  -outpath $outpath  -apikey $apikey}

}
