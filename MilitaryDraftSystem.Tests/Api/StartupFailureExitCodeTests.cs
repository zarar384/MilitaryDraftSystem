using System.Diagnostics;

namespace MilitaryDraftSystem.Tests.Api
{
    /// <summary>
    /// Verifies the application-specific behavior added to <c>Program.cs</c>'s
    /// top-level startup handler: a genuine startup failure (here, an
    /// invalid/missing required connection string, which fails
    /// <c>ValidateOnStart</c>) must make the process exit with a non-zero
    /// exit code, not just log <c>Log.Fatal</c> and exit "successfully".
    /// </summary>
    public class StartupFailureExitCodeTests
    {
        [Fact]
        public void Process_ShouldExitWithNonZeroCode_WhenRequiredConnectionStringIsMissing()
        {
            var apiDllPath = FindApiAssemblyPath();

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{apiDllPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(apiDllPath),
            };

            // Deliberately fail ValidateOnStart for ConnectionStringsOptions
            // by providing an empty connection string, and avoid binding to
            // a real network port.
            startInfo.Environment["ConnectionStrings__DefaultConnection"] = string.Empty;
            startInfo.Environment["ASPNETCORE_URLS"] = "http://127.0.0.1:0";

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start the API process.");

            // Drain stdout/stderr asynchronously: without this, the child
            // process can block forever once its redirected output pipe
            // buffer fills up, which would otherwise make this test hang
            // instead of observing the real exit code.
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var exited = process.WaitForExit(TimeSpan.FromSeconds(30));
            if (!exited)
            {
                process.Kill(entireProcessTree: true);
            }

            Assert.True(exited, "The API process did not exit within the expected timeout.");
            Assert.NotEqual(0, process.ExitCode);
        }

        private static string FindApiAssemblyPath()
        {
            // The API project is referenced (transitively, via
            // WebApplicationFactory<Program> usage elsewhere in this test
            // project), so its build output is copied alongside the test
            // assembly into the same output directory.
            var apiDllPath = Path.Combine(AppContext.BaseDirectory, "MilitaryDraftSystem.API.dll");

            if (!File.Exists(apiDllPath))
            {
                throw new FileNotFoundException(
                    $"Could not locate the built API assembly at '{apiDllPath}'. Build the solution before running this test.",
                    apiDllPath);
            }

            return apiDllPath;
        }
    }
}
