using System;
using System.Collections.Generic;
using System.Globalization;
using global::Avalonia;
using global::Avalonia.Data;
using global::Avalonia.Data.Converters;
using global::Avalonia.Markup.Xaml;

namespace Irihi.Lingua;

/// <summary>
/// Avalonia markup extension for format strings with localized arguments.
///
/// Usage in XAML:
/// <code>
/// &lt;TextBlock&gt;
///     &lt;TextBlock.Text&gt;
///         &lt;FormatTranslate FormatKey="{x:Static local:LanguageManager+Keys.Page_Template}"&gt;
///             &lt;TranslateEntry Binding="{Binding #page.Value}" /&gt;
///             &lt;TranslateEntry Key="{x:Static local:LanguageManager+Keys.Greeting_Message}" /&gt;
///         &lt;/FormatTranslate&gt;
///     &lt;/TextBlock.Text&gt;
/// &lt;/TextBlock&gt;
/// </code>
/// </summary>
public sealed class FormatTranslateExtension : MarkupExtension
{
    public LinguaKey? FormatKey { get; set; }
    public IList<TranslateEntry> Items { get; set; } = new List<TranslateEntry>();

    public FormatTranslateExtension() { }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (FormatKey == null)
            return AvaloniaProperty.UnsetValue;

        var converter = new FormatTranslateConverter();
        var formatObservable = FormatKey.Manager.GetObservable(FormatKey.Key);
        var formatSource = new LinguaBindingSource(formatObservable);

        // Build a MultiBinding with the format template and all arguments
        var multiBinding = new MultiBinding
        {
            Converter = converter,
            Mode = BindingMode.OneWay,
        };

        // Bind to the format template
        multiBinding.Bindings.Add(new Binding
        {
            Source = formatSource,
            Path = "Value",
            Mode = BindingMode.OneWay,
        });

        // Add each argument binding
        foreach (var item in Items)
        {
            if (item.Key != null)
            {
                var argObservable = item.Key.Manager.GetObservable(item.Key.Key);
                var argSource = new LinguaBindingSource(argObservable);
                multiBinding.Bindings.Add(new Binding
                {
                    Source = argSource,
                    Path = "Value",
                    Mode = BindingMode.OneWay,
                });
            }
            else if (item.Binding != null)
            {
                multiBinding.Bindings.Add(item.Binding);
            }
        }

        // Return the MultiBinding directly — Avalonia will apply it
        return multiBinding;
    }
}

/// <summary>
/// An entry in a <see cref="FormatTranslateExtension"/> — either a localized key
/// or a standard Avalonia binding.
/// </summary>
public sealed class TranslateEntry
{
    public LinguaKey? Key { get; set; }
    public BindingBase? Binding { get; set; }

    public TranslateEntry() { }
}

/// <summary>
/// Multi-value converter for format strings. The first value is the format template;
/// remaining values are the format arguments.
/// </summary>
public sealed class FormatTranslateConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 0)
            return AvaloniaProperty.UnsetValue;

        var format = values[0] as string;
        if (string.IsNullOrEmpty(format))
            return AvaloniaProperty.UnsetValue;

        var args = new object?[values.Count - 1];
        for (var i = 1; i < values.Count; i++)
            args[i - 1] = values[i];

        return string.Format(format, args);
    }
}
