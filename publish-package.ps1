if (Test-Path "packages") {

	Remove-Item -Recurse -Force "packages"
}
 
dotnet restore
dotnet build src\RenderRazor\RenderRazor.csproj

dotnet pack src\RenderRazor\RenderRazor.csproj -c release -o ./../../packages --include-symbols --include-source

$packageName = (dir packages | where { $_.Name -match '^RenderRazor\.\d+\.\d+\.\d+\.nupkg$' } | select -first 1).Name

nuget push -ApiKey $env:nuget_key -source https://api.nuget.org/v3/index.json packages/$packageName

pause