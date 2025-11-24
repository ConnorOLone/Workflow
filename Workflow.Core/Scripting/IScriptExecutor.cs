namespace Workflow.Core.Scripting;

/// <summary>
/// Defines the contract for script executors that can execute code in different languages
/// </summary>
public interface IScriptExecutor
{
    /// <summary>
    /// Gets the language supported by this executor (e.g., "powershell", "csharp")
    /// </summary>
    string SupportedLanguage { get; }

    /// <summary>
    /// Executes a script with the provided variables and options
    /// </summary>
    /// <param name="script">The script code to execute</param>
    /// <param name="variables">Variables to pass to the script</param>
    /// <param name="options">Execution options (timeout, security constraints, etc.)</param>
    /// <returns>The result of script execution including return value and modified variables</returns>
    Task<ScriptResult> ExecuteAsync(string script, Dictionary<string, object?> variables, ScriptOptions? options = null);
}

/// <summary>
/// Represents the result of script execution
/// </summary>
public class ScriptResult
{
    /// <summary>
    /// Indicates whether the script executed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The return value or last output of the script
    /// </summary>
    public object? ReturnValue { get; set; }

    /// <summary>
    /// Variables that were modified during script execution
    /// </summary>
    public Dictionary<string, object?> ModifiedVariables { get; set; } = new();

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error stack trace if execution failed
    /// </summary>
    public string? ErrorStackTrace { get; set; }

    /// <summary>
    /// Time taken to execute the script
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Creates a successful script result
    /// </summary>
    public static ScriptResult SuccessResult(object? returnValue, Dictionary<string, object?>? modifiedVars = null)
    {
        return new ScriptResult
        {
            Success = true,
            ReturnValue = returnValue,
            ModifiedVariables = modifiedVars ?? new()
        };
    }

    /// <summary>
    /// Creates a failed script result
    /// </summary>
    public static ScriptResult FailureResult(string errorMessage, string? stackTrace = null)
    {
        return new ScriptResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            ErrorStackTrace = stackTrace
        };
    }
}

/// <summary>
/// Configuration options for script execution
/// </summary>
public class ScriptOptions
{
    /// <summary>
    /// Maximum execution time in seconds (default: 30)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether the script can access the file system (default: false)
    /// </summary>
    public bool AllowFileSystemAccess { get; set; } = false;

    /// <summary>
    /// Whether the script can access the network (default: false)
    /// </summary>
    public bool AllowNetworkAccess { get; set; } = false;

    /// <summary>
    /// Additional namespaces to allow (for C# scripts)
    /// </summary>
    public List<string> AllowedNamespaces { get; set; } = new();

    /// <summary>
    /// Additional assemblies to allow (for C# scripts)
    /// </summary>
    public List<string> AllowedAssemblies { get; set; } = new();
}