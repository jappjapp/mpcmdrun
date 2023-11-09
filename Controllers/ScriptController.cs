using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace RunPwshScript.Controllers;
[ApiController]
[Route("[controller]")]
public class ScriptController : ControllerBase
{
    private readonly ILogger<ScriptController> _logger;
 
    public ScriptController(ILogger<ScriptController> logger)
    {
        _logger = logger;
    }

    [HttpGet("run")]
    public async Task<string> RunScript()
    {
        var path = @"C:\Temp\";
        //var path = @"C:\Temp\doesnotexist";

        var returnValueBeforeProcessExit = ErrorOrMalware();

        var process = new Process // No using statement, manual dispose in background thread
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = @"C:\Program Files\Windows Defender\MpCmdRun.exe",
                Arguments = $"-Scan -ScanType 3 -File {path}",
                RedirectStandardOutput = true,
            }
        };

        process.Start();

        for (var lineNumber = 1; lineNumber <= 3; lineNumber++)
        {
            var lineValue = process.StandardOutput.ReadLine();

            if (lineValue == null)
            {
                break;
            }

            if (lineNumber == 1 && !lineValue.ToLower().Contains("scan starting"))
            {
                break;
            }

            if (lineNumber == 2 && !lineValue.ToLower().Contains("scan finished"))
            {
                break;
            }

            if (lineNumber == 3 && !lineValue.ToLower().Contains("no threats"))
            {
                break;
            }

            if (lineNumber == 3 && lineValue.ToLower().Contains("no threats"))
            {
                returnValueBeforeProcessExit = "sucess";
            }
        }

        // At this point the process can become long-running if it detected a threat that
        // takes some time to clean up. Therefore, execute the rest of the process on a background thread
        // without waiting for it (do not await it, discard the task)
        // This is probably not fool proof, the process is likely created as a child to the web api process
        // which probably can cause problems in edge cases like if the web api terminates
        // and a child MpCmdRun process is executing a long-running cleanup of a big malicious file
        _ = Task.Run(() =>
        {
            process.WaitForExit();
            process.Dispose();
        });

        return returnValueBeforeProcessExit;
    }

    private string ErrorOrMalware()
    {
        return "bad";
    }
}
