@echo off
REM Executa a API sem mostrar janela de console
REM Use este arquivo para criar um atalho que executa em segundo plano

cd /d "%~dp0"

REM Executa em nova janela minimizada
start /min "" "Executar-API.bat"

