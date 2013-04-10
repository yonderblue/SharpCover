C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe Gaillard.SharpCover\Program.csproj
if %errorlevel% neq 0 exit /B 1
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe Gaillard.SharpCover.Tests\ProgramTests.csproj
if %errorlevel% neq 0 exit /B 1
Gaillard.SharpCover\bin\Debug\SharpCover.exe instrument travisCoverageConfig.json
if %errorlevel% neq 0 exit /B 1
"C:\Program Files (x86)\NUnit 2.6.2\bin\nunit-console.exe" Gaillard.SharpCover.Tests\bin\Debug\ProgramTests.dll
if %errorlevel% neq 0 exit /B 1
Gaillard.SharpCover\bin\Debug\SharpCover.exe check
