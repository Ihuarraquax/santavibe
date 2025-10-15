$destination_folder = './approved-openapi-generator-api/'
$swagger_input = 'dll-swagger.json'

if (Test-Path $swagger_input) 
{
  Remove-Item $swagger_input
}

npm install @openapitools/openapi-generator-cli@2.5.1
& "./generate_swagger_from_dll.ps1"
if(!$?) {
    Write-Error "Generation of swagger.json from dll failed"
    exit 1;
}

Remove-Item -Path $destination_folder -Recurse -ErrorAction Ignore

npx openapi-generator-cli generate -i $swagger_input -g typescript-angular -o $destination_folder -c config.json
if(!$?) { exit 1 }

$readme = Get-Content '.\approved-openapi-generator-api\README.md'
Set-Content '.\approved-openapi-generator-api\README.md' ($readme)