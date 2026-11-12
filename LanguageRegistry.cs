using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CreationStudio2026;

public class LanguageDef
{
    public string Name { get; set; } = "";
    /// <summary>Space-separated, e.g. ".py .pyw"</summary>
    public string Extensions { get; set; } = "";
    /// <summary>Space-separated executables to look for on PATH (first found wins).</summary>
    public string Exes { get; set; } = "";
    /// <summary>Command template. Tokens: {exe} {file} {dir} {name} {out}</summary>
    public string Run { get; set; } = "";
    public string Comment { get; set; } = "//";
    public string Keywords { get; set; } = "";
    public string Template { get; set; } = "";

    [JsonIgnore] public string? ResolvedExe { get; set; }
    [JsonIgnore] public string[] ExtList => Extensions.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    [JsonIgnore] public string[] ExeList => Exes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    [JsonIgnore] public bool Installed => ExeList.Length == 0 || ResolvedExe != null;
    [JsonIgnore] public string Display => Installed ? Name : Name + " (not installed)";
}

public static class LanguageRegistry
{
    public static List<LanguageDef> All { get; private set; } = new();
    public static LanguageDef Plain => All.First(l => l.Name == "Plain Text");

    public static string UserFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CreationStudio2026", "languages.json");

    // ---- keyword sets (kept short, just for highlighting) ----
    const string C = "auto break case char const continue default do double else enum extern float for goto if inline int long register return short signed sizeof static struct switch typedef union unsigned void volatile while";
    const string CPP = C + " class namespace new delete template typename public private protected virtual bool true false nullptr using try catch throw this operator constexpr";
    const string CS = "abstract as base bool break byte case catch char class const continue decimal default delegate do double else enum event false finally float for foreach if in int interface is lock long namespace new null object out override private protected public readonly ref return static string struct switch this throw true try using var virtual void while async await record";
    const string JAVA = "abstract boolean break byte case catch char class const continue default do double else enum extends final finally float for if implements import instanceof int interface long new null package private protected public return short static super switch this throw throws true false try void volatile while var";
    const string PY = "and as assert async await break class continue def del elif else except False finally for from global if import in is lambda None nonlocal not or pass raise return True try while with yield print";
    const string JS = "async await break case catch class const continue default delete do else export extends false finally for function if import in instanceof let new null return super switch this throw true try typeof var void while yield";

    static LanguageDef L(string name, string ext, string exes, string run, string comment, string kw, string tpl) =>
        new() { Name = name, Extensions = ext, Exes = exes, Run = run, Comment = comment, Keywords = kw, Template = tpl };

    static List<LanguageDef> BuiltIn() => new()
    {
        L("Plain Text", ".txt", "", "", "", "", ""),
        L("Python", ".py .pyw", "python python3 py", "{exe} \"{file}\"", "#", PY, "print(\"Hello from Creation Studio 2026!\")\n"),
        L("JavaScript (Node)", ".js .mjs", "node", "{exe} \"{file}\"", "//", JS, "console.log(\"Hello from Creation Studio 2026!\");\n"),
        L("TypeScript", ".ts", "tsx ts-node", "{exe} \"{file}\"", "//", JS + " interface type enum implements public private protected readonly abstract namespace declare", "const msg: string = \"Hello from Creation Studio 2026!\";\nconsole.log(msg);\n"),
        L("C#", ".cs .csx", "dotnet", "{exe} \"{file}\"", "//", CS, "Console.WriteLine(\"Hello from Creation Studio 2026!\");\n"),
        L("F#", ".fsx", "dotnet", "{exe} fsi \"{file}\"", "//", "let mutable if then else match with fun type module open true false for do in", "printfn \"Hello from Creation Studio 2026!\"\n"),
        L("C", ".c", "gcc clang", "{exe} \"{file}\" -o \"{out}.exe\" && \"{out}.exe\"", "//", C, "#include <stdio.h>\n\nint main(void) {\n    printf(\"Hello from Creation Studio 2026!\\n\");\n    return 0;\n}\n"),
        L("C++", ".cpp .cc .cxx", "g++ clang++", "{exe} \"{file}\" -o \"{out}.exe\" && \"{out}.exe\"", "//", CPP, "#include <iostream>\n\nint main() {\n    std::cout << \"Hello from Creation Studio 2026!\\n\";\n    return 0;\n}\n"),
        L("Java", ".java", "java", "{exe} \"{file}\"", "//", JAVA, "public class Main {\n    public static void main(String[] args) {\n        System.out.println(\"Hello from Creation Studio 2026!\");\n    }\n}\n"),
        L("Kotlin", ".kt", "kotlinc", "{exe} \"{file}\" -include-runtime -d \"{out}.jar\" && java -jar \"{out}.jar\"", "//", "as break class continue do else false for fun if in interface is null object package return super this throw true try typealias val var when while", "fun main() {\n    println(\"Hello from Creation Studio 2026!\")\n}\n"),
        L("Scala", ".scala", "scala", "{exe} \"{file}\"", "//", "abstract case class def do else extends false final for if implicit import match new null object override package private return sealed super this throw trait true try type val var while with yield", "@main def hello() = println(\"Hello from Creation Studio 2026!\")\n"),
        L("Groovy", ".groovy", "groovy", "{exe} \"{file}\"", "//", JAVA + " def", "println \"Hello from Creation Studio 2026!\"\n"),
        L("Go", ".go", "go", "{exe} run \"{file}\"", "//", "break case chan const continue default defer else fallthrough for func go goto if import interface map package range return select struct switch type var true false nil", "package main\n\nimport \"fmt\"\n\nfunc main() {\n\tfmt.Println(\"Hello from Creation Studio 2026!\")\n}\n"),
        L("Rust", ".rs", "rustc", "{exe} \"{file}\" -o \"{out}.exe\" && \"{out}.exe\"", "//", "as async await break const continue crate else enum extern false fn for if impl in let loop match mod move mut pub ref return self Self static struct super trait true type unsafe use where while", "fn main() {\n    println!(\"Hello from Creation Studio 2026!\");\n}\n"),
        L("Zig", ".zig", "zig", "{exe} run \"{file}\"", "//", "const var fn pub if else while for return try catch defer struct enum union switch", "const std = @import(\"std\");\n\npub fn main() void {\n    std.debug.print(\"Hello from Creation Studio 2026!\\n\", .{});\n}\n"),
        L("Nim", ".nim", "nim", "{exe} c -r --hints:off \"{file}\"", "#", "proc var let const if elif else for while return import echo when case of", "echo \"Hello from Creation Studio 2026!\"\n"),
        L("Swift", ".swift", "swift", "{exe} \"{file}\"", "//", "as break case class continue default defer do else enum extension false for func guard if import in init is let nil protocol return self struct switch throw true try var while", "print(\"Hello from Creation Studio 2026!\")\n"),
        L("Dart", ".dart", "dart", "{exe} run \"{file}\"", "//", "abstract as async await break case catch class const continue default do else enum extends false final finally for if import in is late new null return static super switch this throw true try var void while", "void main() {\n  print('Hello from Creation Studio 2026!');\n}\n"),
        L("Ruby", ".rb", "ruby", "{exe} \"{file}\"", "#", "alias and begin break case class def do else elsif end ensure false for if in module next nil not or redo rescue retry return self super then true undef unless until when while yield puts", "puts \"Hello from Creation Studio 2026!\"\n"),
        L("PHP", ".php", "php", "{exe} \"{file}\"", "//", "abstract and array as break case catch class const continue default do echo else elseif extends false final finally for foreach function if implements include interface namespace new null private protected public require return static switch this throw true try use var while", "<?php\necho \"Hello from Creation Studio 2026!\\n\";\n"),
        L("Perl", ".pl .pm", "perl", "{exe} \"{file}\"", "#", "my our sub if elsif else unless while for foreach return use package print", "print \"Hello from Creation Studio 2026!\\n\";\n"),
        L("Lua", ".lua", "lua luajit", "{exe} \"{file}\"", "--", "and break do else elseif end false for function goto if in local nil not or repeat return then true until while", "print(\"Hello from Creation Studio 2026!\")\n"),
        L("R", ".r", "Rscript", "{exe} \"{file}\"", "#", "function if else for while repeat break next return TRUE FALSE NULL NA", "cat(\"Hello from Creation Studio 2026!\\n\")\n"),
        L("Julia", ".jl", "julia", "{exe} \"{file}\"", "#", "function end if elseif else for while return begin let module using import struct true false nothing", "println(\"Hello from Creation Studio 2026!\")\n"),
        L("Haskell", ".hs", "runghc runhaskell", "{exe} \"{file}\"", "--", "module import where let in if then else case of data type class instance do", "main :: IO ()\nmain = putStrLn \"Hello from Creation Studio 2026!\"\n"),
        L("OCaml", ".ml", "ocaml", "{exe} \"{file}\"", "", "let in if then else match with fun rec type module open begin end for do done while", "let () = print_endline \"Hello from Creation Studio 2026!\"\n"),
        L("Elixir", ".exs .ex", "elixir", "{exe} \"{file}\"", "#", "def defmodule do end if else case cond fn when true false nil import", "IO.puts(\"Hello from Creation Studio 2026!\")\n"),
        L("Racket", ".rkt", "racket", "{exe} \"{file}\"", ";", "define lambda let if cond else and or not", "#lang racket\n(displayln \"Hello from Creation Studio 2026!\")\n"),
        L("Common Lisp", ".lisp .cl", "sbcl", "{exe} --script \"{file}\"", ";", "defun defvar let if cond lambda progn setf loop", "(format t \"Hello from Creation Studio 2026!~%\")\n"),
        L("Fortran", ".f90 .f95", "gfortran", "{exe} \"{file}\" -o \"{out}.exe\" && \"{out}.exe\"", "!", "program end implicit none integer real print do if then else", "program hello\n  print *, \"Hello from Creation Studio 2026!\"\nend program hello\n"),
        L("Pascal", ".pas", "fpc", "{exe} \"{file}\" -o\"{out}.exe\" && \"{out}.exe\"", "//", "program begin end var if then else for do while writeln uses", "program Hello;\nbegin\n  writeln('Hello from Creation Studio 2026!');\nend.\n"),
        L("PowerShell", ".ps1", "powershell pwsh", "{exe} -NoProfile -ExecutionPolicy Bypass -File \"{file}\"", "#", "if elseif else foreach for while function param return switch try catch finally Write-Host Write-Output", "Write-Host \"Hello from Creation Studio 2026!\"\n"),
        L("Batch", ".bat .cmd", "cmd", "\"{file}\"", "::", "echo set if else for goto call exit rem", "@echo off\necho Hello from Creation Studio 2026!\n"),
        L("Bash", ".sh", "bash", "{exe} \"{file}\"", "#", "if then else elif fi for while do done case esac function in echo exit return local", "echo \"Hello from Creation Studio 2026!\"\n"),
        L("HTML (open in browser)", ".html .htm", "cmd", "start \"\" \"{file}\"", "", "", "<!DOCTYPE html>\n<html>\n<body>\n  <h1>Hello from Creation Studio 2026!</h1>\n</body>\n</html>\n"),
    };

    public static void Load()
    {
        All = BuiltIn();
        try
        {
            if (File.Exists(UserFile))
            {
                var user = JsonSerializer.Deserialize<List<LanguageDef>>(File.ReadAllText(UserFile),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (user != null)
                    foreach (var u in user)
                    {
                        All.RemoveAll(l => l.Name.Equals(u.Name, StringComparison.OrdinalIgnoreCase));
                        All.Add(u);
                    }
            }
        }
        catch { /* ignore malformed user file */ }
        Detect();
    }

    /// <summary>Checks PATH for each language's executables.</summary>
    public static void Detect()
    {
        foreach (var l in All)
            l.ResolvedExe = l.ExeList.FirstOrDefault(FoundOnPath);
        All = All.OrderByDescending(l => l.Installed).ThenBy(l => l.Name).ToList();
    }

    static bool FoundOnPath(string exe)
    {
        var exts = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT").Split(';');
        var dirs = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var d in dirs)
            foreach (var e in exts.Prepend(""))
            {
                try { if (File.Exists(Path.Combine(d.Trim('"'), exe + e))) return true; } catch { }
            }
        return false;
    }

    public static LanguageDef ForFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return All.FirstOrDefault(l => l.ExtList.Contains(ext)) ?? Plain;
    }
}
