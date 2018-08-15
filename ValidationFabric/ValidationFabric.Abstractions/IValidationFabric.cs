using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValidationFabric.Abstractions
{
    public interface IValidationFabric<T>
    {
        //IValidationChain<T> this[string key] { get; set; }
        //IEnumerable<IValidationChain<T>> this[T item,Func<T,object> member] { get; set; }
    }
}
