using Microsoft.Playwright;

namespace Test.PlaywrightUI.PageObjects;

public class MainPageObject(IBrowser browser) : BasePageObject
{
    public override string PagePath => "https://localhost:44318/";
    public override IPage Page { get; set; } = null!;
    public override IBrowser Browser { get; set; } = browser;

    public Task ClickSaveButton() => Page.ClickAsync("#btn-save");
    public Task ClickEditButton(string? v) => Page.Locator($"//tbody[@id='todos']/tr[td[contains(text(),\"{v}\")]]//button[text()='Edit']").ClickAsync();
    public Task ClickCompleteCheckbox() => Page.Locator("#edit-isComplete").CheckAsync(); //.ClickAsync();
    public Task ClickDeleteButton(string? v) => Page.Locator($"//tbody[@id='todos']/tr[td[contains(text(),\"{v}\")]]//button[text()='Delete']").ClickAsync();

    public Task EnterName(string v) => Page.FillAsync("#edit-name", v);
    public Task EnterSecureRandom(string v) => Page.FillAsync("#edit-secure-random", v);
    public Task EnterSecureDeterminitstic(string v) => Page.FillAsync("#edit-secure-deterministic", v);

    public async Task<bool> EditAreaShowsItem(string? v) => (await Page.Locator("#edit-name").InputValueAsync()) == v;

    //Checking the grid occurs after an xhr request, so we need to wait for the grid to be updated using WaitForSelectorAsync instead of Locator which occurs immediately
    public async Task<bool> GridItemExists(string? v) => (await Page.WaitForSelectorAsync($"//tbody[@id='todos']/tr/td[contains(text(),\"{v}\")]")) != null;
    public async Task<bool> GridItemDoesNotExist(string? v)
    {
        //give the xhr call 2 seconds to complete; expect exception since the item should not exist 
        try
        {
            await Page.WaitForTimeoutAsync(3000);
            var selector = $"//tbody[@id='todos']/tr/td[contains(text(),\"{v}\")]";
            //
            //since page is re-creating the DOM, need a selector?
            _ = Page.Locator(selector);
            await Page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions { Timeout = 3000 });
            return false; //item exists
        }
        catch
        {
            return true; //exception thrown, item does not exist
        }
    }

    public async Task<bool> GridRowCompleteBoxCheckedValue(string? v)
    {
        var selector = $"//tbody[@id='todos']/tr[td[contains(text(),\"{v}\")]]/td/input[@type='checkbox']";

        //ajax so need to wait; these 2 lines seem to make the test run reliably
        await Page.WaitForTimeoutAsync(1000); 
        

        _ = Page.Locator(selector);
        //return await locator.IsCheckedAsync();
        var elh = await Page.WaitForSelectorAsync(selector);
        return await elh!.IsCheckedAsync();
    }

    //public async Task<bool> GridItemExists(string? v) => (await Page.Locator($"//tbody[@id='todos']/tr/td[contains(text(),\"{v}\")]").CountAsync()) > 0;
    //public async Task<bool> GridItemDoesNotExist(string? v) => (await Page.Locator($"//tbody[@id='todos']/tr/td[contains(text(),\"{v}\")]").CountAsync()) == 0;
    //public async Task<bool> GridRowCompleteBoxCheckedValue(string? v) => 
    //    await Page.Locator($"//tbody[@id='todos']/tr[td[contains(text(),\"{v}\")]]/td/input[@type='checkbox']").IsCheckedAsync(); //.GetAttributeAsync("checked");


}

