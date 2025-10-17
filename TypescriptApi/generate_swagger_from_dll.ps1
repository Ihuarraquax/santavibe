dotnet tool restore

Push-Location ./..
dotnet build .\SantaVibe.Backend\SantaVibe.Backend.sln --configuration Release 
if(!$?) { exit 1 }
Pop-Location

Push-Location ..\SantaVibe.Backend\SantaVibe.Api\bin\Release\net9.0
dotnet swagger tofile --output ../../../../../TypescriptApi/dll-swagger.json SantaVibe.Api.dll v1
if(!$?) { 
    Write-Error "FAILED TO GENERATE SWAGGER"
    exit 1 
}
Pop-Location
