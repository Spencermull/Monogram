using System.Diagnostics;

namespace mngc.Driver;

public record CompileResult(bool Success, string Errors);

public static class GccDriver
{
    public static CompileResult Compile(string cPath, string outputPath, bool needsMath = false)
    {
        var compiler = FindCompiler();
        if (compiler == null)
            return new CompileResult(false, "no C compiler found on PATH (looked for gcc, clang, cc)");

        var psi = new ProcessStartInfo(compiler)
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };

        // When using a full path, inject the compiler's bin dir so its DLLs are found
        if (Path.IsPathRooted(compiler))
        {
            var binDir = Path.GetDirectoryName(compiler)!;
            var existing = psi.Environment.TryGetValue("PATH", out var p) ? p : Environment.GetEnvironmentVariable("PATH") ?? "";
            psi.Environment["PATH"] = binDir + Path.PathSeparator + existing;
        }

        psi.ArgumentList.Add(cPath);
        psi.ArgumentList.Add("-o");
        psi.ArgumentList.Add(outputPath);
        psi.ArgumentList.Add("-std=c11");
        if (needsMath) psi.ArgumentList.Add("-lm");

        using var proc = Process.Start(psi)!;
        var errors = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        return new CompileResult(proc.ExitCode == 0, errors);
    }

    private static readonly string[] WindowsFallbackPaths =
    [
        @"C:\msys64\ucrt64\bin\gcc.exe",
        @"C:\msys64\mingw64\bin\gcc.exe",
        @"C:\msys64\clang64\bin\clang.exe",
        @"C:\Program Files\LLVM\bin\clang.exe",
    ];

    private static string? FindCompiler()
    {
        foreach (var name in new[] { "gcc", "clang", "cc" })
        {
            var probe = new ProcessStartInfo(
                OperatingSystem.IsWindows() ? "where" : "which", name)
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
            };
            using var proc = Process.Start(probe);
            proc?.WaitForExit();
            if (proc?.ExitCode == 0) return name;
        }

        if (OperatingSystem.IsWindows())
        {
            foreach (var path in WindowsFallbackPaths)
                if (File.Exists(path)) return path;
        }

        return null;
    }

    public static string DefaultOutputPath(string inputMngrm)
    {
        var noExt = Path.ChangeExtension(inputMngrm, null);
        return OperatingSystem.IsWindows() ? noExt + ".exe" : noExt;
    }
}
