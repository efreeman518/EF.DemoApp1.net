# Test.PlaywrightUI - Playwright MSTest Integration

This project contains end-to-end UI tests using Playwright with MSTest integration.

## Migration from SpecFlow

This project was migrated from SpecFlow to pure Playwright MSTest implementation because:
- SpecFlow is deprecated and no longer actively maintained
- SpecFlow has compatibility issues with MSTest v4 and .NET 9
- Playwright's MSTest integration provides excellent testing capabilities without the overhead of SpecFlow

## Test Structure

### Before (SpecFlow)
```
Test.PlaywrightUI/
??? Features/
?   ??? TodoItemCRUD.feature
??? Steps/
?   ??? TodoItemCrudSteps.cs
??? Hooks/
?   ??? TestHooks.cs
??? PageObjects/
    ??? BasePageObject.cs
    ??? MainPageObject.cs
```

### After (Playwright MSTest)
```
Test.PlaywrightUI/
??? Tests/
?   ??? TodoItemCrudTests.cs
??? PageObjects/
    ??? BasePageObject.cs (kept for reference)
    ??? MainPageObject.cs (kept for reference)
```

## Key Benefits

1. **Native Playwright Integration**: Uses `Microsoft.Playwright.MSTest` package which provides the `PageTest` base class
2. **Simplified Test Structure**: Tests are written directly in C# without the need for Gherkin feature files
3. **Better IDE Support**: Full IntelliSense and debugging support
4. **Modern .NET Compatibility**: Works seamlessly with .NET 9 and MSTest v4

## Running Tests

### Command Line
```bash
dotnet test Test.PlaywrightUI/Test.PlaywrightUI.csproj
```

### Visual Studio
- Open Test Explorer (Test ? Test Explorer)
- Build the solution
- Tests will appear in Test Explorer
- Right-click and select "Run" or "Debug"

## Test Examples

### Data-Driven Tests
The tests use MSTest's `[DataRow]` attribute for parameterized testing:

```csharp
[TestMethod]
[DataRow("item1a", "123")]
[DataRow("item2a", "321")]
public async Task TodoItem_AddEditDelete_Success(string todoItemName, string appendName)
{
    // Test implementation
}
```

### Browser Configuration
Browser options can be configured by overriding the `ContextOptions()` method:

```csharp
public override BrowserNewContextOptions ContextOptions()
{
    return new BrowserNewContextOptions()
    {
        IgnoreHTTPSErrors = true,
        // Other options...
    };
}
```

## Test Organization

Each test follows the **Arrange-Act-Assert** pattern with clear comments:

```csharp
// Arrange - Generate unique item name
_currentItemName = $"{todoItemName}-{DateTime.UtcNow.Ticks}";

// Act - Add new item
await Page.FillAsync("#edit-name", _currentItemName);
await Page.ClickAsync("#btn-save");

// Assert - Verify item exists
var itemInGrid = await Page.WaitForSelectorAsync(...);
Assert.IsNotNull(itemInGrid);
```

## Package References

The project uses the following packages (managed via Central Package Management):
- `Microsoft.NET.Test.Sdk` - Test platform
- `Microsoft.Playwright.MSTest` - Playwright integration with MSTest
- `MSTest.TestAdapter` - MSTest adapter
- `MSTest.TestFramework` - MSTest framework
- `coverlet.collector` - Code coverage

## Notes

- The `PageObjects` classes (`BasePageObject.cs` and `MainPageObject.cs`) have been kept for reference but are not used in the new test implementation
- Tests now inherit from `PageTest` which provides the `Page` property directly
- Browser lifecycle is managed automatically by the Playwright MSTest integration
- Tests can be run in parallel by default (use `[DoNotParallelize]` attribute if needed)

## Further Reading

- [Playwright .NET Documentation](https://playwright.dev/dotnet/)
- [MSTest Documentation](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-intro)
- [Playwright MSTest Integration](https://playwright.dev/dotnet/docs/test-runners#mstest)
