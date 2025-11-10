using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

// Enable parallel execution: adjust Workers to the desired concurrency
[assembly: Parallelize(Workers = 4, Scope = ExecutionScope.MethodLevel)]

namespace Test.PlaywrightUI.Tests;

[TestClass]
public class TodoItemCrudTests : PageTest
{
    private const string BaseUrl = "https://localhost:44318/";

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions()
        {
            IgnoreHTTPSErrors = true,
            //ViewportSize = new() { Width = 1920, Height = 1080 }
        };
    }

    [TestInitialize]
    public async Task TestInitialize()
    {
        await Page.GotoAsync(BaseUrl);
    }

    [TestMethod]
    [DataRow("item1a", "123")]
    [DataRow("item2a", "321")]
    public async Task TodoItem_AddEditDelete_Success(string todoItemName, string appendName)
    {
        // Arrange - Generate unique item name
        var _currentItemName = $"{todoItemName}-{DateTime.UtcNow.Ticks}";

        // Act & Assert - Add new item
        await Page.FillAsync("#edit-name", _currentItemName);
        await Page.ClickAsync("#btn-save");

        // Verify item exists in grid
        var itemInGrid = await Page.WaitForSelectorAsync($"//tbody[@id='todos']/tr/td[contains(text(),\"{_currentItemName}\")]");
        Assert.IsNotNull(itemInGrid, "Item should exist in grid after creation");

        // Act - Click edit button
        await Page.ClickAsync($"//tbody[@id='todos']/tr[td[contains(text(),\"{_currentItemName}\")]]//button[text()='Edit']");

        // Assert - Verify edit area shows the item
        var editNameValue = await Page.Locator("#edit-name").InputValueAsync();
        Assert.AreEqual(_currentItemName, editNameValue, "Edit area should show the correct item");

        // Act - Append name and update
        _currentItemName = _currentItemName + appendName;
        await Page.FillAsync("#edit-name", _currentItemName);
        await Page.ClickAsync("#btn-save");

        // Assert - Verify updated item exists in grid
        itemInGrid = await Page.WaitForSelectorAsync($"//tbody[@id='todos']/tr/td[contains(text(),\"{_currentItemName}\")]");
        Assert.IsNotNull(itemInGrid, "Updated item should exist in grid");

        // Act - Click edit button again
        await Page.ClickAsync($"//tbody[@id='todos']/tr[td[contains(text(),\"{_currentItemName}\")]]//button[text()='Edit']");

        // Assert - Verify edit area shows updated item
        editNameValue = await Page.Locator("#edit-name").InputValueAsync();
        Assert.AreEqual(_currentItemName, editNameValue, "Edit area should show the updated item");

        // Act - Mark as complete and update
        await Page.CheckAsync("#edit-isComplete");
        await Page.ClickAsync("#btn-save");

        // Assert - Verify complete checkbox is checked in grid
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var completeCheckbox = await Page.WaitForSelectorAsync($"//tbody[@id='todos']/tr[td[contains(text(),\"{_currentItemName}\")]]/td/input[@type='checkbox']");
        var isChecked = await completeCheckbox!.IsCheckedAsync();
        Assert.IsTrue(isChecked, "Complete checkbox should be checked in grid");

        // Act - Delete the item
        await Page.ClickAsync($"//tbody[@id='todos']/tr[td[contains(text(),\"{_currentItemName}\")]]//button[text()='Delete']");

        // Assert - Verify item no longer exists in grid
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(1000);

        bool itemRemoved = false;
        try
        {
            await Page.WaitForSelectorAsync($"//tbody[@id='todos']/tr/td[contains(text(),\"{_currentItemName}\")]",
                new PageWaitForSelectorOptions { Timeout = 1000 });
        }
        catch
        {
            itemRemoved = true;
        }

        Assert.IsTrue(itemRemoved, "Item should no longer exist in grid after deletion");
    }

    [TestMethod]
    public async Task TodoItem_AddNewItem_Success()
    {
        // Arrange
        var _currentItemName = $"SingleItem-{DateTime.UtcNow.Ticks}";

        // Act
        await Page.FillAsync("#edit-name", _currentItemName);
        await Page.ClickAsync("#btn-save");

        // Assert
        var itemInGrid = await Page.WaitForSelectorAsync($"//tbody[@id='todos']/tr/td[contains(text(),\"{_currentItemName}\")]");
        Assert.IsNotNull(itemInGrid, "Item should exist in grid after creation");

        // Cleanup
        await Page.ClickAsync($"//tbody[@id='todos']/tr[td[contains(text(),\"{_currentItemName}\")]]//button[text()='Delete']");
    }

    [TestMethod]
    public async Task TodoItem_EditExistingItem_Success()
    {
        // Arrange
        var _currentItemName = $"EditTest-{DateTime.UtcNow.Ticks}";
        await Page.FillAsync("#edit-name", _currentItemName);
        await Page.ClickAsync("#btn-save");
        await Page.WaitForSelectorAsync($"//tbody[@id='todos']/tr/td[contains(text(),\"{_currentItemName}\")]");

        // Act - Edit the item
        await Page.ClickAsync($"//tbody[@id='todos']/tr[td[contains(text(),\"{_currentItemName}\")]]//button[text()='Edit']");
        _currentItemName = _currentItemName + "-Updated";
        await Page.FillAsync("#edit-name", _currentItemName);
        await Page.ClickAsync("#btn-save");

        // Assert
        var updatedItem = await Page.WaitForSelectorAsync($"//tbody[@id='todos']/tr/td[contains(text(),\"{_currentItemName}\")]");
        Assert.IsNotNull(updatedItem, "Updated item should exist in grid");

        // Cleanup
        await Page.ClickAsync($"//tbody[@id='todos']/tr[td[contains(text(),\"{_currentItemName}\")]]//button[text()='Delete']");
    }
}
