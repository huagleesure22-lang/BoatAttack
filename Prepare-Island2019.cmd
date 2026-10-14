@echo off
setlocal EnableExtensions DisableDelayedExpansion
title BoatAttack - Unity 2019 island preparation v2
echo Unity 2019 island preparation v2 - cached Git, shallow fetch, resumable steps.
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
set "projectPath=%outputParent%\BoatAttack-Island2019-Prepared"
set "zipPath=%outputParent%\BoatAttack-Island2019-%RANDOM%-%RANDOM%.zip"
if exist "%zipPath%" goto collision
echo New project: "%projectPath%"
echo ZIP to share: "%zipPath%"
echo Keep this window open until DONE or ERROR appears.
echo.
set "GIT_LFS_SKIP_SMUDGE=1"
if exist "%projectPath%" goto resumeProject
if exist "D:\github\.git" goto cloneCachedGit
"%gitExe%" init "%projectPath%"
if errorlevel 1 goto failed
goto markProject
:cloneCachedGit
echo Step 1: Copying cached Git data from D:\github without modifying it...
"%gitExe%" clone --local --no-hardlinks --no-checkout "D:\github" "%projectPath%"
if errorlevel 1 goto failed
:markProject
> "%projectPath%\.git\island2019-prepare-v2" echo Owned by the Island2019 preparation tool.
if not exist "%projectPath%\.git\island2019-prepare-v2" goto failed
goto configureProject
:resumeProject
if not exist "%projectPath%\.git\island2019-prepare-v2" goto collision
echo Resuming the existing tool-created project; completed downloads are retained.
if not exist "%projectPath%\.git\island2019-checked-out" goto configureProject
"%gitExe%" -C "%projectPath%" diff --quiet
if errorlevel 1 goto localEdits
"%gitExe%" -C "%projectPath%" diff --cached --quiet
if errorlevel 1 goto localEdits
:configureProject
"%gitExe%" -C "%projectPath%" config remote.origin.url https://github.com/huagleesure22-lang/BoatAttack.git
if errorlevel 1 goto failed
"%gitExe%" -C "%projectPath%" config remote.origin.fetch +refs/heads/island-clean-2019.3:refs/remotes/origin/island-clean-2019.3
if errorlevel 1 goto failed
set "fetchDepth=--depth=1"
rem On later runs retain ancestry so fast-forward updates remain possible.
if exist "%projectPath%\.git\island2019-checked-out" set "fetchDepth="
echo Step 2: Fetching only the selected 2019 branch, using HTTP/1.1...
"%gitExe%" -c http.version=HTTP/1.1 -c http.lowSpeedLimit=1024 -c http.lowSpeedTime=60 -C "%projectPath%" fetch %fetchDepth% --no-tags --progress origin refs/heads/island-clean-2019.3:refs/remotes/origin/island-clean-2019.3
if errorlevel 1 goto networkFailed
if not exist "D:\github\.git\lfs\objects" goto cacheCopied
echo Reusing previously downloaded LFS objects...
robocopy "D:\github\.git\lfs\objects" "%projectPath%\.git\lfs\objects" /E /R:1 /W:1 /NFL /NDL /NJH /NJS >nul
if errorlevel 8 goto failed
:cacheCopied
"%gitExe%" -C "%projectPath%" lfs install --local
if errorlevel 1 goto failed
if exist "%projectPath%\.git\island2019-checked-out" goto updateCheckout
"%gitExe%" -C "%projectPath%" checkout -B island-clean-2019.3 origin/island-clean-2019.3
if errorlevel 1 goto failed
> "%projectPath%\.git\island2019-checked-out" echo Checkout completed.
goto checkoutReady
:updateCheckout
"%gitExe%" -C "%projectPath%" merge --ff-only origin/island-clean-2019.3
if errorlevel 1 goto localEdits
:checkoutReady
set "GIT_LFS_SKIP_SMUDGE="
"%gitExe%" -C "%projectPath%" lfs checkout
if errorlevel 1 goto failed
echo Downloading any missing 2019 model and texture files...
"%gitExe%" -c lfs.concurrenttransfers=2 -C "%projectPath%" lfs pull
if errorlevel 1 goto networkFailed
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
echo ERROR - A destination exists that this tool cannot safely reuse.
echo Project folder: "%projectPath%"
echo Send a screenshot. Do not delete or overwrite an existing project.
goto stop
:localEdits
echo ERROR - The prepared copy contains local edits or cannot be fast-forwarded.
echo Your edits were preserved. Send a screenshot; do not reset the project.
goto stop
:networkFailed
echo ERROR - GitHub download connection failed. Completed steps were retained.
echo Try another network, such as a phone hotspot, then run this SAME v2 tool again.
echo It will resume the tool-created folder, not clone your Unity 2020 project again.
echo Do not share a ZIP until DONE appears.
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
