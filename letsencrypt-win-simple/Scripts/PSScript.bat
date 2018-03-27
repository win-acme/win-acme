REM Generic batch script to execute powershell scripts along with positional parameters
REM Pass path to PS1 script as well as any parameters you want to pass
REM Spaces are not supported in script path
REM Also, in testing, powershell script must use positional parameters. Results may vary
REM Ex. "./Scripts/PSScript.bat c:\scripts\test.ps1 value"

powershell.exe -ExecutionPolicy RemoteSigned -File %*
