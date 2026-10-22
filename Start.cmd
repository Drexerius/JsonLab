@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo JSON Lab: http://localhost:5196
echo Demo account: student / JsonLab2026!
echo Stop: Ctrl+C
dotnet run --project src\JsonLab.Web --launch-profile http -- --SeedDemo=true
pause
