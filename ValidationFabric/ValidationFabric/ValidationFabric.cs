﻿using System;
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
                    return (_chains[key]);
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
            foreach (var validationChain in Compile(this[item,member]))
            {
                var result = validationChain.Invoke(item);
                if (result != ValidationResult.Success)
                    return result;
            }
            return ValidationResult.Success;
        }

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

        private ValidationChain<T> Compile(ValidationChain<T> chain)
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

    public abstract class ValidationChain
    {
        internal ValidationChain()
        {
        }

        public static ValidationChain<T> EmptyChain<T>(string name)=>new ValidationChain<T>{Name = name};
        public static ValidationChain<T> EmptyChain<T>()=>new ValidationChain<T>();
    }
    
}
