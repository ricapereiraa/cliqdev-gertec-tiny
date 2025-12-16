' Script VBScript para executar a API sem mostrar janela de console
' Use este arquivo para criar um atalho que executa sem mostrar a janela preta

Set WshShell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

' Obtém o diretório onde este script está
scriptDir = fso.GetParentFolderName(WScript.ScriptFullName)

' Navega para o diretório e executa o .bat
WshShell.CurrentDirectory = scriptDir
WshShell.Run "Executar-API.bat", 1, False

Set WshShell = Nothing
Set fso = Nothing

