$root = (split-path -parent $MyInvocation.MyCommand.Definition) + '\..'
$version = [System.Reflection.Assembly]::LoadFile("$root\\AzureStorageTableExternalLoginStore\bin\Release\UmbracoIdentity.AzureLoginStore.dll").GetName().Version
$versionStr = "{0}.{1}.{2}" -f ($version.Major, $version.Minor, $version.Build)

Write-Host "Setting .nuspec version tag to $versionStr"

$content = (Get-Content $root\build\Package.nuspec) 
$content = $content -replace '\$version\$',$versionStr

$content | Out-File $root\build\UmbracoIdentity.AzureLoginStore.compiled.nuspec

& $root\build\NuGet.exe pack $root\build\UmbracoIdentity.AzureLoginStore.compiled.nuspec