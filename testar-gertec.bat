@echo off
echo ========================================
echo Testando conexao com Gertec
echo IP: 192.168.1.57
echo Porta: 6500
echo ========================================
echo.

echo [1] Testando PING...
ping -n 4 192.168.1.57
echo.

echo [2] Testando PORTA 6500 (TCP)...
powershell -Command "Test-NetConnection -ComputerName 192.168.1.57 -Port 6500"
echo.

echo [3] Verificando tabela ARP...
arp -a | findstr 192.168.1.57
echo.

echo ========================================
echo Teste concluido!
echo ========================================
pause

