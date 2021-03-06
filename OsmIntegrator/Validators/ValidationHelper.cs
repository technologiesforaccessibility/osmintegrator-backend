using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OsmIntegrator.ApiModels.Errors;

namespace OsmIntegrator.Validators
{
    public interface IModelValidator
    {
        Error Validate(ModelStateDictionary modelState);
    }

    public class ModelValidator: IModelValidator
    {
        public Error Validate(ModelStateDictionary modelState)
        {
            if (!modelState.IsValid)
            {
                List<string> errors = new List<string>();
                foreach (var modalError in modelState.Values.SelectMany(v => v.Errors))
                {
                    errors.Add(modalError.ErrorMessage);
                }

                Error error = new ValidationError() { Message = String.Join(",", errors) };
                return error;
            }
            return null;
        }
    }
}