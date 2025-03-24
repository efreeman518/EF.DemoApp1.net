using MudBlazor;

namespace SampleApp.UI1.Utility;

public static class ColorThemes
{
    public readonly static MudTheme ThemePastel1 = new()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#007BFF", // Modern blue
            Secondary = "#6C757D", // Muted gray
            Background = "#F1F1F1", // Light grayish background
            Surface = "#EEEEEE", // Clean white surface
            AppbarBackground = "#007BFF", // Blue app bar
            DrawerBackground = "#E9ECEF", // Slightly darker background for contrast
            DrawerText = "#212529", // Dark text for better readability
            Success = "#28A745", // Fresh green
            Warning = "#FFC107", // Bright yellow
            Error = "#DC3545", // Strong red
            Info = "#17A2B8", // Soft cyan
            TextPrimary = "#212529", // Dark gray for readability
            TextSecondary = "#495057" // Softer gray for less prominent text
        },
        PaletteDark = new PaletteDark()
        {
            Primary = "#0D6EFD", // Slightly darker blue for modern look
            Secondary = "#5D557D", // contrast text; drawer background
            Background = "#121212", // Dark background
            Surface = "#1E1E1E", // Dark gray surface
            AppbarBackground = "#212529", // Almost black app bar
            DrawerBackground = "#2D2F33", // ignored?? Dark gray with slight blue tint
            DrawerText = "#F8F9FA", // White text for contrast
            Success = "#198754",
            Warning = "#FFC107",
            Error = "#DC3545",
            Info = "#0DCAF0",
            TextPrimary = "#E1E1E1", // Light text for dark mode
            TextSecondary = "#CED4DA" // Softer light gray for contrast
        },
        LayoutProperties = new()
        {
            DefaultBorderRadius = "4px"
        }
        //Shadows = new Shadow()
        //{
        //    Elevation =
        //    [
        //        "none",
        //        "0px 1px 3px rgba(0, 0, 0, 0.2)",
        //        "0px 3px 6px rgba(0, 0, 0, 0.16)",
        //        "0px 6px 12px rgba(0, 0, 0, 0.15)",
        //        "0px 10px 20px rgba(0, 0, 0, 0.14)"
        //    ]
        //}
    };
}
