echo "Making dir"
mkdir .\publish
echo "Copying Mavis release (1/2)"
xcopy /d /e /s /y src\Mavis\Mavis\bin\Release\net5.0 publish\net5.0\
echo "Copying Sources (2/2)"
xcopy /y %appdata%\SplatTag\Snapshot-*.json publish\
REM xcopy /f /y %appdata%\SplatTag\dola-gsheet-access-*.json publish\dola-gsheet-access-*.json
