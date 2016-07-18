xbuild Gaillard.SharpCover/Program.csproj \
&& xbuild Gaillard.SharpCover.Tests/ProgramTests.csproj \
&& mono Gaillard.SharpCover/bin/Debug/SharpCover.exe instrument travisCoverageConfig.json \
&& nunit-console Gaillard.SharpCover.Tests/bin/Debug/ProgramTests.dll \
&& mono Gaillard.SharpCover/bin/Debug/SharpCover.exe check || true \
&& mono Gaillard.SharpCover/bin/Debug/SharpCover.exe view html -yellow=90 -red=70