@echo off
setlocal EnableDelayedExpansion
title Build - Defender Exclusion Manager

echo ============================================
echo   Building DefenderExclusion.exe
echo ============================================
echo.

:: Locate csc.exe
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if not exist "%CSC%" (
    echo [ERROR] csc.exe kandillla.
    pause & exit /b 1
)

echo Compiler: %CSC%
echo.

:: ============================================
:: AUTO-DETECT WPF Reference Assemblies
:: ============================================
set "REFBASE=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework"
set "REFDIR="

if exist "%REFBASE%" (
    for /f "delims=" %%D in ('dir /b /ad /o-n "%REFBASE%" 2^>nul') do (
        if not defined REFDIR (
            if exist "%REFBASE%\%%D\PresentationCore.dll" (
                set "REFDIR=%REFBASE%\%%D"
            )
        )
    )
)

:: Fallback to WPF runtime
if not defined REFDIR (
    if exist "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\PresentationCore.dll" (
        set "REFDIR=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF"
    )
)

if not defined REFDIR (
    echo [ERROR] WPF DLLs kandillla.
    echo .NET Framework 4.8.1 Developer Pack install cheyyuka.
    pause & exit /b 1
)

echo WPF Reference: !REFDIR!
echo.

:: Icon check
set "ICON_OPT="
if exist "icon.ico" (
    set "ICON_OPT=/win32icon:icon.ico"
    echo Icon: icon.ico (found)
)
echo.

:: Build
"%CSC%" /target:winexe ^
        /out:DefenderExclusion.exe ^
        /win32manifest:app.manifest ^
        !ICON_OPT! ^
        /optimize+ ^
        /reference:System.Windows.Forms.dll ^
        /reference:System.Drawing.dll ^
        /reference:"!REFDIR!\PresentationCore.dll" ^
        /reference:"!REFDIR!\PresentationFramework.dll" ^
        /reference:"!REFDIR!\WindowsBase.dll" ^
        /reference:"!REFDIR!\System.Xaml.dll" ^
        /reference:"!REFDIR!\WindowsFormsIntegration.dll" ^
        DefenderExclusion.cs

if %errorlevel%==0 (
    echo.
    echo ============================================
    echo   [OK] Build success!
    echo   File: DefenderExclusion.exe
    echo ============================================
) else (
    echo.
    echo [FAIL] Build failed.
)

echo.
pause
endlocal