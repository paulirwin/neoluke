using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using System;
using Microsoft.Extensions.Logging;

namespace NeoLuke.Views.Controls;

public partial class ThemedIcon : UserControl
{
    private static readonly ILogger _logger = Program.LoggerFactory.CreateLogger<ThemedIcon>();

    public static readonly StyledProperty<string?> IconPathProperty =
        AvaloniaProperty.Register<ThemedIcon, string?>(nameof(IconPath));

    public static readonly StyledProperty<double> IconWidthProperty =
        AvaloniaProperty.Register<ThemedIcon, double>(nameof(IconWidth), 16.0);

    public static readonly StyledProperty<double> IconHeightProperty =
        AvaloniaProperty.Register<ThemedIcon, double>(nameof(IconHeight), 16.0);

    public string? IconPath
    {
        get => GetValue(IconPathProperty);
        set => SetValue(IconPathProperty, value);
    }

    public double IconWidth
    {
        get => GetValue(IconWidthProperty);
        set => SetValue(IconWidthProperty, value);
    }

    public double IconHeight
    {
        get => GetValue(IconHeightProperty);
        set => SetValue(IconHeightProperty, value);
    }

    public ThemedIcon()
    {
        InitializeComponent();

        // Subscribe to property changes
        IconPathProperty.Changed.AddClassHandler<ThemedIcon>((x, e) => x.OnIconPathChanged());
        IconWidthProperty.Changed.AddClassHandler<ThemedIcon>((x, e) => x.UpdateSvgSize());
        IconHeightProperty.Changed.AddClassHandler<ThemedIcon>((x, e) => x.UpdateSvgSize());

        // Subscribe to theme changes
        ActualThemeVariantChanged += OnThemeChanged;

        // Set initial state when loaded
        Loaded += (s, e) =>
        {
            OnIconPathChanged();
            UpdateSvgSize();
            UpdateIconColor();
        };
    }

    private void OnIconPathChanged()
    {
        if (SvgIcon != null && IconPath != null)
        {
            var pathProperty = SvgIcon.GetType().GetProperty("Path");
            pathProperty?.SetValue(SvgIcon, IconPath);
            UpdateIconColor();
        }
    }

    private void UpdateSvgSize()
    {
        if (SvgIcon != null)
        {
            SvgIcon.Width = IconWidth;
            SvgIcon.Height = IconHeight;
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateIconColor();
    }

    private void UpdateIconColor()
    {
        if (SvgIcon == null)
        {
            _logger.LogWarning("SvgIcon is null, cannot update color");
            return;
        }

        var currentTheme = ActualThemeVariant;
        var isDark = currentTheme == ThemeVariant.Dark;
        var fillColor = isDark ? "#FFFFFF" : "#000000";
        var css = $"path {{ fill: {fillColor}; }}";

        // Try using AvaloniaProperty approach
        try
        {
            var cssPropertyField = SvgIcon.GetType().GetField("CssProperty",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.FlattenHierarchy);

            if (cssPropertyField != null)
            {
                var avaloniaProperty = cssPropertyField.GetValue(null) as AvaloniaProperty;
                if (avaloniaProperty != null)
                {
                    SvgIcon.SetValue(avaloniaProperty, css);

                    // Force reload by re-setting the Path
                    var pathPropertyField = SvgIcon.GetType().GetField("PathProperty",
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.Static |
                        System.Reflection.BindingFlags.FlattenHierarchy);

                    if (pathPropertyField != null)
                    {
                        var pathAvaloniaProperty = pathPropertyField.GetValue(null) as AvaloniaProperty;
                        if (pathAvaloniaProperty != null)
                        {
                            var currentPath = SvgIcon.GetValue(pathAvaloniaProperty);
                            SvgIcon.SetValue(pathAvaloniaProperty, null);
                            SvgIcon.SetValue(pathAvaloniaProperty, currentPath);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("CssProperty field found but is not an AvaloniaProperty");
                }
            }
            else
            {
                _logger.LogWarning("CssProperty field not found on Svg type");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating SVG icon CSS");
        }
    }
}
