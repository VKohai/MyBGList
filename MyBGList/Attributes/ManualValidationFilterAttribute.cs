using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace MyBGList.Attributes;

public class ManualValidationFilterAttribute : Attribute, IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        for (var i = 0; i < action.Filters.Count; ++i)
        {
            if (action.Filters[i] is ModelStateInvalidFilter ||
                action.Filters[i].GetType().Name == "ModelStateInvalidFilterFactory")
            {
                action.Filters.RemoveAt(i);
                break;
            }
        }
    }
}