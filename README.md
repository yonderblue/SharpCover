#SharpCover
[![Build Status](https://travis-ci.org/gaillard/SharpCover.png)](https://travis-ci.org/gaillard/SharpCover)

C# code coverage tool with Linux ([Mono](https://github.com/mono/mono)) and Windows ([.NET 4.0](http://www.microsoft.com/en-us/download/details.aspx?id=17851)) support.

##Features

 * [CIL](http://www.ecma-international.org/publications/standards/Ecma-335.htm) instruction coverage
 * Namespace, class, method, line and instruction inclusions/exclusions
 * Inclusions/Exclusions specifications are outside of code.
 * Cross platform Linux/Windows by way of [Cecil](http://www.mono-project.com/Cecil)
 * Easy integration into builds (target user program is invoked seperately)

##Usage

 * After [building](#tool-build) run `SharpCover.exe instrument json` where `json` is a string or file with contents that reflects the following format, most options
 are optional:

```json
{
    "assemblies": ["../anAssembly.dll", "/someplace/anotherAssembly.dll"],
    "typeInclude": ".*SomePartOfAQualifiedTypeName.*",
    "typeExclude": ".*obviouslyARegex.*",
    "methodInclude": ".*SomePartOfAQualifiedMethodName.*",
    "methodExclude": ".*obviouslyARegex.*",
    "methodBodyExcludes": [
        {
            "method": "System.Void Type::Method()",
            "offsets": [4, 8],
            "lines": ["line content", "++i;"]
        }
    ]
}
```

The exit code will be zero on instrument success.

 * Excercise the assemblies you listed in the config.

 * Afterwards run `SharpCover.exe check` in the same directory you ran `instrument`.
The results will be in `coverageResults.txt`, with missed instructions prefixed with `MISS !`.
The exit code will be zero for success, and total coverage percentage is printed.

###Notes
Full `method` names for `methodBodyExcludes` can be found in the output, as well as offsets.

The `methodBodyExcludes` by `lines` are line content matches ignoring leading/trailing whitespace.
This keeps coverage exclusions outside the code while not relying on offsets which can easily change if new code is added to the method.
For excluding instructions by line that have no source, the last instruction to have a sequence point is used as that instructions "line".

Remember to rebuild your assemblies before you instrument again !

It is highly recommended to use the includes/excludes to achieve a zero exit from `check`, otherwise you are cheating yourself !

##Tool Build

Make sure you are in the repository root.

###Linux

Make sure [Mono](https://github.com/mono/mono) which comes with [xbuild](http://www.mono-project.com/Microsoft.Build) is installed.

```bash
xbuild Gaillard.SharpCover/Program.csproj
```

###Windows

Make sure [.NET SDK](http://www.microsoft.com/en-us/download/details.aspx?id=8279) which comes with [MSBuild](http://msdn.microsoft.com/en-us/library/dd393574.aspx) is installed.

```dos
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe Gaillard.SharpCover\Program.csproj
```

Navigate to the `Gaillard.SharpCover/bin/Debug` directory where the `SharpCover.exe` executable can be used.

##Contact

Developers may be contacted at:

 * [Pull Requests](https://github.com/gaillard/SharpCover/pulls)
 * [Issues](https://github.com/gaillard/SharpCover/issues)

Questions / Feedback / Feature requests are welcome !!

##Project Build

Make sure you are in the repository root.
Make sure [nunit-console](http://www.nunit.org/index.php?p=nunit-console&r=2.2.10) is installed.

###Linux

Make sure [Mono](https://github.com/mono/mono) which comes with [xbuild](http://www.mono-project.com/Microsoft.Build) is installed.


```bash
sh build.sh
```

###Windows

Make sure [.NET SDK](http://www.microsoft.com/en-us/download/details.aspx?id=8279) which comes with [MSBuild](http://msdn.microsoft.com/en-us/library/dd393574.aspx) is installed.

```dos
build.bat
```

#####Notes

Some paths might need changing depending on your environment.

##Enhancements

A standard output format that can be used with available visualizers would be very useful.

A more complete test suite.

Contributions welcome !
