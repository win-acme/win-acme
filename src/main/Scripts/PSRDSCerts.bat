REM Batch script to run each RDS import script sequentially
REM Spaces are not supported in script path
REM Must pass the thumbprint as the only parameter
REM Ex. "./Scripts/PSRDSCerts.bat {5}"

powershell.exe -ExecutionPolicy RemoteSigned -File ./Scripts/ImportRDListener.ps1 %*
powershell.exe -ExecutionPolicy RemoteSigned -File ./Scripts/ImportRDGateway.ps1 %*
