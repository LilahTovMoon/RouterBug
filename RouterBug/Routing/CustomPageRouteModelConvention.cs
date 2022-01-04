using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace RouterBug.Routing;

public class CustomPageRouteModelConvention : IPageRouteModelConvention
{
    public void Apply(PageRouteModel model)
    {
        var page = model.RouteValues["page"];
        if (page.EndsWith("/Index"))
        {
            model.RouteValues["page"] = page.Remove(page.Length - 6);
            var toRemove = new List<int>();
            for (var i = model.Selectors.Count - 1; i >= 0; i--)
            {
                var pageRouteMetadata = model.Selectors[i].EndpointMetadata.OfType<PageRouteMetadata>().SingleOrDefault();
                if (pageRouteMetadata != null)
                {
                    if (pageRouteMetadata.PageRoute.EndsWith("Index"))
                    {
                        toRemove.Add(i);
                    }
                }
            }
            foreach (var i in toRemove)
            {
                model.Selectors.RemoveAt(i);
            }
        }
    }
}