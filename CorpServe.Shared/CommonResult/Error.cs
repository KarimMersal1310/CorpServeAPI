using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_Commerce.Shared.CommonResult
{
    public class Error
    {
        public string Code { get; }
        public string Description { get; }
        public ErrorType Type { get; }
        private Error(string code, string description, ErrorType type)
        {
            Code = code;
            Description = description;
            Type = type;
        }

        // static Factory Methods To Create Error
        public static Error Failure(string Code = "General.Failure", string Description = "A General Failure Has Occurred")
        {
            return new Error(Code, Description, ErrorType.Failure);
        }
        public static Error Validation(string Code = "General.Validation", string Description = "A Validation Failure Has Occurred")
        {
            return new Error(Code, Description, ErrorType.Validation);
        }
        public static Error NotFound(string Code = "General.NotFound", string Description = "A NotFound Failure Has Occurred")
        {
            return new Error(Code, Description, ErrorType.NotFound);
        }
        public static Error Unauthorized(string Code = "General.Unauthorized", string Description = "A Unauthorized Failure Has Occurred")
        {
            return new Error(Code, Description, ErrorType.Unauthorized);
        }
        public static Error forbidden(string Code = "General.forbidden", string Description = "A forbidden Failure Has Occurred")
        {
            return new Error(Code, Description, ErrorType.Forbidden);
        }
        public static Error InvalidCrendentials(string Code = "General.InvalidCrendentials", string Description = "A InvalidCrendentials Failure Has Occurred")
        {
            return new Error(Code, Description, ErrorType.InvalidCrendentials);
        }
    }

}
