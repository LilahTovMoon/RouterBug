using Microsoft.AspNetCore.Mvc.Routing;

namespace RouterBug.Routing;

public class RouteTransformer : DynamicRouteValueTransformer
{
    public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
    {
        values["page"] = "/Index";
        return ValueTask.FromResult(values);
    }
}