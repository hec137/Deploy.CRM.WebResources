echo "installation started"
$root = (Get-Location).Path + "\..\..\.."
$file = Get-ChildItem -Path $root -Recurse -Filter ".gitignore" | Select-Object -First 1
if ($file) {
     Add-Content -Path $file.FullName -Value "*/webDeploy/connections.json"
     echo "added connections.json to gitignore"
}
echo "installation complited"