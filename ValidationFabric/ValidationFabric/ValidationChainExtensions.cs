using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ValidationFabric
{
    /// <summary>
    /// Provides extensions for <see cref="ValidationFabric{T}"/>, <see cref="ValidationChain{T}"/> and <see cref="ValidationLink{T}"/> types
    /// </summary>
    public static class ValidationChainExtensions
    {
        [DebuggerStepThrough]
        internal class ExpressionBag<T>
        {
            private ParameterExpression _parameter;
            public ParameterExpression Parameter => _parameter??
                                                    (_parameter=Expression.Parameter(typeof(T), "entity"));

            private Expression _success;
            public Expression Success => _success??
                                         (_success=Expression.Constant(ValidationResult.Success, typeof(ValidationResult)));


            private Expression<Func<T,ValidationResult>> _successLambda;
            public Expression<Func<T, ValidationResult>> SuccessLambda => _successLambda ??
                                         (_successLambda = x=>ValidationResult.Success);
            
            private Expression _zeroConstant;
            public Expression ZeroConstant => _zeroConstant??(_zeroConstant=Expression.Constant(0, typeof(int)));

            private ParameterExpression _variable;
            public ParameterExpression Variable => _variable??
                                                   (_variable=Expression.Variable(typeof(ValidationResult), "var"));

            private ParameterExpression _variable2;
            public ParameterExpression Variable2 => _variable2 ??
                                                   (_variable2 = Expression.Variable(typeof(ValidationResult), "var2"));

        }

        [DebuggerStepThrough]
        private static Expression<Func<T,ValidationResult>> CreateExpression<T>(ValidationLink<T> link, Expression next, ExpressionBag<T> bag, ValidationFabric<T> fabric)
        {
            Expression block=bag.SuccessLambda;
            switch (link.Type)
            {
                case ValidationLink<T>.LinkType.Expression:
                    block = CreateNodeExpression(link, next, bag);
                    break;
                case ValidationLink<T>.LinkType.ChainName:
                    block = CreateChainExpression(link, next, bag, fabric);
                    break;
                case ValidationLink<T>.LinkType.OrBranch:
                    block = CreateLogicalOrExpression(link, next, bag, fabric);
                    break;
                case ValidationLink<T>.LinkType.XorBranch:
                    block = CreateLogicalXorExpression(link, next, bag, fabric);
                    break;

            }
            
            var lambda = Expression.Lambda<Func<T, ValidationResult>>(block, bag.Parameter);

            
            return lambda;
        }
        [DebuggerStepThrough]
        private static Expression CreateLogicalOrExpression<T>(ValidationLink<T> link, Expression next, ExpressionBag<T> bag, ValidationFabric<T> fabric)
        {
            var link1 = link.Branch.Item1;
            var link2 = link.Branch.Item2;

            if ((link1.Type==ValidationLink<T>.LinkType.ChainName ||
               link2.Type==ValidationLink<T>.LinkType.ChainName) &&
               fabric==null)
                throw new ArgumentException("Cannot compile a chan that references another chain from the fabric when fabric is null", nameof(fabric));

            var exp1 = CreateExpression(link1, bag.SuccessLambda, bag, fabric);
            var exp2 = CreateExpression(link2, bag.SuccessLambda, bag, fabric);

            Expression<Func<ValidationResult, int>> errorCount = v =>
                v.ErrorMessages == null ? 0 : v.ErrorMessages.Count;
            Expression<Func<ValidationResult,ValidationResult,ValidationResult>> failCombine=
                (v1,v2)=>ValidationResult.Failure(
                    Enumerable.Concat(
                        v1.ErrorMessages??Enumerable.Empty<string>(),
                        v2.ErrorMessages??Enumerable.Empty<string>()).ToArray());

            var nextInvocation = Expression.Invoke(next,bag.Parameter);
            /*
             * var=invoke exp1
             * var2=success
             *
             * if(var==success)
             *      var=invoke next
             * else
             *      var2=invoke exp2
             *
             * if(var != success && var2 != success)
             *      var=var+var2
             * else
             *      var=success
             * return var
             */
            return Expression.Block(
                new ParameterExpression[]
                {
                    bag.Variable,
                    bag.Variable2
                },
                Expression.Assign(
                    bag.Variable2,
                    bag.Success
                    ),
                Expression.Assign(
                    bag.Variable,
                    Expression.Invoke(
                        exp1,
                        bag.Parameter
                        )
                    ),
                Expression.IfThenElse(
                    Expression.Equal(
                        bag.Success,
                        bag.Variable
                        ),
                    Expression.Assign(
                        bag.Variable,
                        nextInvocation
                        ),
                    Expression.Assign(
                        bag.Variable2,
                        Expression.Invoke(
                            exp2,
                            bag.Parameter
                            )
                        )
                    ),
                Expression.IfThenElse(
                    Expression.AndAlso(
                        Expression.NotEqual(
                            bag.Success,
                            bag.Variable
                            ),
                        Expression.NotEqual(
                            bag.Success,
                            bag.Variable2
                            )
                        ),
                    Expression.Assign(
                        bag.Variable,
                        Expression.Invoke(
                            failCombine,
                            bag.Variable,
                            bag.Variable2
                            )
                        ),
                    Expression.Assign(
                        bag.Variable,
                        bag.Success
                        )
                    ),
                Expression.IfThen(
                    Expression.AndAlso(
                        Expression.NotEqual(
                            bag.Success,
                            bag.Variable),
                        Expression.Equal(
                            bag.ZeroConstant,
                            Expression.Invoke(
                                errorCount,
                                bag.Variable)
                            )
                    ),
                    Expression.Assign(
                        bag.Variable,
                        Expression.Constant(
                            ValidationResult.Failure(link.ErrorMessages.ToArray()),
                            typeof(ValidationResult))
                    )
                ),
                bag.Variable
            );

            //Create.IfThen(
            //    Create.Equal(
            //        bag.ZeroConstant,
            //        Create.Invoke(
            //            errorCount,
            //            bag.Variable)
            //    ),
            //    Create.Assign(
            //        bag.Variable,
            //        Create.Constant(
            //            ValidationResult.Failure(link.ErrorMessages.ToArray()),
            //            typeof(ValidationResult))
            //    )
            //)


        }
        [DebuggerStepThrough]
        private static Expression CreateLogicalXorExpression<T>(ValidationLink<T> link, Expression next, ExpressionBag<T> bag, ValidationFabric<T> fabric)
        {
            /*
             * 0 | 0 = 0
             * 0 | 1 = 1
             * 1 | 0 = 1
             * 1 | 1 = 0
             */
            var link1 = link.Branch.Item1;
            var link2 = link.Branch.Item2;

            if ((link1.Type == ValidationLink<T>.LinkType.ChainName ||
               link2.Type == ValidationLink<T>.LinkType.ChainName) &&
               fabric == null)
                throw new ArgumentException("Cannot compile a chan that references another chain from the fabric when fabric is null", nameof(fabric));

            var exp1 = CreateExpression(link1, bag.SuccessLambda, bag, fabric);
            var exp2 = CreateExpression(link2, bag.SuccessLambda, bag, fabric);


            Expression<Func<ValidationResult, ValidationResult, ValidationResult>> failCombine =
                (v1, v2) => ValidationResult.Failure(
                    Enumerable.Concat(
                        v1.ErrorMessages ?? Enumerable.Empty<string>(),
                        v2.ErrorMessages ?? Enumerable.Empty<string>()).ToArray());

            var nextInvocation = Expression.Invoke(next, bag.Parameter);
            /*
             * var=invoke exp1
             * var2=invoke exp2
             * if((var==success && var2==success) or (var != success && var2 != success))
             *      var=fail
             * else
             *      var= invoke next
             * return var
             *
             */
            return Expression.Block(
                new ParameterExpression[]
                {
                    bag.Variable,
                    bag.Variable2
                },
                Expression.Assign(
                    bag.Variable,
                    Expression.Invoke(
                        exp1,
                        bag.Parameter
                    )
                ),
                Expression.Assign(
                    bag.Variable2,
                    Expression.Invoke(
                        exp2,
                        bag.Parameter
                        )
                    ),
                Expression.IfThenElse(
                    Expression.OrElse(
                        Expression.AndAlso(
                            Expression.NotEqual(bag.Variable, bag.Success),
                            Expression.NotEqual(bag.Variable2, bag.Success)),
                        Expression.AndAlso(
                            Expression.Equal(bag.Variable,bag.Success),
                            Expression.Equal(bag.Variable2, bag.Success))),
                    Expression.Assign(bag.Variable,Expression.Constant(ValidationResult.Failure(link.ErrorMessages.ToArray()),typeof(ValidationResult))),
                    Expression.Assign(bag.Variable,nextInvocation)),
                bag.Variable
            );

        }
        [DebuggerStepThrough]
        private static Expression CreateChainExpression<T>(ValidationLink<T> link, Expression next, ExpressionBag<T> bag, ValidationFabric<T> fabric)
        {
            if (fabric == null)
                throw new ArgumentException("Cannot compile a chan that references another chain from the fabric when fabric is null", nameof(fabric));
            if (fabric[link.ChainName] is ValidationChain<T> c)
            {
                c.CompileRecursive(fabric);
                Expression<Func<ValidationResult, int>> errorCount = v =>
                    v.ErrorMessages == null ? 0 : v.ErrorMessages.Count;
                return Expression.Block(
                    new ParameterExpression[] { bag.Variable },
                    Expression.Assign(
                        bag.Variable,
                        Expression.Invoke(
                            c.Expression,
                            bag.Parameter)
                        ),
                    Expression.IfThenElse(
                        Expression.Equal(
                            bag.Variable,
                            bag.Success),
                        Expression.Assign(
                            bag.Variable,
                            Expression.Invoke(
                                next,
                                bag.Parameter)
                            ),
                        Expression.IfThen(
                            Expression.Equal(
                                bag.ZeroConstant,
                                Expression.Invoke(
                                    errorCount,
                                    bag.Variable)
                                ),
                            Expression.Assign(
                                bag.Variable,
                                Expression.Constant(
                                    ValidationResult.Failure(link.ErrorMessages.ToArray()),
                                    typeof(ValidationResult))
                                )
                            )
                        ),
                    bag.Variable
                    );
            }
            else
            {
                throw new ArgumentException($"The chain with the name {link.ChainName} does not exist in the fabric.", nameof(link.ChainName));
            }
            
        }
        [DebuggerStepThrough]
        private static BlockExpression CreateNodeExpression<T>(ValidationLink<T> link, Expression next, ExpressionBag<T> bag)
        {
            return Expression.Block(new ParameterExpression[] { bag.Variable },
                Expression.IfThenElse(Expression.Invoke(link.Link, bag.Parameter),
                    Expression.Assign(bag.Variable, Expression.Invoke(next, bag.Parameter)),
                    Expression.Assign(bag.Variable,
                        Expression.Constant(ValidationResult.Failure(link.ErrorMessages.ToArray()), typeof(ValidationResult)))
                ),
                bag.Variable
            );
        }
        [DebuggerStepThrough]
        internal static void CompileRecursive<T>(this ValidationChain<T> chain,
            ValidationFabric<T> fabric)
        {
            if (chain.IsCompiled)
                return;

            var bag=new ExpressionBag<T>();


            //LAST node
            var temp =
                CreateExpression<T>(
                    chain.InvocationChain[chain.InvocationChain.Count - 1],
                    bag.SuccessLambda,
                    bag,
                    fabric);

            for (int i = chain.InvocationChain.Count - 2; i >= 0; i--)
            {
                //intermediate nodes
                temp = CreateExpression<T>(
                    chain.InvocationChain[i],
                    temp,
                    bag,
                    fabric);
            }

            chain.Expression = temp;
            

        }

        /// <summary>
        /// Creates a new link combining the left-hand and right-hand link.
        /// This is a logical OR link type
        /// </summary>
        /// <typeparam name="T">the type of model</typeparam>
        /// <param name="left">left hand side</param>
        /// <param name="right">right hand side</param>
        /// <returns>a new OR link</returns>
        public static ValidationLink<T> Or<T>(this ValidationLink<T> left, ValidationLink<T> right)
        {
            return ValidationLink<T>.OrBranch(left, right);
        }

        /// <summary>
        /// Creates a new link combining the left-hand and right-hand link.
        /// This is a logical XOR link type
        /// </summary>
        /// <typeparam name="T">the type of model</typeparam>
        /// <param name="left">left hand side</param>
        /// <param name="right">right hand side</param>
        /// <returns>a new XOR link</returns>
        public static ValidationLink<T> Xor<T>(this ValidationLink<T> left, ValidationLink<T> right)
        {
            return ValidationLink<T>.XorBranch(left,right);
        }

    }
}