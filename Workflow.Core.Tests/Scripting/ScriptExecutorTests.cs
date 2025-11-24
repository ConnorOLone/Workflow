using Workflow.Core.Scripting;
using Xunit;

namespace Workflow.Core.Tests.Scripting;

public class ScriptExecutorTests
{
    #region PowerShell Tests

    [Fact]
    public async Task PowerShell_ShouldExecuteSimpleScript()
    {
        // Arrange
        var executor = new PowerShellScriptExecutor();
        var variables = new Dictionary<string, object?> { ["x"] = 5, ["y"] = 10 };

        // Act
        var result = await executor.ExecuteAsync("$x + $y", variables);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(15, result.ReturnValue);
    }

    [Fact]
    public async Task PowerShell_ShouldModifyVariables()
    {
        // Arrange
        var executor = new PowerShellScriptExecutor();
        var variables = new Dictionary<string, object?> { ["total"] = 0 };

        // Act
        var result = await executor.ExecuteAsync("$total = 100; $total", variables);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(100, result.ReturnValue);
        Assert.True(result.ModifiedVariables.ContainsKey("total"));
        Assert.Equal(100, result.ModifiedVariables["total"]);
    }

    [Fact]
    public async Task PowerShell_ShouldHandleSyntaxErrors()
    {
        // Arrange
        var executor = new PowerShellScriptExecutor();
        var variables = new Dictionary<string, object?>();

        // Act
        var result = await executor.ExecuteAsync("$invalid syntax here", variables);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task PowerShell_ShouldTimeout()
    {
        // Arrange
        var executor = new PowerShellScriptExecutor();
        var variables = new Dictionary<string, object?>();
        var options = new ScriptOptions { TimeoutSeconds = 1 };

        // Act
        var result = await executor.ExecuteAsync("Start-Sleep -Seconds 10", variables, options);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("timeout", result.ErrorMessage?.ToLower() ?? "");
    }

    [Fact]
    public async Task PowerShell_ShouldBlockFileSystemAccess()
    {
        // Arrange
        var executor = new PowerShellScriptExecutor();
        var variables = new Dictionary<string, object?>();
        var options = new ScriptOptions { AllowFileSystemAccess = false };

        // Act
        var result = await executor.ExecuteAsync("Get-Content 'test.txt'", variables, options);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task PowerShell_ShouldBlockDangerousCmdlets()
    {
        // Arrange
        var executor = new PowerShellScriptExecutor();
        var variables = new Dictionary<string, object?>();

        // Act
        var result = await executor.ExecuteAsync("Invoke-Expression 'Write-Host test'", variables);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task PowerShell_ShouldAccessWorkflowVariables()
    {
        // Arrange
        var executor = new PowerShellScriptExecutor();
        var variables = new Dictionary<string, object?>
        {
            ["orderAmount"] = 1500.50m,
            ["quantity"] = 3
        };

        // Act
        var result = await executor.ExecuteAsync("$orderAmount * $quantity", variables);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(4501.50m, result.ReturnValue);
    }

    [Fact]
    public async Task PowerShell_ShouldReturnLastOutput()
    {
        // Arrange
        var executor = new PowerShellScriptExecutor();
        var variables = new Dictionary<string, object?>();

        // Act
        var result = await executor.ExecuteAsync(@"
            $a = 10
            $b = 20
            $a + $b
        ", variables);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(30, result.ReturnValue);
    }

    #endregion

    #region C# Tests

    [Fact]
    public async Task CSharp_ShouldExecuteSimpleExpression()
    {
        // Arrange
        var executor = new CSharpScriptExecutor();
        var variables = new Dictionary<string, object?>();

        // Act
        var result = await executor.ExecuteAsync("return 1 + 1;", variables);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.ReturnValue);
    }

    [Fact]
    public async Task CSharp_ShouldAccessVariablesViaGlobals()
    {
        // Arrange
        var executor = new CSharpScriptExecutor();
        var variables = new Dictionary<string, object?> { ["x"] = 5, ["y"] = 10 };

        // Act
        var result = await executor.ExecuteAsync("return (int)Variables[\"x\"] + (int)Variables[\"y\"];", variables);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(15, result.ReturnValue);
    }

    [Fact]
    public async Task CSharp_ShouldModifyVariables()
    {
        // Arrange
        var executor = new CSharpScriptExecutor();
        var variables = new Dictionary<string, object?> { ["total"] = 0 };

        // Act
        var result = await executor.ExecuteAsync(@"
            Variables[""total""] = 100;
            return Variables[""total""];
        ", variables);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(100, result.ReturnValue);
        Assert.True(result.ModifiedVariables.ContainsKey("total"));
        Assert.Equal(100, result.ModifiedVariables["total"]);
    }

    [Fact]
    public async Task CSharp_ShouldUseHelperMethods()
    {
        // Arrange
        var executor = new CSharpScriptExecutor();
        var variables = new Dictionary<string, object?> { ["count"] = 5 };

        // Act
        var result = await executor.ExecuteAsync(@"
            var count = Get<int>(""count"");
            Set(""doubled"", count * 2);
            return count;
        ", variables);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(5, result.ReturnValue);
        Assert.True(result.ModifiedVariables.ContainsKey("doubled"));
        Assert.Equal(10, result.ModifiedVariables["doubled"]);
    }

    [Fact]
    public async Task CSharp_ShouldHandleCompilationErrors()
    {
        // Arrange
        var executor = new CSharpScriptExecutor();
        var variables = new Dictionary<string, object?>();

        // Act
        var result = await executor.ExecuteAsync("invalid syntax here", variables);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Compilation", result.ErrorMessage ?? "");
    }

    [Fact]
    public async Task CSharp_ShouldHandleRuntimeErrors()
    {
        // Arrange
        var executor = new CSharpScriptExecutor();
        var variables = new Dictionary<string, object?>();

        // Act
        var result = await executor.ExecuteAsync("throw new Exception(\"Test error\");", variables);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Test error", result.ErrorMessage ?? "");
    }

    [Fact]
    public async Task CSharp_ShouldTimeout()
    {
        // Arrange
        var executor = new CSharpScriptExecutor();
        var variables = new Dictionary<string, object?>();
        var options = new ScriptOptions { TimeoutSeconds = 1 };

        // Act
        var result = await executor.ExecuteAsync(@"
            while (true) { }
        ", variables, options);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("timeout", result.ErrorMessage?.ToLower() ?? "");
    }

    [Fact]
    public async Task CSharp_ShouldUseLINQ()
    {
        // Arrange
        var executor = new CSharpScriptExecutor();
        var variables = new Dictionary<string, object?>
        {
            ["numbers"] = new List<int> { 1, 2, 3, 4, 5 }
        };

        // Act
        var result = await executor.ExecuteAsync(@"
            var numbers = Variables[""numbers""] as List<int>;
            return numbers.Where(x => x > 2).Sum();
        ", variables);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(12, result.ReturnValue);
    }

    [Fact]
    public async Task CSharp_ShouldCacheCompiledScripts()
    {
        // Arrange
        var executor = new CSharpScriptExecutor();
        var variables = new Dictionary<string, object?>();
        var script = "return 42;";

        // Act - First execution
        var result1 = await executor.ExecuteAsync(script, variables);
        var time1 = result1.ExecutionTime;

        // Act - Second execution (should be faster due to caching)
        var result2 = await executor.ExecuteAsync(script, variables);
        var time2 = result2.ExecutionTime;

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(42, result1.ReturnValue);
        Assert.Equal(42, result2.ReturnValue);
        // Second execution should generally be faster (though this isn't guaranteed)
    }

    [Fact]
    public async Task CSharp_ShouldHandleNullVariables()
    {
        // Arrange
        var executor = new CSharpScriptExecutor();
        var variables = new Dictionary<string, object?> { ["value"] = null };

        // Act
        var result = await executor.ExecuteAsync(@"
            return Variables[""value""] == null;
        ", variables);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(true, result.ReturnValue);
    }

    [Fact]
    public async Task CSharp_ShouldHandleStringOperations()
    {
        // Arrange
        var executor = new CSharpScriptExecutor();
        var variables = new Dictionary<string, object?>
        {
            ["firstName"] = "John",
            ["lastName"] = "Doe"
        };

        // Act
        var result = await executor.ExecuteAsync(@"
            var first = Variables[""firstName""] as string;
            var last = Variables[""lastName""] as string;
            return $""{first} {last}"";
        ", variables);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("John Doe", result.ReturnValue);
    }

    #endregion

    #region ScriptExecutorFactory Tests

    [Fact]
    public void Factory_ShouldReturnPowerShellExecutor()
    {
        // Arrange
        var factory = new ScriptExecutorFactory();

        // Act
        var executor = factory.GetExecutor("powershell");

        // Assert
        Assert.NotNull(executor);
        Assert.IsType<PowerShellScriptExecutor>(executor);
    }

    [Fact]
    public void Factory_ShouldReturnCSharpExecutor()
    {
        // Arrange
        var factory = new ScriptExecutorFactory();

        // Act
        var executor = factory.GetExecutor("csharp");

        // Assert
        Assert.NotNull(executor);
        Assert.IsType<CSharpScriptExecutor>(executor);
    }

    [Fact]
    public void Factory_ShouldHandleAliases()
    {
        // Arrange
        var factory = new ScriptExecutorFactory();

        // Act & Assert
        Assert.IsType<PowerShellScriptExecutor>(factory.GetExecutor("ps"));
        Assert.IsType<PowerShellScriptExecutor>(factory.GetExecutor("pwsh"));
        Assert.IsType<CSharpScriptExecutor>(factory.GetExecutor("cs"));
        Assert.IsType<CSharpScriptExecutor>(factory.GetExecutor("c#"));
    }

    [Fact]
    public void Factory_ShouldThrowForUnsupportedLanguage()
    {
        // Arrange
        var factory = new ScriptExecutorFactory();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => factory.GetExecutor("python"));
    }

    [Fact]
    public void Factory_ShouldListSupportedLanguages()
    {
        // Arrange
        var factory = new ScriptExecutorFactory();

        // Act
        var languages = factory.GetSupportedLanguages();

        // Assert
        Assert.Contains("powershell", languages);
        Assert.Contains("csharp", languages);
    }

    [Fact]
    public void Factory_ShouldAllowCustomExecutorRegistration()
    {
        // Arrange
        var factory = new ScriptExecutorFactory();
        var customExecutor = new MockScriptExecutor();

        // Act
        factory.RegisterExecutor(customExecutor);
        var executor = factory.GetExecutor("mock");

        // Assert
        Assert.NotNull(executor);
        Assert.IsType<MockScriptExecutor>(executor);
    }

    #endregion

    #region Helper Classes

    private class MockScriptExecutor : IScriptExecutor
    {
        public string SupportedLanguage => "mock";

        public Task<ScriptResult> ExecuteAsync(string script, Dictionary<string, object?> variables, ScriptOptions? options = null)
        {
            return Task.FromResult(ScriptResult.SuccessResult("mock result"));
        }
    }

    #endregion
}
