using MudBlazor;

namespace SampleApp.UI1.Utility;

public static class ColorThemes
{
    public readonly static MudTheme Theme1 = new()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#F3F3F3",
            TextPrimary = "#151515",
            Secondary = "#010961",
            DrawerIcon = "#F3F3F3"
            //Primary = "#007BFF",
            //Secondary = "#6C757D",
            //Background = "#EDEDED", // Less bright light gray
            //Surface = "#E0E0E0", // Soft contrast to Background
            //AppbarBackground = "#3A50F2",
            //DrawerBackground = "#010961", // Improved contrast for drawer
            //DrawerText = "#F1F1F1",
            //Success = "#28A745",
            //Warning = "#FFC107",
            //Error = "#DC3545",
            //Info = "#17A2B8",
            //TextPrimary = "#212529",
            //TextSecondary = "#343A40" // Darkened for better readability
        },
        PaletteDark = new PaletteDark()
        {
            Primary = "#151515",
            TextPrimary = "#F3F3F3",
            Secondary = "#010961",
            DrawerIcon = "#F3F3F3" 
            //Primary = "#ADADFD",
            //Secondary = "#5D557D",
            //Background = "#0E0E0E", // Darker for contrast
            //Surface = "#25272A", // Slightly lighter for differentiation
            //AppbarBackground = "#3A50F2",
            //DrawerBackground = "#010961", // Better distinction
            //DrawerText = "#F1F1F1", // Slightly brighter for better readability
            //Success = "#198754",
            //Warning = "#FFC107",
            //Error = "#DC3545",
            //Info = "#0DCAF0",
            //TextPrimary = "#F5F5F5", // Lighter text for clarity
            //TextSecondary = "#B0B8C2" // More readable contrast
        },
        LayoutProperties = new()
        {
            DefaultBorderRadius = "4px"
        }
    };
}
