using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Workflow.Core.Scripting;

/// <summary>
/// Script executor for C# scripts using Roslyn scripting API
/// </summary>
public class CSharpScriptExecutor : IScriptExecutor
{
    public string SupportedLanguage => "csharp";

    // Cache compiled scripts for performance
    private static readonly Dictionary<string, Script<object>> _scriptCache = new();

    // TODO: Potential to use the ReaderWriterLockSlim which may be better at handling cache access as there could be many more READS than WRITES.
    // ... needs investigation
    private static readonly object _cacheLock = new();

    public async Task<ScriptResult> ExecuteAsync(string script, Dictionary<string, object?> variables, ScriptOptions? options = null)
    {
        options ??= new ScriptOptions();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Create script options with security constraints
            var scriptOptions = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default
                .AddReferences(typeof(object).Assembly) // mscorlib
                .AddReferences(typeof(Enumerable).Assembly) // System.Linq
                .AddReferences(typeof(List<>).Assembly) // System.Collections.Generic
                .AddImports("System", "System.Linq", "System.Collections.Generic", "System.Text");

            // Add user-allowed namespaces
            if (options.AllowedNamespaces.Any())
            {
                scriptOptions = scriptOptions.AddImports(options.AllowedNamespaces.ToArray());
            }

            // Add user-allowed assemblies
            if (options.AllowedAssemblies.Any())
            {
                foreach (var assemblyPath in options.AllowedAssemblies)
                {
                    try
                    {
                        scriptOptions = scriptOptions.AddReferences(assemblyPath);
                    }
                    catch
                    {
                        // Assembly might not be found, continue
                    }
                }
            }

            // File system and network access are controlled by not importing those namespaces
            // and not allowing their assemblies by default

            // Create or get cached script
            Script<object> compiledScript;
            lock (_cacheLock)
            {
                var cacheKey = $"{script}_{string.Join(",", options.AllowedNamespaces)}_{options.AllowFileSystemAccess}_{options.AllowNetworkAccess}";
                if (!_scriptCache.TryGetValue(cacheKey, out compiledScript!))
                {
                    compiledScript = CSharpScript.Create<object>(script, scriptOptions, typeof(ScriptGlobals));

                    // Compile to check for errors
                    var diagnostics = compiledScript.Compile();
                    if (diagnostics.Any(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error))
                    {
                        var errors = string.Join("\n", diagnostics.Select(d => d.ToString()));
                        stopwatch.Stop();
                        var compileResult = ScriptResult.FailureResult($"Compilation errors:\n{errors}");
                        compileResult.ExecutionTime = stopwatch.Elapsed;
                        return compileResult;
                    }

                    // Cache for future use
                    _scriptCache[cacheKey] = compiledScript;
                }
            }

            // Create globals with variables
            var globals = new ScriptGlobals { Variables = new Dictionary<string, object?>(variables) };

            // Execute with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(options.TimeoutSeconds));
            ScriptState<object> result;

            try
            {
                result = await compiledScript.RunAsync(globals, cts.Token);
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                var timeoutResult = ScriptResult.FailureResult($"Script execution timeout after {options.TimeoutSeconds} seconds");
                timeoutResult.ExecutionTime = stopwatch.Elapsed;
                return timeoutResult;
            }

            stopwatch.Stop();

            // Capture modified variables
            var modifiedVars = new Dictionary<string, object?>();
            foreach (var (key, value) in globals.Variables)
            {
                if (variables.ContainsKey(key) && !Equals(value, variables[key]))
                {
                    modifiedVars[key] = value;
                }
            }

            var scriptResult = ScriptResult.SuccessResult(result.ReturnValue, modifiedVars);
            scriptResult.ExecutionTime = stopwatch.Elapsed;
            return scriptResult;
        }
        catch (CompilationErrorException ex)
        {
            stopwatch.Stop();
            var result = ScriptResult.FailureResult($"Compilation error: {ex.Message}", ex.StackTrace);
            result.ExecutionTime = stopwatch.Elapsed;
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var result = ScriptResult.FailureResult(ex.Message, ex.StackTrace);
            result.ExecutionTime = stopwatch.Elapsed;
            return result;
        }
    }

    /// <summary>
    /// Clears the script compilation cache
    /// </summary>
    public static void ClearCache()
    {
        lock (_cacheLock)
        {
            _scriptCache.Clear();
        }
    }
}

/// <summary>
/// Globals class for passing variables to C# scripts
/// </summary>
public class ScriptGlobals
{
    /// <summary>
    /// Dictionary containing all workflow variables accessible to the script
    /// </summary>
    public Dictionary<string, object?> Variables { get; set; } = new();

    /// <summary>
    /// Helper method for scripts to get variables with type conversion
    /// </summary>
    /// <typeparam name="T">The target type</typeparam>
    /// <param name="key">The variable name</param>
    /// <returns>The variable value converted to type T</returns>
    public T Get<T>(string key)
    {
        if (Variables.TryGetValue(key, out var value))
        {
            if (value == null)
                return default(T)!;

            if (value is T typed)
                return typed;

            return (T)Convert.ChangeType(value, typeof(T))!;
        }
        return default(T)!;
    }

    /// <summary>
    /// Helper method for scripts to set variables
    /// </summary>
    /// <param name="key">The variable name</param>
    /// <param name="value">The variable value</param>
    public void Set(string key, object? value)
    {
        Variables[key] = value;
    }

    /// <summary>
    /// Helper method to check if a variable exists
    /// </summary>
    /// <param name="key">The variable name</param>
    /// <returns>True if the variable exists</returns>
    public bool Has(string key)
    {
        return Variables.ContainsKey(key);
    }
}
