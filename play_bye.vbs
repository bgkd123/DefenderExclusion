' ============================================
'  Play bye.wav hidden — Portable
'  Created by Sanil
' ============================================

Set objShell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

strScriptDir = fso.GetParentFolderName(WScript.ScriptFullName)
strWav = strScriptDir & "\bye.wav"

If Not fso.FileExists(strWav) Then
    strConfig = strScriptDir & "\wav_path.txt"
    If fso.FileExists(strConfig) Then
        Set objFile = fso.OpenTextFile(strConfig, 1)
        strWav = Trim(objFile.ReadLine)
        objFile.Close
    End If
End If

If fso.FileExists(strWav) Then
    objShell.Run "powershell.exe -WindowStyle Hidden -Command ""(New-Object Media.SoundPlayer '" & strWav & "').PlaySync()""", 0, False
End If