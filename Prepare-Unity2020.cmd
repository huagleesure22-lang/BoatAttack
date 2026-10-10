@echo off
setlocal EnableExtensions DisableDelayedExpansion
title BoatAttack - Unity 2020 recovery v2
echo BoatAttack Unity 2020 recovery v2 - no administrator rights needed.

rem Create a NEW copy from the published adventure commit.
rem Never restore, clean, reset, or delete files in the original project.
rem Unity packages still need a working connection to Unity's package CDN.
set "baseline=da267c229f35c66ad771bbe094e4b2eeafd00811"
set "sourcePath=%~dp0"
if not exist "%sourcePath%\.git" set "sourcePath=D:\github"
if not exist "%sourcePath%\.git" goto askSource
goto sourceReady

:askSource
echo Enter the folder cloned by GitHub Desktop, without surrounding quotes.
set /p "sourcePath=Project folder: "

:sourceReady
if not exist "%sourcePath%\.git" goto missingSource
for %%D in ("%sourcePath%") do set "sourcePath=%%~fD"
if "%sourcePath:~-1%"=="\" set "sourcePath=%sourcePath:~0,-1%"
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
"%gitExe%" -C "%sourcePath%" cat-file -e "%baseline%^{commit}" >nul 2>&1
if errorlevel 1 goto missingCommit
rem Git show emits LF line endings. FINDSTR /X can reject those on Windows.
rem Read the version as fields instead, accepting both LF and CRLF.
set "versionCheck=%TEMP%\BoatAttack-Version-%RANDOM%-%RANDOM%.txt"
if exist "%versionCheck%" goto targetExists
"%gitExe%" -C "%sourcePath%" show "%baseline%:ProjectSettings/ProjectVersion.txt" > "%versionCheck%"
if errorlevel 1 goto versionReadFailed
set "versionOK="
for /f "usebackq tokens=1,2" %%A in ("%versionCheck%") do if "%%A"=="m_EditorVersion:" if "%%B"=="2020.3.23f1" set "versionOK=1"
if not defined versionOK goto badVersion

set "targetPath=%sourcePath%-Unity2020-%RANDOM%-%RANDOM%"
if exist "%targetPath%" goto targetExists
echo.
echo Original project: "%sourcePath%"
echo New project:      "%targetPath%"
echo The original project and its local edits will be preserved.
echo Keep this window open until DONE or ERROR appears.
echo.

rem Skip smudge during clone so cached LFS objects can be copied first.
set "GIT_LFS_SKIP_SMUDGE=1"
"%gitExe%" clone --no-checkout --local --no-hardlinks "%sourcePath%" "%targetPath%"
if errorlevel 1 goto failed
"%gitExe%" -C "%targetPath%" remote set-url origin https://github.com/huagleesure22-lang/BoatAttack.git
if errorlevel 1 goto failed

rem GitHub Desktop normally keeps LFS objects here. Missing objects are
rem fetched by git lfs pull below; no assets from the working tree are copied.
if not exist "%sourcePath%\.git\lfs\objects" goto cacheCopied
robocopy "%sourcePath%\.git\lfs\objects" "%targetPath%\.git\lfs\objects" /E /R:1 /W:1 /NFL /NDL /NJH /NJS >nul
if errorlevel 8 goto failed

:cacheCopied
"%gitExe%" -C "%targetPath%" lfs install --local
if errorlevel 1 goto failed
"%gitExe%" -C "%targetPath%" checkout -B tidebound-island "%baseline%"
if errorlevel 1 goto failed
set "GIT_LFS_SKIP_SMUDGE="
"%gitExe%" -C "%targetPath%" lfs checkout
if errorlevel 1 goto failed
"%gitExe%" -C "%targetPath%" lfs pull
if errorlevel 1 goto failed
"%gitExe%" -C "%targetPath%" lfs fsck --objects --pointers
if errorlevel 1 goto failed
if not exist "%targetPath%\Assets\scenes\tidebound_Island.unity" goto failed
if exist "%targetPath%\Library" goto failed
"%gitExe%" -C "%targetPath%" diff --quiet "%baseline%" -- Assets Packages ProjectSettings
if errorlevel 1 goto failed

echo.
echo DONE - A clean project copy is ready for Unity to import.
echo This does not mean Unity compilation or Play Mode has passed.
echo.
echo In Unity Hub, import this folder:
echo "%targetPath%"
echo Select Unity 2020.3.23f1. Do not open this copy in Tuanjie.
echo After import, open Assets/scenes/tidebound_Island.unity.
echo If package downloads fail, send the new Editor.log in private chat.
echo.
pause
exit /b 0

:missingSource
echo ERROR - Could not find the original Git clone. Check the folder path.
goto stop
:missingGit
echo ERROR - Git was not found. Open GitHub Desktop once, then retry.
goto stop
:missingLfs
echo ERROR - Git LFS was not found. Use the Git included with GitHub Desktop.
goto stop
:missingCommit
echo ERROR - The published adventure commit is not in this clone yet.
echo In GitHub Desktop, click Fetch origin and select tidebound-island.
echo Then run this tool again. Do not discard local changes.
goto stop
:badVersion
echo ERROR - The source commit does not match Unity 2020.3.23f1.
echo Version read from the source commit:
type "%versionCheck%"
echo Git executable: "%gitExe%"
echo Source folder: "%sourcePath%"
goto stop
:versionReadFailed
echo ERROR - Git could not read ProjectVersion.txt from the published commit.
echo Git executable: "%gitExe%"
echo Source folder: "%sourcePath%"
goto stop
:targetExists
echo ERROR - The generated destination already exists. Run the tool again.
goto stop
:failed
echo.
echo ERROR - Preparation stopped. Do not import the incomplete new copy.
echo Your original project was preserved. Send a screenshot of this window.
:stop
pause
exit /b 1
