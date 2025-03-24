@echo off
set package_root=..\..\
REM
For /R %package_root% %%G IN (DeployWeb.exe) do (
	IF EXIST "%%G" (set exe_file=%%G
	goto :continue)
	)

:continue
@echo Using '%exe_file%'
REM 

setlocal enabledelayedexpansion
set cmd="%exe_file%" create %CD%

for %%A in (%*) do (
    set cmd=!cmd! %%A
)

!cmd!