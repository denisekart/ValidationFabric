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
                if (_chains.ContainsKey(key))
                    _chains[key] = value;
                else
                {
                    _chains.Add(key,value);
                }
            }
        }

        public IEnumerable<ValidationChain<T>> this[T item,string property]
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

        public void CompileAOT()
        {
            _ = Compile(_chains.Values).ToList();
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

    internal static class ValidationChainCompiler
    {
        internal class ExpressionBag<T>
        {
            private ParameterExpression _parameter;
            public ParameterExpression Parameter => _parameter??
                   (_parameter=Expression.Parameter(typeof(T), "entity"));

            private Expression _success;
            public Expression Success => _success??
                   (_success=Expression.Constant(ValidationResult.Success, typeof(ValidationResult)));

            private Expression _zeroConstant;
            public Expression ZeroConstant => _zeroConstant??(_zeroConstant=Expression.Constant(0, typeof(int)));

            private ParameterExpression _variable;
            public ParameterExpression Variable => _variable??
                                                   (_variable=Expression.Variable(typeof(ValidationResult), "var"));

        }


        internal static Expression CompileLinkExpression<T>(this ValidationLink<T> link, Expression nextLink,
            ValidationFabric<T> fabric, ExpressionBag<T> bag)
        {
            switch (link.Type)
            {
                case ValidationLink<T>.LinkType.Expression:
                    Expression<Func<T, ValidationResult>> expr = 
                        (e) =>
                        link.Link.Compile().Invoke(e)
                            ? Expression.Lambda<Func<T, ValidationResult>>(nextLink, bag.Parameter).Compile().Invoke(e)
                            : ValidationResult.Failure(link.ErrorMessages.ToArray());

                    return Expression.Invoke(expr, bag.Parameter);

                case ValidationLink<T>.LinkType.ChainName:
                    if (fabric == null)
                        throw new ArgumentException("Cannot compile a chan that references another chain from the fabric when fabric is null", nameof(fabric));
                    if (fabric[link.ChainName] is ValidationChain<T> c)
                    {

                        Expression<Func<T, ValidationResult>> caller = 
                            (e) => 
                                (c.Invoke(e));

                        var invoker = Expression.Invoke(caller, bag.Parameter);
                        
                        var block = Expression.Block(
                            new ParameterExpression[] { bag.Variable }, //var
                            Expression.Assign(bag.Variable, invoker), //previous
                            Expression.IfThenElse(//if else
                                Expression.Equal(bag.Variable, bag.Success),
                                Expression.Assign(bag.Variable,
                                    Expression.Invoke(
                                        Expression.Lambda<Func<T, ValidationResult>>(
                                            nextLink,
                                            bag.Parameter),
                                        bag.Parameter)),//success - next
                                Expression.Assign(bag.Variable,
                                    Expression.Constant(ValidationResult.Failure(link.ErrorMessages.ToArray())))),//fail -error
                            bag.Variable);

                        return Expression.Invoke(
                            Expression.Lambda(
                                block,
                                bag.Parameter),
                            bag.Parameter);
                        
                    }
                    throw new ArgumentException($"The chain with the name {link.ChainName} does not exist in the fabric.");
                    
                default:
                    throw new ArgumentException($"Unknown link type {link.Type}");
                    break;
            }
        }

        internal static ValidationChain<T> CompileTree<T>(this ValidationChain<T> chain,
            ValidationFabric<T> fabric)
        {
            if (chain.IsCompiled)
                return chain;
            
            var bag=new ExpressionBag<T>();
            

            Func<T, ValidationResult> expression;
            if (chain.InvocationChain.Count == 0)
                expression=x=>ValidationResult.Success;
            else
            {
                var temp =
                    (chain.InvocationChain[chain.InvocationChain.Count - 1]).CompileLinkExpression(bag.Success, fabric,bag);

                for (int i = chain.InvocationChain.Count - 2; i >= 0; i--)
                {
                    temp = (chain.InvocationChain[i]).CompileLinkExpression(temp, fabric,bag);
                }

                expression = Expression.Lambda<Func<T, ValidationResult>>(temp,bag.Parameter)
                    .Compile();
            }
            
                chain.Compile(expression);
                return chain;
        }

        
    }

    public class ValidationChain<T>
    {
        private bool _locked = false;
        public bool IsCompiled => _locked;
        internal Func<T,ValidationResult> CompiledExpression { get; private set; }

        internal void Compile(Func<T, ValidationResult> func)
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
        public Func<T,string,bool> ActivationCondition { get; protected set; }

        /// <summary>
        /// The invocation chain used for validation
        /// </summary>
        public List<ValidationLink<T>> InvocationChain { get; } = new List<ValidationLink<T>>();

        public virtual ValidationResult Invoke(T item)
        {
            if(!IsCompiled)
                throw new AccessViolationException("This chain has not been compiled yet.");
            return CompiledExpression.Invoke(item);
        }

        private readonly List<string> _floatingMessages=new List<string>();

        public virtual ValidationChain<T> AddLink(Func<T, bool> link)
        {
            ValidateAccess();
            InvocationChain.Add((ValidationLink<T>)link);
            return this;
        }
        //public virtual ValidationChain AddChain(Func<object,string, bool> chain)
        //{
        //    ValidateAccess();
        //    InvocationChain.Add((ValidationLink)chain);
        //    return this;
        //}
        public virtual ValidationChain<T> AddChain(string chainName)
        {
            ValidateAccess();
            InvocationChain.Add((ValidationLink<T>)chainName);
            return this;
        }

        public ValidationChain<T> AddErrorMessage(string message)
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

    public class ValidationLink<T>
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
