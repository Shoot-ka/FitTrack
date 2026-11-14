using FitTrack.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FitTrack.Controllers;

public sealed class CatalogExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is CatalogException exception)
        {
            context.Result = new ObjectResult(exception.Error) { StatusCode = exception.Error.Status };
            context.ExceptionHandled = true;
        }
    }
}
