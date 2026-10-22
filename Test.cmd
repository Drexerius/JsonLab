@echo off
cd /d "%~dp0"
dotnet test JsonLab.sln --logger "trx;LogFileName=results.trx" --results-directory TestResults
pause
