# MapDynamicPageRoute doesn't work with Index routes

This repository is to demonstrate a bug in ASP.NET Core 6 routing when using a `DynamicRouteValueTransformer`

To reproduce:

1. Create a new Razor Pages project

2. Create this `DynamicRouteValueTransformer`

```
public class RouteTransformer : DynamicRouteValueTransformer
{   
    public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
    {   
        values["page"] = "/Index";
        return ValueTask.FromResult(values);
    }
}

```

3. Register it in Program.cs
```
app.MapDynamicPageRoute<RouteTransformer>("{**dynamicRoute}");
```

4. Visit a URL that would get routed by the DynamicRouteValueTransformer (like `/foobar`) and it will say that there are two routes for `/Index`

```
AmbiguousMatchException: The request matched multiple endpoints. Matches:

/Index
/Index
Microsoft.AspNetCore.Routing.Matching.DefaultEndpointSelector.ReportAmbiguity(CandidateState[] candidateState)
```

**This only impacts Index routing (and only Razor Pages, not MVC)**

For example, change `RouteTransformer` to have `values["page"] = "/Privacy";` and it will route `/foobar` to `/Privacy` and the Privacy Policy page will show correctly. If you move the files to `/Home/Index` and `/Home/Privacy` and set `values["page"] = "/Home/Index";`, you'll get the same `AmbiguousMatchException`.

------

I don't know enough about ASP.NET's routing internals so the following might be wrong.

It seems like we're getting two routes for `/Index` rather than a route for `/Index` and a route for `/`. For something like `/Privacy.cshtml` there's only one path, but for something like `/Index.cshtml` there are two paths.

When applying the `DynamicPageEndpointMatcherPolicy` and specifically `selector.SelectEndpoints`, we end up matching two routes because `ActionSelectionTable<Endpoint> Table` has two entries for `new string[] {"/Index"}` via `ActionSelectionTable.Select` when it does `OrdinalEntries.TryGetValue`. This gets populated off the `ActionDescriptor.RouteValues` via `ActionSelectionTable.Create` (see the lambda in there). **tl;dr: We have two entries that have a `RouteValues` that match `/Index`.**

So where does this happen? It looks like [PageActionDescriptorProvider](https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.RazorPages/src/Infrastructure/PageActionDescriptorProvider.cs) takes the `/Index` route with two selectors and sets two `Index` routes in `AddActionDescriptors` ([one for each selector](https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.RazorPages/src/Infrastructure/PageActionDescriptorProvider.cs#L110)). The problem is that it uses `model.RouteValues` for each selector and so it gets two `/Index` routes rather than a `/Index` and `/`.

So how should this be fixed? I'm not sure. **It seems like we need something like `TransformPageRoute` that takes into consideration the `selector`** (but as I noted, I don't know enough about ASP.NET's routing internals to speak well on this topic).

Maybe:

```
var pageRouteMetadata = selectorModel.EndpointMetadata.OfType<PageRouteMetadata>().SingleOrDefault();
if (pageRouteMetadata != null)
{
    descriptor.RouteValues.Add("page", pageRouteMetadata.PageRoute);
}

```

But this is where I'm running into a wall since Rider doesn't seem to want to open the `aspnetcore` repository on my Apple Silocon Mac so I can't really test this.

There's a [test](https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.RazorPages/test/Infrastructure/PageActionDescriptorProviderTest.cs#L332-L339) which seems to think the `descriptor.RouteValues["page"]` should have `/Index` for both descriptors, despite the `descriptor.AttributeRouteInfo.Template` only having `/Index` in one of the two. So I could simply be wrong about the RouteValues.

I can use an `IPageRouteModelConvention` to remove the `/Index` selector and reset the model.RouteValues["page"] to exclude `/Index`. I'm not sure I'd ever want to route to `/People/Index` instead of just `/People`. A demo of this is shown at https://github.com/lilahtovmoon/RouterBug/tree/customPageRouteModelConvention
