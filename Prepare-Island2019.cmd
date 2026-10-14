@echo off
setlocal EnableExtensions DisableDelayedExpansion
title BoatAttack - Prepare and ZIP the clean Unity 2019 island
echo Prepare a separate Unity 2019.3.0f6 island project and ZIP.
echo Your Unity 2020 project will not be changed.
echo No administrator rights or Unity 2019 installation are needed for this step.
echo.
set "gitExe="
for /f "delims=" %%G in ('where git.exe 2^>nul') do if not defined gitExe set "gitExe=%%G"
if defined gitExe goto gitReady
for /d %%D in ("%LOCALAPPDATA%\GitHubDesktop\app-*") do if exist "%%D\resources\app\git\cmd\git.exe" set "gitExe=%%D\resources\app\git\cmd\git.exe"
if defined gitExe goto gitReady
for /d %%D in ("%LOCALAPPDATA%\GitHubDesktop\app-*") do if exist "%%D\resources\app\git\mingw64\bin\git.exe" set "gitExe=%%D\resources\app\git\mingw64\bin\git.exe"
if not defined gitExe goto missingGit
:gitReady
for %%G in ("%gitExe%") do set "gitBin=%%~dpG"
set "PATH=%gitBin%;%gitBin%..\mingw64\bin;%PATH%"
"%gitExe%" lfs version >nul 2>&1
if errorlevel 1 goto missingLfs
set "outputParent=%USERPROFILE%\Downloads"
if exist "D:\" set "outputParent=D:"
if not exist "%outputParent%" goto missingFolder
set "projectPath=%outputParent%\BoatAttack-Island2019-%RANDOM%-%RANDOM%"
if exist "%projectPath%" goto collision
set "zipPath=%projectPath%.zip"
if exist "%zipPath%" goto collision
echo New project: "%projectPath%"
echo ZIP to share: "%zipPath%"
echo Keep this window open until DONE or ERROR appears.
echo.
set "GIT_LFS_SKIP_SMUDGE=1"
"%gitExe%" clone --single-branch --no-checkout --branch island-clean-2019.3 https://github.com/huagleesure22-lang/BoatAttack.git "%projectPath%"
if errorlevel 1 goto failed
if not exist "D:\github\.git\lfs\objects" goto cacheCopied
echo Reusing previously downloaded LFS objects...
robocopy "D:\github\.git\lfs\objects" "%projectPath%\.git\lfs\objects" /E /R:1 /W:1 /NFL /NDL /NJH /NJS >nul
if errorlevel 8 goto failed
:cacheCopied
"%gitExe%" -C "%projectPath%" lfs install --local
if errorlevel 1 goto failed
"%gitExe%" -C "%projectPath%" checkout island-clean-2019.3
if errorlevel 1 goto failed
set "GIT_LFS_SKIP_SMUDGE="
"%gitExe%" -C "%projectPath%" lfs checkout
if errorlevel 1 goto failed
echo Downloading any missing 2019 model and texture files...
"%gitExe%" -C "%projectPath%" lfs pull
if errorlevel 1 goto failed
"%gitExe%" -C "%projectPath%" lfs fsck --objects --pointers
if errorlevel 1 goto failed
if not exist "%projectPath%\Assets\scenes\island_clean_2019.unity" goto failed
if not exist "%projectPath%\Assets\Island2019\Island_WithoutBoats.prefab" goto failed
set "versionOK="
for /f "usebackq tokens=1,2" %%A in ("%projectPath%\ProjectSettings\ProjectVersion.txt") do if "%%A"=="m_EditorVersion:" if "%%B"=="2019.3.0f6" set "versionOK=1"
if not defined versionOK goto badVersion

echo.
echo Creating ZIP. This can take a few minutes; keep this window open.
set "ISLAND2019_PATH=%projectPath%"
set "ISLAND2019_ZIP=%zipPath%"
powershell.exe -NoProfile -Command "$ErrorActionPreference='Stop'; $root=$env:ISLAND2019_PATH; $items=@('Assets','Packages','ProjectSettings','ISLAND2019.md','LICENSE.md') | ForEach-Object { Join-Path $root $_ }; Compress-Archive -LiteralPath $items -DestinationPath $env:ISLAND2019_ZIP -CompressionLevel Optimal"
if errorlevel 1 goto zipFailed
if not exist "%zipPath%" goto zipFailed
echo.
echo DONE - Download, LFS verification and ZIP creation completed.
echo Send this ZIP to your classmate:
echo "%zipPath%"
echo Use Unity 2019.3.0f6 and open Assets/scenes/island_clean_2019.unity.
echo Unity import and rendering still need verification on that computer.
pause
exit /b 0
:missingGit
echo ERROR - Git was not found. Open GitHub Desktop once and retry.
goto stop
:missingLfs
echo ERROR - Git LFS was not found. Use GitHub Desktop's bundled Git.
goto stop
:missingFolder
echo ERROR - No writable output folder was found.
goto stop
:collision
echo ERROR - Generated destination already exists. Run this tool again.
goto stop
:badVersion
echo ERROR - Unexpected project version. Do not open this copy in Unity 2020.
type "%projectPath%\ProjectSettings\ProjectVersion.txt"
goto stop
:zipFailed
echo ERROR - The verified project was downloaded, but ZIP creation failed.
echo Send a screenshot of this window. The original Unity 2020 project is intact.
echo Project folder: "%projectPath%"
goto stop
:failed
echo ERROR - Preparation stopped. Do not share the incomplete new project.
echo The original Unity 2020 project is intact. Send a screenshot of this window.
:stop
pause
exit /b 1
