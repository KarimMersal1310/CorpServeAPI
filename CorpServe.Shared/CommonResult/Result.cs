using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace E_Commerce.Shared.CommonResult
{
    public class Result
    {
        protected readonly List<Error> _errors = [];
        public bool IsSuccess => _errors.Count == 0; // True
        public bool IsFailure => !IsSuccess; // False
        public IReadOnlyList<Error> Errors => _errors;
        // OK - Success
        protected Result()
        {

        }
        // fail with error
        protected Result(Error error)
        {
            _errors.Add(error);
        }
        // fail with errors
        protected Result(List<Error> errors)
        {
            _errors.AddRange(errors);
        }

        public static Result Ok() => new Result();
        public static Result Fail(Error error) => new Result(error);
        public static Result Fail(List<Error> errors) => new Result(errors);
    }

    public class Result<TValue> : Result
    {
        private readonly TValue _value;
        public TValue Value => IsSuccess ? _value : throw new InvalidOperationException("Can Not Access The Value Of Failed Result");
        private Result(TValue value)
        {
            _value = value;
        }
        private Result(Error error):base(error)
        {
            _value = default!;
        }
        private Result(List<Error> errors):base(errors)
        {
            _value = default!;
        }
        public static Result<TValue> Ok(TValue value) => new(value);
        public static new Result<TValue> Fail(Error error) => new(error);
        public static new Result<TValue> Fail(List<Error> errors) => new(errors);

        public static implicit operator Result<TValue>(TValue value) => Ok(value);
        public static implicit operator Result<TValue>(Error error) => Fail(error);
        public static implicit operator Result<TValue>(List<Error> errors) => Fail(errors);
    }
}
