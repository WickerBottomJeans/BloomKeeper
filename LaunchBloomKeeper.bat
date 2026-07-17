@echo off
for %%I in ("%~dp0.") do set "PROJECT_DIR=%%~fI"
set "SOLUTION_PATH=%PROJECT_DIR%\BloomKeeper.sln"
set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.4.2f1\Editor\Unity.exe"
set "RIDER_EXE=C:\Program Files\JetBrains\JetBrains Rider 2026.1.0.1\bin\rider64.exe"
set "CODEX_APP=shell:AppsFolder\OpenAI.Codex_2p2nqsd0c76g0!App"

start "BloomKeeper Unity" "%UNITY_EXE%" -projectPath "%PROJECT_DIR%"
start "BloomKeeper Rider" "%RIDER_EXE%" "%SOLUTION_PATH%"
start "Codex" explorer.exe "%CODEX_APP%"
