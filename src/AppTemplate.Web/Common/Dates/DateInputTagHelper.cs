using AppTemplate.Application.Common;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace AppTemplate.Web.Common.Dates;

/// <summary>
/// Makes every <c>&lt;input asp-for="SomeDate"&gt;</c> follow the standard without anyone remembering to:
/// MVC would render a DateOnly as type="date" with value 2026-11-19 (a format the binder rejects).
/// This runs after MVC's own InputTagHelper and turns it into a text box with the value in
/// dd-MMM-yyyy and the date picker attached.
/// </summary>
[HtmlTargetElement("input", Attributes = "asp-for")]
public sealed class DateInputTagHelper : TagHelper
{
    // MVC's InputTagHelper runs at -1000. A higher number runs later and sees its output.
    public override int Order => 1000;

    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = null!;

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var type = Nullable.GetUnderlyingType(For.ModelExplorer.ModelType) ?? For.ModelExplorer.ModelType;
        if (type != typeof(DateOnly) || output.Attributes.ContainsName("data-native-date"))
        {
            return;
        }

        output.Attributes.SetAttribute("type", "text");
        output.Attributes.SetAttribute("data-datepicker", string.Empty);
        output.Attributes.SetAttribute("autocomplete", "off");
        output.Attributes.SetAttribute("placeholder", DateFormat.Date);

        // After a failed post, show what the user typed, not a reformatted value.
        var attempted = ViewContext.ViewData.ModelState[For.Name]?.AttemptedValue;
        var value = attempted ?? (For.Model is DateOnly date ? date.ToDisplay() : string.Empty);
        output.Attributes.SetAttribute("value", value);
    }
}
