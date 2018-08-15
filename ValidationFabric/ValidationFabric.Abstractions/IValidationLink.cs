using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ValidationFabric.Abstractions
{
    public interface IValidationLink<T>
    {
        Expression<Func<T, bool>> Link { get; }
        string ChainName { get; }
        List<string> ErrorMessages { get; }
        Tuple<IValidationLink<T>, IValidationLink<T>> Branch { get; }
        IValidationLink<T> AddError(string errorMessage);
    }
}