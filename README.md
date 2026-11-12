# Creation Studio 2026

A multi-language code editor and runner built with C# / WPF (.NET 8) and AvalonEdit.

## Requirements

- Windows 10/11 (64-bit Windows can run this x86 app)
- .NET 8 SDK
- Internet connection on the first build so NuGet can restore AvalonEdit

## Build and run (32-bit Windows target)

Open PowerShell in the folder containing `CreationStudio2026.csproj`:

```powershell
dotnet restore -r win-x86
dotnet run -r win-x86
```

To publish a standalone 32-bit folder:

```powershell
dotnet publish -c Release -r win-x86 --self-contained false
```

The app targets `net8.0-windows` and WPF. It must be built and run on Windows; `EnableWindowsTargeting` only permits cross-target compilation where the Windows Desktop targeting pack is available.

## How language support works

On startup (and on **Languages > Re-scan**) the app searches your PATH for each language's compiler/interpreter. Installed languages are enabled; others show "(not installed)".
Press **F5** to run the current file; output streams into the Output panel, and the `stdin` box sends input to the running program.

Built in: Python, JavaScript, TypeScript, C#, F#, C, C++, Java, Kotlin, Scala, Groovy, Go,
Rust, Zig, Nim, Swift, Dart, Ruby, PHP, Perl, Lua, R, Julia, Haskell, OCaml, Elixir, Racket,
Common Lisp, Fortran, Pascal, PowerShell, Batch, Bash, HTML.

## Add another language

**Languages > Open custom languages file** edits `%AppData%\\CreationStudio2026\\languages.json`.

Example:

```json
[
  {
    "Name": "Crystal",
    "Extensions": ".cr",
    "Exes": "crystal",
    "Run": "{exe} run \\"{file}\\"",
    "Comment": "#",
    "Keywords": "def end if else class",
    "Template": "puts \\"hi\\""
  }
]
```

Tokens: `{exe}` `{file}` `{dir}` `{name}` `{out}` (temporary output path for compiled languages).

## Shortcuts

F5 run · Shift+F5 stop · Ctrl+S save · Ctrl+N new · Ctrl+O open


## Integrated terminal and C# execution

- Type a Windows command in the bottom terminal input and press Enter. Commands run in the remembered terminal working directory.
- The working directory follows the opened project folder or active file. Use `cd`, `cd ..`, or `cd "C:\some folder"` to change it; `cls` clears the terminal output.
- Commands execute through Windows `cmd.exe`; long-running programs can receive standard input through the same input box, and Shift+F5 stops the process.
- Select C# and press F5 on a `.cs` file. Creation Studio creates a temporary .NET 8 SDK project and runs it with `dotnet run`; `dotnet-script` is not required.
- The supplied black-and-white cube image is used for the window and executable icon.
- The file explorer uses extension-aware colored names for folders and common source, markup, image, and executable files.
- Requires the .NET 8 SDK for C# execution.
