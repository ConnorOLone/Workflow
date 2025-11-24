namespace Workflow.Core.Scripting;

/// <summary>
/// Factory interface for managing script executors
/// </summary>
public interface IScriptExecutorFactory
{
    /// <summary>
    /// Gets a script executor for the specified language
    /// </summary>
    /// <param name="language">The script language (e.g., "powershell", "csharp")</param>
    /// <returns>The script executor for the specified language</returns>
    /// <exception cref="NotSupportedException">Thrown when the language is not supported</exception>
    IScriptExecutor GetExecutor(string language);

    /// <summary>
    /// Registers a new script executor
    /// </summary>
    /// <param name="executor">The executor to register</param>
    void RegisterExecutor(IScriptExecutor executor);

    /// <summary>
    /// Gets a list of all supported languages
    /// </summary>
    /// <returns>Array of supported language names</returns>
    string[] GetSupportedLanguages();
}

/// <summary>
/// Factory for creating and managing script executors
/// </summary>
public class ScriptExecutorFactory : IScriptExecutorFactory
{
    private readonly Dictionary<string, IScriptExecutor> _executors = new();

    public ScriptExecutorFactory()
    {
        // Register default executors
        RegisterExecutor(new PowerShellScriptExecutor());
        RegisterExecutor(new CSharpScriptExecutor());
    }

    public IScriptExecutor GetExecutor(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            throw new ArgumentException("Language cannot be null or empty", nameof(language));
        }

        var normalizedLanguage = language.ToLowerInvariant();

        // Handle aliases
        normalizedLanguage = normalizedLanguage switch
        {
            "ps" or "ps1" or "pwsh" => "powershell",
            "cs" or "c#" => "csharp",
            _ => normalizedLanguage
        };

        if (_executors.TryGetValue(normalizedLanguage, out var executor))
        {
            return executor;
        }

        throw new NotSupportedException(
            $"Script language '{language}' is not supported. " +
            $"Supported languages: {string.Join(", ", GetSupportedLanguages())}");
    }

    public void RegisterExecutor(IScriptExecutor executor)
    {
        if (executor == null)
        {
            throw new ArgumentNullException(nameof(executor));
        }

        var language = executor.SupportedLanguage.ToLowerInvariant();
        _executors[language] = executor;
    }

    public string[] GetSupportedLanguages()
    {
        return _executors.Keys.ToArray();
    }
}
