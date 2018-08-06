using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ValidationFabric
{
    /// <summary>
    /// This is a validation link that is a part of the chain
    /// Invocations for this type are also available in
    /// <see cref="ValidationChain{T}"/>
    /// </summary>
    /// <typeparam name="T">the type of model to associate</typeparam>
    public class ValidationLink<T>
    {
        /// <summary>
        /// Creates a link with the specified expression. Keep the expression simple.
        /// Multiple simple links are prefered over few complex ones performance wise
        /// </summary>
        /// <param name="expression">the expression</param>
        /// <returns>the new link</returns>
        public static ValidationLink<T> Create(Expression<Func<T, bool>> expression)=>
            new ValidationLink<T> { Type = LinkType.Expression, Link = expression };
        /// <summary>
        /// Creates a link for the chain. The chain does not have to exist in the fabric yet
        /// </summary>
        /// <param name="chainName">the name of the chain</param>
        /// <returns>the new validation link</returns>
        public static ValidationLink<T> Create(string chainName)=> 
            new ValidationLink<T> { Type = LinkType.ChainName, ChainName = chainName };

        internal ValidationLink()
        {
            
        }
        internal enum LinkType
        {
            Expression,
            ChainName,
            OrBranch,
            XorBranch
        }
        
        internal LinkType Type { get; private set; }
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
        /// <summary>
        /// The branch chain links
        /// </summary>
        public Tuple<ValidationLink<T>, ValidationLink<T>> Branch { get; private set; }
        /// <summary>
        /// Adds an error to this link. Multiple errors can be specified
        /// </summary>
        /// <param name="errorMessage">the message to add</param>
        /// <returns>this is a fluid method</returns>
        public ValidationLink<T> AddError(string errorMessage)
        {
            ErrorMessages.Add(errorMessage);
            return this;
        }

        internal static ValidationLink<T> OrBranch(ValidationLink<T> left, ValidationLink<T> right)
        {
            return new ValidationLink<T>
            {
                Type = LinkType.OrBranch,
                Branch = new Tuple<ValidationLink<T>, ValidationLink<T>>(left, right)
            };
        }
        internal static ValidationLink<T> XorBranch(ValidationLink<T> left, ValidationLink<T> right)
        {
            return new ValidationLink<T>
            {
                Type = LinkType.XorBranch,
                Branch = new Tuple<ValidationLink<T>, ValidationLink<T>>(left, right)
            };
        }
        /// <summary>
        /// This operator combines the specified links and creates a logical OR link
        /// </summary>
        /// <param name="left">the left hand side</param>
        /// <param name="right">the right hand side</param>
        /// <returns>the OR validation link</returns>
        public static ValidationLink<T> operator | (ValidationLink<T> left, ValidationLink<T> right)
        {
            return OrBranch(left, right);
        }

        /// <summary>
        /// This operator combines the specified links and creates a logical XOR link
        /// </summary>
        /// <param name="left">the left hand side</param>
        /// <param name="right">the right hand side</param>
        /// <returns>the XOR validation link</returns>
        public static ValidationLink<T> operator ^ (ValidationLink<T> left, ValidationLink<T> right)
        {
            return XorBranch(left, right);
        }

        public static implicit operator ValidationLink<T>(Expression<Func<T, bool>> l)
        {
            var lnk = new ValidationLink<T> { Type = LinkType.Expression, Link = l };
            return lnk;
        }

        public static explicit operator ValidationLink<T>(Func<T, bool> l)
        {
            var lnk = new ValidationLink<T> {Type = LinkType.Expression, Link = x=>l.Invoke(x)};
            return lnk;
        }
        
        public static implicit operator ValidationLink<T>(string l)
        {
            var lnk = new ValidationLink<T> { Type = LinkType.ChainName, ChainName = l};
            return lnk;
        }
    }
}