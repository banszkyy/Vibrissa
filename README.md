# Vibrissa

[![.Net 11.0](https://img.shields.io/badge/.NET-11.0-5C2D91?style=flat-square)](#)

**Bytecode interpreted, statically-typed, embedded** language with minimal restrictions. On top of custom bytecode, it can also generate Brainfuck, and optimize functions into MSIL, or compile the whole script into a [DynamicMethod](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.emit.dynamicmethod).

[Wiki](https://github.com/banszkyy/Vibrissa/wiki) • [VSCode Extension](https://github.com/banszkyy/Vibrissa-VSCodeExtension) • [Language Server](https://github.com/banszkyy/Vibrissa-LanguageServer) • [Debugger Host](https://github.com/banszkyy/Vibrissa-DebugHost)

## Goals

- Familiar syntax (like C)
- Full control over the runtime
- Minimal built-in features, maximum flexibility

## Hello World

```cpp
using "https://raw.githubusercontent.com/banszkyy/Vibrissa/master/StandardLibrary/System.Console.vi";

print("hello, world\n");
```

Without dependencies:

```cpp
[External("stdout")]
void print(u8 message);

void print(u8[] message)
{
    for (i32 i = 0; message[i]; i++)
    {
        print(message[i]);
    }
}

print("hello, world\n");
```

## Building

It's just a .NET project so you can build it as usual. [read more](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build)

```sh
git clone https://github.com/banszkyy/Vibrissa
cd Vibrissa
dotnet publish Utility/Utility.csproj --configuration Release --output ./out/linux-x64
```

### Unity

1. Import the `/Unity/package.json` using the Unity Package Manager. [read more](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-ui-local.html)
2. Create a symlink at `/Unity/Source` pointing at `/Source`. To do so, run this inside the `/Unity` directory:\
    Linux:
    ```sh
    ln -s ../Source Runtime
    ```
    Windows: [read more](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/mklink)
    ```sh
    mklink /j "Runtime" "..\Source"
    ```
3. In Unity, navigate to `Edit > Project Settings... > Player > Other Settings > Scripting Define Symbols` and add the `UNITY` variable. [read more](https://docs.unity3d.com/2022.3/Documentation//Manual/CustomScriptingSymbols.html)
    - If you are using the [Burst compiler](https://docs.unity3d.com/Packages/com.unity.burst@latest), add `UNITY_BURST` too. You must also add a reference to `Unity.Burst` in `/Unity/Vibrissa.asmdef`:
    ```json
    {
      ...
      "references": [ "Unity.Burst" ],
      ...
    }
    ```
    - If you want [profiler analytics](https://docs.unity3d.com/6000.3/Documentation/Manual/profiler-introduction.html), add `UNITY_PROFILER` too.
4. Install the necessary NuGet packages with this tool: [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) or import the dll-s manually. You only need to install these:
    - [System.Collections.Immutable](https://www.nuget.org/packages/System.Collections.Immutable)

## Troubleshooting

### api-ms-win-crt-string-l1-1-0.dll Missing Error

install [this](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170)

## Project Structure

- `/Examples` Examples for using the project as a library.
- `/StandardLibrary` Pre-implemented functions and structures and some "external function" declarations.
- `/TestFiles` Test files for testing.
- `/Source` The core functionality.
- `/Utility` The command line interface.
- `/Debugger` A terminal based debugger.

## [Tests](https://github.com/banszkyy/Vibrissa/blob/master/Tests.md)
