using Microsoft.AspNetCore.Mvc.Routing;

namespace RouterBug.Routing;

public class RouteTransformer : DynamicRouteValueTransformer
{
    public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
    {
        values["page"] = "/About";
        return ValueTask.FromResult(values);
    }
}