using AppTemplate.Application.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AppTemplate.Web.Common.Dates;

/// <summary>
/// Binds DateOnly and DateTime (and their nullable forms) from forms, query strings and routes,
/// accepting only the house format. Replaces MVC's default binder, which parses with the
/// server's culture and would read 03/04/2026 as March or April depending on the machine.
/// JSON bodies are handled by <see cref="DateJsonConverters"/>, not by this class.
/// </summary>
public sealed class DateModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (value == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value);
        var text = value.FirstValue;
        var type = bindingContext.ModelMetadata.UnderlyingOrModelType;
        var isNullable = bindingContext.ModelMetadata.IsReferenceOrNullableType;
        var label = bindingContext.ModelMetadata.GetDisplayName();

        if (string.IsNullOrWhiteSpace(text))
        {
            if (isNullable)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }
            else
            {
                // A non-nullable DateOnly would silently become 01-Jan-0001; [Required] cannot catch that.
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, $"{label} is required.");
            }

            return Task.CompletedTask;
        }

        if (type == typeof(DateOnly) && DateFormat.TryParseDate(text, out var date))
        {
            bindingContext.Result = ModelBindingResult.Success(date.Value);
        }
        else if (type == typeof(DateTime) && DateFormat.TryParseDateTime(text, out var dateTime))
        {
            bindingContext.Result = ModelBindingResult.Success(dateTime.Value);
        }
        else
        {
            var expected = type == typeof(DateTime) ? DateFormat.DateTime : DateFormat.Date;
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, $"Enter {label} as {expected}, for example 19-Nov-2026.");
        }

        return Task.CompletedTask;
    }
}

public sealed class DateModelBinderProvider : IModelBinderProvider
{
    private static readonly DateModelBinder Binder = new();

    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var type = context.Metadata.UnderlyingOrModelType;
        return type == typeof(DateOnly) || type == typeof(DateTime) ? Binder : null;
    }
}
