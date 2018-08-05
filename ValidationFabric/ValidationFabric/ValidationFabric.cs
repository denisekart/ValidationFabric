using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace ValidationFabric
{
    public class ValidationFabric<T>
    {
        private Dictionary<string, ValidationChain<T>> _chains=new Dictionary<string, ValidationChain<T>>();
        private int _nextIndex = 0;

        private string NextKey()
        {
            return $"ValidationChain_{++_nextIndex}";
        }
        public ValidationChain<T> this[string key]
        {
            get
            {
                if (_chains.ContainsKey(key))
                    return Compile(_chains[key]);
                return null;
            }
            set
            {
                if(value==null)
                    throw new ArgumentException("The chain cannot be null",nameof(value));
                if (string.IsNullOrWhiteSpace(key) && 
                    string.IsNullOrWhiteSpace(value.Name))
                {
                    key = NextKey();
                }

                if (string.IsNullOrWhiteSpace(value.Name))
                    value.Name = key;

                if (string.IsNullOrWhiteSpace(key))
                    key = value.Name;

                if (!key.Equals(value.Name))
                    value.Name = key;

                if (_chains.ContainsKey(key))
                    _chains[key] = value;
                else
                {
                    _chains.Add(key,value);
                }
            }
        }
        public IEnumerable<ValidationChain<T>> this[T item,Func<T,object> member]
        {
            get
            {
                if (item==null)
                    throw new ArgumentException("The item cannot be null",nameof(item));

                var m = member?.Invoke(item);
                return Compile(_chains.Values
                    .Where(x =>
                    {
                        var am = x.ActivationMember(item);
                        return am==null || am.Equals(m);
                    })
                    .Where(x=>x.ActivationCondition(item)));
            }
            set
            {
                foreach (var validationChain in value)
                {
                    if (!string.IsNullOrWhiteSpace(validationChain.Name))
                        this[validationChain.Name] = validationChain;
                    else
                    {
                        var nextName = validationChain.Name;
                        if (string.IsNullOrWhiteSpace(nextName))
                            nextName = NextKey();
                        
                        validationChain.Name = nextName;
                        if (member != null)
                        {
                            Expression<Func<T, object>> mem = x => member.Invoke(x);
                            validationChain.SetMember(mem);
                        }

                        _chains.Add(nextName, validationChain);
                    }
                        
                }
            }
        }
        public void CompileAheadOfTime()
        {
            _ = Compile(_chains.Values).ToList();
        }

        public ValidationFabric<T> AddChain(ValidationChain<T> chain)
        {
            this[null] = chain;
            return this;
        }
        public ValidationResult Validate(T item, Func<T, object> member)
        {
            foreach (var validationChain in this[item,member])
            {
                var result = validationChain.Invoke(item);
                if (result != ValidationResult.Success)
                    return result;
            }
            return ValidationResult.Success;
        }


        private ValidationChain<T> Compile(ValidationChain<T> chain)
        {
            if (chain.IsCompiled)
                return chain;
            return chain.CompileTree(this);
        }
        private IEnumerable<ValidationChain<T>> Compile(IEnumerable<ValidationChain<T>> chain)
        {
            foreach (var validationChain in chain)
            {
                yield return Compile(validationChain);
            }
        }
    }

    public abstract class ValidationChain
    {
        internal ValidationChain()
        {
        }

        public static ValidationChain<T> EmptyChain<T>(string name)=>new ValidationChain<T>{Name = name};
        public static ValidationChain<T> EmptyChain<T>()=>new ValidationChain<T>();
    }

    public class ValidationContext
    {
        public object Instance { get; set; }
        public string Propertyname { get; set; }
        public object Tag { get; set; }
    }

    public class ValidationLink<T>
    {
        public enum LinkType
        {
            Expression,
            ChainName,
        }

        public LinkType Type { get; set; }

        /// <summary>
        /// The validator for the link
        /// </summary>
        public virtual Expression<Func<T,bool>> Link { get; private set; }
        /// <summary>
        /// The name of the chain to invoke
        /// </summary>
        public string ChainName { get; private set; }


        /// <summary>
        /// The error messages produced on link failure 
        /// </summary>
        public List<string> ErrorMessages { get;  }=new List<string>();

        public static explicit operator ValidationLink<T>(Func<T, bool> l)
        {
            var lnk = new ValidationLink<T> {Type = LinkType.Expression, Link = x=>l.Invoke(x)};
            return lnk;
        }
        
        public static explicit operator ValidationLink<T>(string l)
        {
            var lnk = new ValidationLink<T> { Type = LinkType.ChainName, ChainName = l};
            return lnk;
        }
    }
}
