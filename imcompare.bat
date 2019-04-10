@echo off
setlocal

set IMG1=%~1
set IMG2=%~2
set THR=%~3

for /F "tokens=2 delims=() " %%A in ( 'magick compare -metric RMSE "%IMG1%" "%IMG2%" null: 2^>^&1' ) do (
	if %%A LSS %THR% (
		echo Similar
		exit /b 0
	) else (
		echo Different
		exit /b 1
	)
)