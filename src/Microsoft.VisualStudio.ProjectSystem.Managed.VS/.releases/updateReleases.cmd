@ECHO OFF

SETLOCAL ENABLEDELAYEDEXPANSION

REM Download the index
ECHO Downloading releases.json for version %%V...
curl -o %~dp0.releases.json https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json 
ECHO Saved as %~dp0.releases.json

REM Define the base URL
SET BASE_URL=https://builds.dotnet.microsoft.com/dotnet/release-metadata

REM Define the versions to download, these should all be present in the releases-index.json
REM Note, we are assuming a consistent BASE_URL for each release metadata, this *could* change
SET VERSIONS=5.0 6.0 7.0 8.0 9.0 10.0

REM Loop through each version and download the corresponding releases.json
FOR %%V IN (%VERSIONS%) DO (
    SET FILENAME=%~dp0%%V.releases.json
    ECHO Downloading releases.json for version %%V...
    curl -o "!FILENAME!" "!BASE_URL!/%%V/releases.json"
    ECHO Saved as !FILENAME!
)

ENDLOCAL