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
    public async Task<bool> GridItemExists(string? v) => (await Page.Locator($"//tbody[@id='todos']/tr/td[contains(text(),\"{v}\")]").CountAsync()) > 0;
    public async Task<bool> GridItemDoesNotExist(string? v) => (await Page.Locator($"//tbody[@id='todos']/tr/td[contains(text(),\"{v}\")]").CountAsync()) == 0;
    public async Task<bool> GridRowCompleteBoxCheckedValue(string? v) => await Page.Locator($"//tbody[@id='todos']/tr[td[contains(text(),\"{v}\")]]/td/input[@type='checkbox']").IsCheckedAsync(); //.GetAttributeAsync("checked");

}

