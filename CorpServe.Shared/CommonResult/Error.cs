using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Shared.CommonResult
{
    public class Error
    {
        public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

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
        public static Error forbidden(string Code = "General.Forbidden", string Description = "A Forbidden Failure Has Occurred")
        {
            return new Error(Code, Description, ErrorType.Forbidden);
        }
        public static Error InvalidCrendentials(string Code = "General.InvalidCredentials", string Description = "An InvalidCredentials Failure Has Occurred")
        {
            return new Error(Code, Description, ErrorType.InvalidCrendentials);
        }
        public static Error Conflict(string Code = "General.Conflict", string Description = "A Conflict Failure Has Occurred")
        {
            return new Error(Code, Description, ErrorType.Conflict);
        }
    }
}
