using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Workflow.Core.Scripting;

/// <summary>
/// Script executor for PowerShell scripts with security constraints
/// </summary>
public class PowerShellScriptExecutor : IScriptExecutor
{
    public string SupportedLanguage => "powershell";

    public async Task<ScriptResult> ExecuteAsync(string script, Dictionary<string, object?> variables, ScriptOptions? options = null)
    {
        options ??= new ScriptOptions();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Create initial session state with constraints
            var iss = InitialSessionState.CreateDefault();
            iss.LanguageMode = PSLanguageMode.ConstrainedLanguage;

            // Apply security constraints
            if (!options.AllowFileSystemAccess)
            {
                RemoveFileSystemCmdlets(iss);
            }

            if (!options.AllowNetworkAccess)
            {
                RemoveNetworkCmdlets(iss);
            }

            // BUG003: dangerous cmdlets are not being denied
            // Remove dangerous cmdlets regardless of options
            RemoveDangerousCmdlets(iss);

            // Create runspace
            using var runspace = RunspaceFactory.CreateRunspace(iss);
            runspace.Open();

            // Set variables
            foreach (var (key, value) in variables)
            {
                runspace.SessionStateProxy.SetVariable(key, value);
            }

            using var ps = PowerShell.Create();
            ps.Runspace = runspace;

            // Add script
            ps.AddScript(script);

            // Execute with timeout
            var task = Task.Run(() => ps.Invoke());
            if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(options.TimeoutSeconds))) != task)
            {
                ps.Stop();
                stopwatch.Stop();
                var result = ScriptResult.FailureResult($"Script execution timeout after {options.TimeoutSeconds} seconds");
                result.ExecutionTime = stopwatch.Elapsed;
                return result;
            }

            var results = await task;

            stopwatch.Stop();

            // Check for errors
            if (ps.HadErrors)
            {
                var errors = string.Join("\n", ps.Streams.Error.Select(e => e.ToString()));
                var result = ScriptResult.FailureResult(errors);
                result.ExecutionTime = stopwatch.Elapsed;
                return result;
            }

            // Capture modified variables
            var modifiedVars = new Dictionary<string, object?>();
            foreach (var key in variables.Keys)
            {
                try
                {
                    var currentValue = runspace.SessionStateProxy.GetVariable(key);
                    if (!Equals(currentValue, variables[key]))
                    {
                        modifiedVars[key] = currentValue;
                    }
                }
                catch
                {
                    // Variable might not exist anymore, skip it
                }
            }

            // Get return value (last output or explicit return)
            object? returnValue = null;
            if (results.Count > 0)
            {
                var lastResult = results[results.Count - 1];
                returnValue = lastResult?.BaseObject;
            }

            var successResult = ScriptResult.SuccessResult(returnValue, modifiedVars);
            successResult.ExecutionTime = stopwatch.Elapsed;
            return successResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var result = ScriptResult.FailureResult(ex.Message, ex.StackTrace);
            result.ExecutionTime = stopwatch.Elapsed;
            return result;
        }
    }

    private void RemoveFileSystemCmdlets(InitialSessionState iss)
    {
        var fileSystemCmdlets = new[]
        {
            "Get-Content", "Set-Content", "Add-Content", "Clear-Content",
            "Get-Item", "Set-Item", "Remove-Item", "Copy-Item", "Move-Item", "Rename-Item",
            "New-Item", "Get-ChildItem", "Test-Path", "Resolve-Path",
            "Get-ItemProperty", "Set-ItemProperty", "Remove-ItemProperty",
            "Get-Location", "Set-Location", "Push-Location", "Pop-Location",
            "Out-File", "Export-Csv", "Import-Csv", "Export-Clixml", "Import-Clixml"
        };

        foreach (var cmdlet in fileSystemCmdlets)
        {
            try
            {
                iss.Commands.Remove(cmdlet, typeof(object));
            }
            catch
            {
                // Cmdlet might not exist in this version, ignore
            }
        }
    }

    private void RemoveNetworkCmdlets(InitialSessionState iss)
    {
        var networkCmdlets = new[]
        {
            "Invoke-WebRequest", "Invoke-RestMethod",
            "Start-BitsTransfer", "Add-BitsFile",
            "Test-Connection", "Test-NetConnection",
            "New-WebServiceProxy",
            "Send-MailMessage"
        };

        foreach (var cmdlet in networkCmdlets)
        {
            try
            {
                iss.Commands.Remove(cmdlet, typeof(object));
            }
            catch
            {
                // Cmdlet might not exist in this version, ignore
            }
        }
    }

    private void RemoveDangerousCmdlets(InitialSessionState iss)
    {
        var dangerousCmdlets = new[]
        {
            "Invoke-Expression", "Invoke-Command",
            "Start-Process", "Stop-Process", "Get-Process",
            "Start-Job", "Stop-Job", "Remove-Job",
            "Enter-PSSession", "Exit-PSSession", "New-PSSession",
            "Add-Type", "New-Module", "Import-Module", "Remove-Module",
            "Set-ExecutionPolicy",
            "Start-Service", "Stop-Service", "Restart-Service",
            "Register-ScheduledJob", "Unregister-ScheduledJob",
            "New-EventLog", "Write-EventLog", "Remove-EventLog"
        };

        foreach (var cmdlet in dangerousCmdlets)
        {
            try
            {
                iss.Commands.Remove(cmdlet, typeof(object));
            }
            catch
            {
                // Cmdlet might not exist in this version, ignore
            }
        }
    }
}
