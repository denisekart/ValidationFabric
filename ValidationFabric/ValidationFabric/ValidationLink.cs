using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ValidationFabric
{
    public class ValidationLink<T>
    {
        public static ValidationLink<T> Expression(Expression<Func<T, bool>> expression)=>
            new ValidationLink<T> { Type = LinkType.Expression, Link = expression };
        public static ValidationLink<T> Chain(string chainName)=> 
            new ValidationLink<T> { Type = LinkType.ChainName, ChainName = chainName };

        internal ValidationLink()
        {
            
        }
        public enum LinkType
        {
            Expression,
            ChainName,
            OrBranch,
            XorBranch
        }
        
        public LinkType Type { get; private set; }
        /// <summary>
        /// The validator for the link
        /// </summary>
        public Expression<Func<T,bool>> Link { get; private set; }
        /// <summary>
        /// The name of the chain to invoke
        /// </summary>
        public string ChainName { get; private set; }
        /// <summary>
        /// The error messages produced on link failure 
        /// </summary>
        public List<string> ErrorMessages { get;  }=new List<string>();

        public Tuple<ValidationLink<T>, ValidationLink<T>> Branch { get; private set; }

        public ValidationLink<T> WithError(string errorMessage)
        {
            ErrorMessages.Add(errorMessage);
            return this;
        }

        public static ValidationLink<T> OrBranch(ValidationLink<T> left, ValidationLink<T> right)
        {
            return new ValidationLink<T>
            {
                Type = LinkType.OrBranch,
                Branch = new Tuple<ValidationLink<T>, ValidationLink<T>>(left, right)
            };
        }
        public static ValidationLink<T> XorBranch(ValidationLink<T> left, ValidationLink<T> right)
        {
            return new ValidationLink<T>
            {
                Type = LinkType.XorBranch,
                Branch = new Tuple<ValidationLink<T>, ValidationLink<T>>(left, right)
            };
        }

        public static ValidationLink<T> operator | (ValidationLink<T> left, ValidationLink<T> right)
        {
            return OrBranch(left, right);
        }

        public static ValidationLink<T> operator ^ (ValidationLink<T> left, ValidationLink<T> right)
        {
            return XorBranch(left, right);
        }

        public static explicit operator ValidationLink<T>(Expression<Func<T, bool>> l)
        {
            var lnk = new ValidationLink<T> { Type = LinkType.Expression, Link = l };
            return lnk;
        }

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