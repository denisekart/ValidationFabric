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
    public class ValidationFabric
    {
        private Dictionary<string, ValidationChain> _chains=new Dictionary<string, ValidationChain>();
        private int _nextIndex = 0;
        public ValidationChain this[string key]
        {
            get
            {
                if (_chains.ContainsKey(key))
                    return Compile(_chains[key]);
                return null;
            }
            set
            {
                if (_chains.ContainsKey(key))
                    _chains[key] = value;
                else
                {
                    _chains.Add(key,value);
                }
            }
        }

        public IEnumerable<ValidationChain> this[object item,string property]
        {
            get { return _chains.Values.Where(x => x.ActivationCondition?.Invoke(item, property) ?? true); }
            set
            {
                foreach (var validationChain in value)
                {
                    if (!string.IsNullOrWhiteSpace(validationChain.Name))
                        this[validationChain.Name] = validationChain;
                    else
                    _chains.Add($"ValidationChain_{++_nextIndex}", validationChain);
                }
            }
        }

        private ValidationChain Compile(ValidationChain chain)
        {
            if (chain.IsCompiled)
                return chain;

            var param = Expression.Parameter(typeof(object), "obj");

            Expression ExtractExpression(ValidationLink link)
            {
                switch (link.Type)
                {
                    case ValidationLink.LinkType.Expression:
                        return Expression.Invoke(link.Link, param);


                    case ValidationLink.LinkType.ChainName:
                        if (this[link.ChainName] is ValidationChain c)
                        {
                            var xxp=(Expression<Func<object,bool>>)(p => c.CompiledExpression(p));
                            return Expression.Invoke(xxp, param);
                        }
                        throw new ArgumentException($"The chain with the name {link.ChainName} does not exist in the fabric.");


                    //case ValidationLink.LinkType.ChainCondition:
                    //    var nfunc = link.ActivationCondition;
                    //    foreach (var validationChain in this._chains.Values)
                    //    {
                            
                    //    }


                    //    Func<object, string, bool> xp = (o, p) =>
                    //    {
                    //        var chs = this[o, p].ToList();
                    //        if (chs.Count == 0)
                    //            return true;

                    //        Expression CompileFunc(Func<object, bool> f)
                    //        {
                    //            return (Expression<Func<object, bool>>)(xx => f.Invoke(xx));
                    //        }

                    //        var temp = (Expression)Expression.Invoke(
                    //            CompileFunc(chs[chs.Count - 1].CompiledExpression), param);
                    //        for (int i = chs.Count - 2; i >= 0; i--)
                    //        {
                    //            temp = Expression.AndAlso(
                    //                Expression.Invoke(CompileFunc(chs[i].CompiledExpression), param), temp);
                    //        }

                    //        var r = Expression.Lambda<Func<object, bool>>(temp, param).Compile();
                    //        return r.Invoke(o);
                    //    };

                    //    var expr = (Expression<Func<object, bool>>)(p => xp.Invoke(p, null));
                    //    return Expression.Invoke(expr, param);

                    //    break;
                    default:
                        return null;
                }
            }


            Func<object, bool> _expression;
            if (chain.InvocationChain.Count == 0)
                _expression = x => true;
            else
            {
                var temp = ExtractExpression(chain.InvocationChain[chain.InvocationChain.Count - 1]);
                for (int i = chain.InvocationChain.Count - 2; i >= 0; i--)
                {
                    temp=Expression.AndAlso(ExtractExpression(chain.InvocationChain[i]),param);
                }
                _expression=Expression.Lambda<Func<object,bool>>(temp,param).Compile();
            }
            chain.Compile(_expression);


            return null;
        }
        private IEnumerable<ValidationChain> Compile(IEnumerable<ValidationChain> chain)
        {
            foreach (var validationChain in chain)
            {
                yield return Compile(validationChain);
            }
        }
    }

    public class ValidationChain
    {
        private bool _locked = false;
        public bool IsCompiled => _locked;
        internal Func<object,bool> CompiledExpression { get; set; }

        internal void Compile(Func<object, bool> func)
        {
            ValidateAccess();
            CompiledExpression = func;

            _locked = true;
        }

        private void ValidateAccess()
        {
            if (_locked)
                throw new AccessViolationException("This compiler has already been invoked");
        }


        /// <summary>
        /// The unique name of the chain
        /// </summary>
        public string Name { get; protected set; }
        /// <summary>
        /// The activation condition of the chain
        /// </summary>
        public virtual Func<object,string,bool> ActivationCondition { get; protected set; }

        /// <summary>
        /// The invocation chain used for validation
        /// </summary>
        public virtual List<ValidationLink> InvocationChain { get; protected set; } = new List<ValidationLink>();

        public virtual ValidationResult Invoke(object item)
        {
            if(!IsCompiled)
                throw new AccessViolationException("This chain has not been compiled yet.");
            return ValidationResult.Success;
        }

        private readonly List<string> _floatingMessages=new List<string>();

        public virtual ValidationChain AddLink(Func<object, bool> link)
        {
            ValidateAccess();
            InvocationChain.Add((ValidationLink)link);
            return this;
        }
        //public virtual ValidationChain AddChain(Func<object,string, bool> chain)
        //{
        //    ValidateAccess();
        //    InvocationChain.Add((ValidationLink)chain);
        //    return this;
        //}
        public virtual ValidationChain AddChain(string chainName)
        {
            ValidateAccess();
            InvocationChain.Add((ValidationLink)chainName);
            return this;
        }

        public ValidationChain AddErrorMessage(string message)
        {
            ValidateAccess();
            if(InvocationChain.Count==0)
                _floatingMessages.Add(message);
            else
            {
                if (_floatingMessages.Any())
                {
                    InvocationChain[InvocationChain.Count - 1].ErrorMessages.AddRange(_floatingMessages);
                    _floatingMessages.Clear();
                }

                InvocationChain[InvocationChain.Count-1].ErrorMessages.Add(message);
            }
            return this;
        }
    }

    public class ValidationContext
    {
        public object Instance { get; set; }
        public string Propertyname { get; set; }
        public object Tag { get; set; }
    }

    public class ValidationLink
    {
        public enum LinkType
        {
            Expression,
            ChainName,
            ChainCondition
        }

        public LinkType Type { get; set; }

        /// <summary>
        /// The validator for the link
        /// </summary>
        public virtual Expression<Func<object,bool>> Link { get; protected set; }
        /// <summary>
        /// The name of the chain to invoke
        /// </summary>
        public string ChainName { get; protected set; }


        /// <summary>
        /// The error messages produced on link failure 
        /// </summary>
        public List<string> ErrorMessages { get;  }=new List<string>();

        public static explicit operator ValidationLink(Func<object, bool> l)
        {
            var lnk = new ValidationLink {Type = LinkType.Expression, Link = x=>l.Invoke(x)};
            return lnk;
        }
        
        public static explicit operator ValidationLink(string l)
        {
            var lnk = new ValidationLink { Type = LinkType.ChainName, ChainName = l};
            return lnk;
        }
    }
}
