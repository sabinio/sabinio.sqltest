[CmdletBinding()]
param
()
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
  #  $AttribColl.Add((New-Object System.Management.Automation.ValidateSetAttribute(($files | Select-Object -ExpandProperty baseName))));
  #  $RuntimeParam = New-Object System.Management.Automation.RuntimeDefinedParameter("simon", [string], $AttribColl)
  #  $RuntimeParamDic.Add("simon", $RuntimeParam)
    
    return  $RuntimeParamDic
}
process {

    if ($psboundparameters["build"]) { & $psscriptroot\build.ps1   }

    if ($psboundparameters["package"]){ & $psscriptroot\package.ps1   }

    if ($psboundparameters["test"]){ & $psscriptroot\test.ps1   }

}
