using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LLamaWebAPI.Core.Middleware
{
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        private readonly ILogger _logger;

        public ValidateModelAttribute(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("Security");
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var actionName = context.ActionDescriptor.DisplayName;
                var errors = string.Join("; ", context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                _logger.LogWarning("Invalid model state for {Action}: {Errors}", actionName, errors);

                context.Result = new BadRequestObjectResult(context.ModelState);
            }
        }
    }
}
