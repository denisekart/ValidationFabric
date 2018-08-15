using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using ValidationFabric.Abstractions;


[assembly: AllowPartiallyTrustedCallers]
[assembly: SecurityTransparent]
[assembly: SecurityRules(SecurityRuleSet.Level2, SkipVerificationInFullTrust = true)]
namespace ValidationFabric
{
    /// <summary>
    /// the base class for the validation chain
    /// </summary>
    public abstract class ValidationChain
    {
        internal ValidationChain()
        {
        }
        /// <summary>
        /// Creates a new empty chain for the type with the given name
        /// </summary>
        /// <typeparam name="T">the type to create the chain for</typeparam>
        /// <param name="name">the name of the chain</param>
        /// <returns>the new empty chain</returns>
        public static ValidationChain<T> Create<T>(string name)=>new ValidationChain<T>{Name = name};
        /// <summary>
        /// Creates a new empty chain for the type
        /// </summary>
        /// <typeparam name="T">the type to create the chain for</typeparam>
        /// <returns>the new empty chain</returns>
        public static ValidationChain<T> Create<T>()=>new ValidationChain<T>();
    }

    /// <summary>
    /// This is a validation chain for the <see cref="ValidationFabric{T}"/>
    /// </summary>
    /// <typeparam name="T">the type to build the chain for</typeparam>
    public sealed class ValidationChain<T> : ValidationChain,IValidationChain<T>
    {

        internal ValidationChain()
        {   
        }

        public bool CanModify => !IsCompiled;
        /// <summary>
        /// Returns true if the chain has already been compiled.
        /// After the chain is compiled, all modifications are disallowed and will
        /// result in an exceptions
        /// </summary>
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


        
        private delegate ValidationResult InvocationDelegate(T item);

        private InvocationDelegate _invocator;
        /// <summary>
        /// Invokes the validation for the given chain
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public ValidationResult Invoke(T item)
        {
            if (!IsCompiled)
                throw new AccessViolationException("This chain has not been compiled yet.");
            CreateDelegate();
            return _invocator(item);
        }
        private void CreateDelegate()
        {
            if (_invocator != null)
                return;
            _invocator = new InvocationDelegate(Expression.Compile());
            //_invocatorFunc = Create.Compile();
        }
        /// <summary>
        /// Adds a new validation link
        /// </summary>
        /// <param name="link">the link to add</param>
        /// <returns>this is a fluid method</returns>
        public ValidationChain<T> Add(ValidationLink<T> link)
        {
            InvocationChain.Add(link);
            return this;
        }
        /// <summary>
        /// Adds a new validation link of type expression.
        /// It is preffered to keep single expressions as simple as possible.
        /// Multiple small expressions in preferred over few complex ones performance wise
        /// </summary>
        /// <param name="link">the link to add</param>
        /// <returns>this is a fluid method</returns>
        public ValidationChain<T> Add(Expression<Func<T, bool>> link)
        {
            ValidateAccess();
            InvocationChain.Add((ValidationLink<T>)link);
            return this;
        }
        /// <summary>
        /// Adds a new validation link of type ValidationChain
        /// The name must exist in the fabric only at compilation time
        /// so adding links to chains that have not been added yet is allowed
        /// </summary>
        /// <param name="chainName">the chain name</param>
        /// <returns>this is a fluid method</returns>
        public ValidationChain<T> Add(string chainName)
        {
            ValidateAccess();
            InvocationChain.Add((ValidationLink<T>)chainName);
            return this;
        }
        /// <summary>
        /// Adds an error message to the last link that was added. If none were added,
        /// the message will be appended to the first link in the chain.
        /// Single link can have multiple messages
        /// </summary>
        /// <param name="message">the message to add</param>
        /// <returns>this is a fluid method</returns>
        public ValidationChain<T> AddError(string message)
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
        /// <summary>
        /// Sets the member this chain will be invoked for
        /// </summary>
        /// <param name="member">the member</param>
        /// <returns>this is a fluid method</returns>
        public ValidationChain<T> SetMember(Expression<Func<T, object>> member)
        {
            ValidateAccess();
            ActivationMemberExpression = member;
            return this;
        }
        /// <summary>
        /// Sets the condition under which this method will be invoked
        /// </summary>
        /// <param name="condition">the condition to set</param>
        /// <returns>this is a fluid method</returns>
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