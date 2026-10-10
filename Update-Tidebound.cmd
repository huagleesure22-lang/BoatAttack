@echo off
setlocal EnableExtensions DisableDelayedExpansion
title BoatAttack - Update the Unity 2020 adventure
echo Close the Unity editor before updating this project.
echo This tool fetches the published branch and only allows a fast-forward update.
echo Local edits will not be discarded.
echo.
set "projectPath=%~dp0"
if not exist "%projectPath%\Assets\Tidebound" set "projectPath=D:\github-Unity2020-28206-10141"
if exist "%projectPath%\Assets\Tidebound" goto projectReady
echo Paste your CLEAN Unity 2020 project folder, without surrounding quotes.
set /p "projectPath=Project folder: "
:projectReady
if not exist "%projectPath%\.git" goto missingProject
if not exist "%projectPath%\Assets\Tidebound" goto missingProject
set "gitExe="
for /f "delims=" %%G in ('where git.exe 2^>nul') do if not defined gitExe set "gitExe=%%G"
if defined gitExe goto gitReady
for /d %%D in ("%LOCALAPPDATA%\GitHubDesktop\app-*") do if exist "%%D\resources\app\git\cmd\git.exe" set "gitExe=%%D\resources\app\git\cmd\git.exe"
if defined gitExe goto gitReady
for /d %%D in ("%LOCALAPPDATA%\GitHubDesktop\app-*") do if exist "%%D\resources\app\git\mingw64\bin\git.exe" set "gitExe=%%D\resources\app\git\mingw64\bin\git.exe"
if not defined gitExe goto missingGit
:gitReady
echo Updating "%projectPath%"
"%gitExe%" -C "%projectPath%" fetch https://github.com/huagleesure22-lang/BoatAttack.git tidebound-island
if errorlevel 1 goto failed
"%gitExe%" -C "%projectPath%" merge --ff-only FETCH_HEAD
if errorlevel 1 goto failed
echo.
echo DONE - Latest adventure changes downloaded.
echo Open "%projectPath%" in Unity Hub with Unity 2020.3.23f1.
echo Then use Tidebound / Open Island Adventure and press Play.
pause
exit /b 0
:missingProject
echo ERROR - This folder is not a Git clone containing Assets/Tidebound.
goto stop
:missingGit
echo ERROR - Git was not found. Open GitHub Desktop once and retry.
goto stop
:failed
echo ERROR - Update stopped. Local changes were not discarded.
echo Send a screenshot of this window. Do not reset or delete your project.
:stop
pause
exit /b 1
