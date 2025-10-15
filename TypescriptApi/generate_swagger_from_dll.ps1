dotnet tool restore

Push-Location ./..
dotnet build .\SantaVibe.Api\SantaVibe.sln --configuration Release 
if(!$?) { exit 1 }
Pop-Location

Push-Location ..\SantaVibe.Api\bin\Release\net9.0
dotnet swagger tofile --output ../../../../TypescriptApi/dll-swagger.json SantaVibe.Api.dll api
if(!$?) { 
    Write-Error "FAILED TO GENERATE SWAGGER"
    exit 1 
}
Pop-Location
