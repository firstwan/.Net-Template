using GDBAPI.Domain.Constants;
using GDBAPI.DtoModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using System.Linq;

namespace GDBAPI.ActionFilters
{
    public class ValidationActionFilter : IActionFilter
    {
        /// <summary>
        /// This action filter will run after validate the input, if the validation failed,
        /// we generate our custom Bad request response here.
        /// 
        /// Microsoft doc: https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/filters?view=aspnetcore-5.0#action-filters
        /// </summary>
        /// <param name="context"></param>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState.Where(x => x.Value.Errors.Count() > 0);
                Dictionary<string, ModelErrorCollection> errorMessages = new Dictionary<string, ModelErrorCollection>();

                foreach (var error in errors)
                {
                    errorMessages.Add(error.Key, error.Value.Errors);
                }

                var badRequestResponse = new ErrorResponseDto<Dictionary<string, ModelErrorCollection>>()
                {
                    Code = ErrorCodeConstants.VALIDATION_ERROR,
                    Message = "Invalid data format",
                    Data = errorMessages
                };

                context.Result = new BadRequestObjectResult(badRequestResponse);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
