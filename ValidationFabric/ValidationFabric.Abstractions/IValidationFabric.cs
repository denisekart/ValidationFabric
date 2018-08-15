using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
[assembly: AllowPartiallyTrustedCallers]
[assembly: SecurityTransparent]
[assembly: SecurityRules(SecurityRuleSet.Level2, SkipVerificationInFullTrust = true)]

namespace ValidationFabric.Abstractions
{
    public interface IValidationFabric<T>
    {
        IValidationChain<T> this[string key] { get; set; }
        IEnumerable<IValidationChain<T>> this[T item, Func<T, object> member] { get; set; }
        IEnumerable<IValidationChain<T>> Get();
        IEnumerable<IValidationChain<T>> Get(Func<T, object> member);
        IValidationChain<T> Get(string chainName);
        ValidationResult Validate(T item, Func<T, object> member);
        ValidationResult Validate(T item);
        void CompileAheadOfTime();
    }
}
