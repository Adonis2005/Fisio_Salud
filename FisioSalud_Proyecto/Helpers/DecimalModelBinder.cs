using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FisioSalud_Proyecto.Helpers
{
    // Los controles number envían punto; formularios localizados pueden enviar coma.
    public sealed class DecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext context)
        {
            var value = context.ValueProvider.GetValue(context.ModelName);
            if (value == ValueProviderResult.None) return Task.CompletedTask;
            context.ModelState.SetModelValue(context.ModelName, value);
            var text = value.FirstValue?.Trim();
            if (string.IsNullOrEmpty(text) && context.ModelMetadata.IsNullableValueType)
                context.Result = ModelBindingResult.Success(null);
            else if (decimal.TryParse(text?.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number))
                context.Result = ModelBindingResult.Success(number);
            else context.ModelState.TryAddModelError(context.ModelName, "Ingrese un número válido sin separadores de miles.");
            return Task.CompletedTask;
        }
    }
    public sealed class DecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context) =>
            context.Metadata.ModelType == typeof(decimal) || context.Metadata.ModelType == typeof(decimal?) ? new DecimalModelBinder() : null;
    }
}
