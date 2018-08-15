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
using ValidationFabric.Abstractions;

namespace ValidationFabric
{
    /// <summary>
    /// The orchestrator class for business type validation of models and/or reposisories
    /// </summary>
    /// <typeparam name="T">the type of model or repository to manage</typeparam>
    public class ValidationFabric<T> : IValidationFabric<T>
    {
        private readonly Dictionary<string, ValidationChain<T>> _chains=new Dictionary<string, ValidationChain<T>>();
        private int _nextIndex = 0;
        private const string AnonymousChainPrefix = "AnonTypedChain_";
        private string NextKey()
        {
            return $"{AnonymousChainPrefix}{++_nextIndex}";
        }

        /// <summary>
        /// Gets or sets the chain for the given key
        /// If the chain does not exist, it is created.
        /// If the chain exists, it is overriden
        /// If setting the existing chain to null, it is deleted
        /// If setting the new chain to null an exception of type <exception cref="ArgumentException"/> is thrown
        /// If the chain has not been compiled yet, the invocation will most likely result in an exception
        /// of type <exception cref="AccessViolationException"></exception>
        /// Adding a chain without a name specified will give it an internally generated name on the form of
        /// "AnonTypedChain_0" where the number is a unique index generated per the fabric instance
        /// Use <see cref="Compile"/> to compile the chain before invoking it
        /// </summary>
        /// <param name="key">the key of the chain</param>
        /// <returns>the chain or null if it does not exist</returns>
        public ValidationChain<T> this[string key]
        {
            get
            {
                if (_chains.ContainsKey(key))
                    return (_chains[key]);
                return null;
            }
            set
            {
                if(value==null)
                    if (!string.IsNullOrWhiteSpace(key) && _chains.ContainsKey(key))
                    {
                        _chains.Remove(key);
                        return;
                    }
                    else
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
        /// <summary>
        /// Enumerates all the chains in the fabric that correspond to the member and also
        /// all of the chains that do not have a member constraint.
        /// The enumeration is further constricted to a set activation condition for the chain
        /// Setting the chains for the current member expression will append the chains to the collection
        /// and set a member constriction on the chains thus overriding any preset constrictions.
        /// Adding a chain without a name specified will give it an internally generated name
        /// </summary>
        /// <param name="item">the item to return the chains for</param>
        /// <param name="member">the member expression to check</param>
        /// <returns>an enumeration of the filtered chains</returns>
        public IEnumerable<ValidationChain<T>> this[T item,Func<T,object> member]
        {
            get
            {
                if (item==null)
                    throw new ArgumentException("The item cannot be null",nameof(item));

                var m = member?.Invoke(item);
                return (_chains.Values
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
        /// <summary>
        /// Gets the chain using a name
        /// </summary>
        /// <param name="chainName"></param>
        /// <returns>the specified chain or null if it does not exist</returns>
        public ValidationChain<T> Get(string chainName)
        {
            if(chainName.StartsWith(AnonymousChainPrefix))
                throw new ArgumentException("Querying for an anonymous chain is forbidden",nameof(chainName));
            return _chains[chainName];
        }
        /// <summary>
        /// Gets all of the chains from the fabric
        /// </summary>
        /// <returns>an enumeration of chains</returns>
        public IEnumerable<ValidationChain<T>> Get()
        {
            return _chains.Values;
        }
        /// <summary>
        /// Gets all of the chains for the given member
        /// </summary>
        /// <param name="member">the member to return the corresponding chains for</param>
        /// <returns>an enumeration of chains</returns>
        public IEnumerable<ValidationChain<T>> Get(Func<T, object> member)
        {
            var tmp = default(T);
            var m = member?.Invoke(tmp);
            return (_chains.Values
                .Where(x =>
                {
                    var am = x.ActivationMember(tmp);
                    return am == null || am.Equals(m);
                }));
        }

        /// <summary>
        /// Compiles all of the chains currently in the fabric in an AOT fashion
        /// Given the complexity of the fabric, this call may take a while
        /// but all of the subsequent validations will be faster
        /// </summary>
        public void CompileAheadOfTime()
        {
            _ = Compile(_chains.Values).ToList();
        }
        /// <summary>
        /// Adds the new chain to the collection
        /// If the chain has no name specified it will be generated internally
        /// </summary>
        /// <param name="chain">the chain to add</param>
        /// <returns>this is a fluid method</returns>
        public ValidationFabric<T> Add(ValidationChain<T> chain)
        {
            this[null] = chain;
            return this;
        }
        /// <summary>
        /// Validates the given member of the given instance of the type
        /// </summary>
        /// <param name="item">the instance to validate</param>
        /// <param name="member">the member to validate</param>
        /// <returns>the validation result</returns>
        public ValidationResult Validate(T item, Func<T, object> member)
        {
            foreach (var validationChain in Compile(this[item,member]))
            {
                var result = validationChain.Invoke(item);
                if (result != ValidationResult.Success)
                    return result;
            }
            return ValidationResult.Success;
        }

        /// <summary>
        /// Validates the given instance of the type.
        /// All of the chains that do not have a member constraint are invoked
        /// </summary>
        /// <param name="item">the item to validate</param>
        /// <returns>the validation result</returns>
        public ValidationResult Validate(T item)
        {
            foreach (var validationChain in Compile(this[item, null]))
            {
                var result = validationChain.Invoke(item);
                if (result != ValidationResult.Success)
                    return result;
            }
            return ValidationResult.Success;
        }

        /// <summary>
        /// Compiles the given validation chain.
        /// If the chain has already been compiled, this method does nothing
        /// </summary>
        /// <param name="chain">the chain to compile</param>
        /// <returns>the compiled chain</returns>
        public ValidationChain<T> Compile(ValidationChain<T> chain)
        {

            if (!chain.IsCompiled)
                chain.CompileRecursive(this);
            return chain;
        }
        private IEnumerable<ValidationChain<T>> Compile(IEnumerable<ValidationChain<T>> chain)
        {
            foreach (var validationChain in chain)
            {
                yield return Compile(validationChain);
            }
        }
    }
}
