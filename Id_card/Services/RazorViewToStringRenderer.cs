using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Id_card.Services
{
    public class RazorViewToStringRenderer
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public RazorViewToStringRenderer(IRazorViewEngine viewEngine, ITempDataProvider tempDataProvider, IServiceProvider serviceProvider)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task<string> RenderViewToStringAsync(Controller controller, string viewName, object model)
        {
            var actionContext = new ActionContext(controller.HttpContext, controller.RouteData, controller.ControllerContext.ActionDescriptor);

            // Try standard FindView first
            var viewEngineResult = _viewEngine.FindView(actionContext, viewName, false);
            if (!viewEngineResult.Success)
            {
                // Try a series of candidate paths to be resilient across environments
                var candidates = new List<string>();
                candidates.Add(viewName);
                candidates.Add(viewName.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase) ? viewName : viewName + ".cshtml");
                var basePath = viewName.TrimStart('~').TrimStart('/');
                if (!basePath.StartsWith("Views/", StringComparison.OrdinalIgnoreCase))
                {
                    basePath = "Views/" + basePath;
                }
                candidates.Add("~/" + basePath);
                candidates.Add("~/" + (basePath.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase) ? basePath : basePath + ".cshtml"));
                candidates.Add("/" + basePath);
                candidates.Add("/" + (basePath.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase) ? basePath : basePath + ".cshtml"));

                IView? foundView = null;
                foreach (var cand in candidates)
                {
                    var tryGet = _viewEngine.GetView(executingFilePath: null, viewPath: cand, isMainPage: true);
                    if (tryGet.Success)
                    {
                        foundView = tryGet.View;
                        viewEngineResult = tryGet;
                        break;
                    }
                }
                if (foundView == null)
                {
                    throw new InvalidOperationException($"View '{viewName}' not found.");
                }
            }

            var view = viewEngineResult.View;
            await using var sw = new StringWriter();
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model
            };

            var tempData = new TempDataDictionary(controller.HttpContext, _tempDataProvider);
            var viewContext = new ViewContext(actionContext, view, viewData, tempData, sw, new HtmlHelperOptions());

            await view.RenderAsync(viewContext);
            return sw.ToString();
        }

        public async Task<string> RenderViewToStringAsync(Controller controller, string viewName, object model, IDictionary<string, object>? additionalViewData)
        {
            var actionContext = new ActionContext(controller.HttpContext, controller.RouteData, controller.ControllerContext.ActionDescriptor);

            // Try standard FindView first
            var viewEngineResult = _viewEngine.FindView(actionContext, viewName, false);
            if (!viewEngineResult.Success)
            {
                var candidates = new List<string>();
                candidates.Add(viewName);
                candidates.Add(viewName.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase) ? viewName : viewName + ".cshtml");
                var basePath = viewName.TrimStart('~').TrimStart('/');
                if (!basePath.StartsWith("Views/", StringComparison.OrdinalIgnoreCase))
                {
                    basePath = "Views/" + basePath;
                }
                candidates.Add("~/" + basePath);
                candidates.Add("~/" + (basePath.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase) ? basePath : basePath + ".cshtml"));
                candidates.Add("/" + basePath);
                candidates.Add("/" + (basePath.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase) ? basePath : basePath + ".cshtml"));

                IView? foundView = null;
                foreach (var cand in candidates)
                {
                    var tryGet = _viewEngine.GetView(executingFilePath: null, viewPath: cand, isMainPage: true);
                    if (tryGet.Success)
                    {
                        foundView = tryGet.View;
                        viewEngineResult = tryGet;
                        break;
                    }
                }
                if (foundView == null)
                {
                    throw new InvalidOperationException($"View '{viewName}' not found.");
                }
            }

            var view = viewEngineResult.View;
            await using var sw = new StringWriter();
            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model
            };
            if (additionalViewData != null)
            {
                foreach (var kvp in additionalViewData)
                {
                    viewData[kvp.Key] = kvp.Value;
                }
            }

            var tempData = new TempDataDictionary(controller.HttpContext, _tempDataProvider);
            var viewContext = new ViewContext(actionContext, view, viewData, tempData, sw, new HtmlHelperOptions());

            await view.RenderAsync(viewContext);
            return sw.ToString();
        }
    }
}


