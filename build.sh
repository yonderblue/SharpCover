/usr/bin/xbuild Gaillard.SharpCover/Program.csproj \
&& /usr/bin/xbuild Gaillard.SharpCover.Tests/ProgramTests.csproj \
&& /usr/bin/mono Gaillard.SharpCover/bin/Debug/SharpCover.exe instrument travisCoverageConfig.json \
&& /usr/bin/nunit-console Gaillard.SharpCover.Tests/bin/Debug/ProgramTests.dll \
&& /usr/bin/mono Gaillard.SharpCover/bin/Debug/SharpCover.exe check
