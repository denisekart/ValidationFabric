using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;


[assembly: AllowPartiallyTrustedCallers]
[assembly: SecurityTransparent]
[assembly: SecurityRules(SecurityRuleSet.Level2, SkipVerificationInFullTrust = true)]
namespace ValidationFabric
{
    public sealed class ValidationChain<T> : ValidationChain
    {

        internal ValidationChain()
        {   
        }

        public bool IsCompiled => _locked;
        /// <summary>
        /// The unique name of the chain
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// Constructs and invokes the activation invocation expression
        /// </summary>
        /// <param name="item">the item to check</param>
        /// <returns>true if condition is met. false otherwise. if condition is not set it returns true</returns>
        public bool ActivationCondition(T item) =>
            _activationCondition?.Invoke(item) ??
            ((_activationCondition = ActivationConditionExpression?.Compile())
             ?.Invoke(item) ?? true);
        /// <summary>
        /// Constructs and invokes the member invocation expression
        /// </summary>
        /// <param name="item">the item to check</param>
        /// <returns>the activation member or null if activation member is not set</returns>
        public object ActivationMember(T item) =>
            _activationMember?.Invoke(item) ??
            ((_activationMember = ActivationMemberExpression?.Compile())
                ?.Invoke(item));


        //private delegate ValidationResult MyDelegate(T item);
        //private  MyDelegate _delegate = 
        //    (MyDelegate)Delegate.CreateDelegate(typeof(MyDelegate),);

        delegate ValidationResult InvocationDelegate(T item);

        private InvocationDelegate _invocator;
        private Func<T, ValidationResult> _invocatorFunc;
        public ValidationResult Invoke2(T item)
        {
            if (!IsCompiled)
                throw new AccessViolationException("This chain has not been compiled yet.");
            CreateDelegate();
            return _invocator(item);
            //return _invocatorFunc.Invoke(item);
        }

        private void CreateDelegate()
        {
            if (_invocator != null)
                return;
            _invocator = new InvocationDelegate(Expression.Compile());
            //_invocatorFunc = Expression.Compile();
        }



        [Obsolete]
        public ValidationResult Invoke(T item)
        {
            if (!IsCompiled)
                throw new AccessViolationException("This chain has not been compiled yet.");
            return CompiledExpression(item);
        }

        public ValidationChain<T> AddLink(ValidationLink<T> link)
        {
            InvocationChain.Add(link);
            return this;
        }
        public ValidationChain<T> AddLink(Expression<Func<T, bool>> link)
        {
            ValidateAccess();
            InvocationChain.Add((ValidationLink<T>)link);
            return this;
        }
        public ValidationChain<T> AddChain(string chainName)
        {
            ValidateAccess();
            InvocationChain.Add((ValidationLink<T>)chainName);
            return this;
        }
        public ValidationChain<T> AddErrorMessage(string message)
        {
            ValidateAccess();
            if (InvocationChain.Count == 0)
                _floatingMessages.Add(message);
            else
            {
                if (_floatingMessages.Any())
                {
                    InvocationChain[InvocationChain.Count - 1].ErrorMessages.AddRange(_floatingMessages);
                    _floatingMessages.Clear();
                }

                InvocationChain[InvocationChain.Count - 1].ErrorMessages.Add(message);
            }
            return this;
        }
        public ValidationChain<T> SetMember(Expression<Func<T, object>> member)
        {
            ValidateAccess();
            ActivationMemberExpression = member;
            return this;
        }
        public ValidationChain<T> SetCondition(Expression<Func<T, bool>> condition)
        {
            ValidateAccess();
            ActivationConditionExpression = condition;
            return this;
        }





        /// <summary>
        /// The invocation chain used for validation
        /// </summary>
        internal List<ValidationLink<T>> InvocationChain { get; } = new List<ValidationLink<T>>();

        [Obsolete]
        internal Func<T,ValidationResult> CompiledExpression { get; private set; }
        [Obsolete]
        internal void Compile(Func<T, ValidationResult> func)
        {
            ValidateAccess();
            CompiledExpression = func;

            _locked = true;
        }
        /// <summary>
        /// The activation condition of the chain
        /// </summary>
        private Expression<Func<T,bool>> ActivationConditionExpression { get; set; }
        private Func<T, bool> _activationCondition;
        /// <summary>
        /// The member that activates the chain
        /// </summary>
        private Expression<Func<T,object>> ActivationMemberExpression { get; set; }
        private void ValidateAccess()
        {
            if (_locked)
                throw new AccessViolationException("This compiler has already been invoked");
        }



        private Func<T, object> _activationMember;
        private readonly List<string> _floatingMessages=new List<string>();
        private bool _locked = false;
        

        private Expression<Func<T, ValidationResult>> _expression;

        internal Expression<Func<T, ValidationResult>> Expression
        {
            get => _expression;
            set
            {
                ValidateAccess();
                _expression = value;
                if (_expression != null)
                    _locked = true;
            }
        }
    }
}