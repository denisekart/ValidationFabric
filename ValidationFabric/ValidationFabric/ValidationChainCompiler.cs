using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ValidationFabric
{
    //[DebuggerStepThrough]
    internal static class ValidationChainCompiler
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

            private Expression _zeroConstant;
            public Expression ZeroConstant => _zeroConstant??(_zeroConstant=Expression.Constant(0, typeof(int)));

            private ParameterExpression _variable;
            public ParameterExpression Variable => _variable??
                                                   (_variable=Expression.Variable(typeof(ValidationResult), "var"));

        }

        //public static void CreateWrapper<T>(
        //    Func<T,ValidationResult> inner,
        //    Func<T, ValidationResult> outer,
        //    ExpressionBag<T> bag
        //)
        //{
        //    Expression body=Expression.(
        //        Expression.Lambda<Func<T,ValidationResult>>
        //            (Expression.Constant(inner),bag.Parameter),
        //        inner.Method,
        //        bag.Parameter);
            

        //}

        //static Delegate CreateBehaviorCallDelegate(
        //    Delegate currentBehavior,
        //    MethodInfo methodInfo,
        //    ParameterExpression outerContextParam,
        //    Delegate previous
        //{
        //    // Creates expression for `currentBehavior.Invoke(outerContext, next)`
        //    Expression body = Expression.Call(
        //        instance: Expression.Constant(currentBehavior),
        //        method: methodInfo,
        //        arg0: outerContextParam,
        //        arg1: Expression.Constant(previous));

        //    // Creates lambda expression `outerContext => currentBehavior.Invoke(outerContext, next)`
        //    var lambdaExpression = Expression.Lambda(body, outerContextParam);

        //    // Compile the lambda expression to a Delegate
        //    return lambdaExpression.Compile();
        //}

        //[DebuggerStepThrough]
        internal static Expression CompileLinkExpression<T>(this ValidationLink<T> link, Expression nextLink,
            ValidationFabric<T> fabric, ExpressionBag<T> bag)
        {
            switch (link.Type)
            {
                case ValidationLink<T>.LinkType.Expression:
                    //ta je počasen
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

                        Expression<Func<ValidationResult, int>> errc = v =>
                            v.ErrorMessages == null ? 0 : v.ErrorMessages.Count;


                        //1. var=execute previous
                        //2. if success then invoke next link
                        //3. else
                        //if errored chain has any error messages
                        //then return those messages
                        //else return the messages from the link
                        var block = Expression.Block(
                            new ParameterExpression[] { bag.Variable }, //var
                            Expression.Assign(bag.Variable, invoker), //previous
                            Expression.IfThenElse(//if else
                                Expression.Equal(bag.Variable, bag.Success),
                                Expression.Assign(bag.Variable,
                                    Expression.Invoke(
                                        Expression.Lambda<Func<T, ValidationResult>>(
                                            nextLink,true,
                                            bag.Parameter),
                                        bag.Parameter)),//success - next
                                Expression.IfThen(
                                    Expression.Equal(bag.ZeroConstant,Expression.Invoke(errc,bag.Variable)),
                                    Expression.Assign(bag.Variable,Expression.Constant(ValidationResult.Failure(link.ErrorMessages.ToArray())))
                                )
                            ),//fail -error
                            bag.Variable);

                        return Expression.Invoke(
                            Expression.Lambda<Func<T,ValidationResult>>(
                                block,true,
                                bag.Parameter),
                            bag.Parameter);
                        
                    }
                    throw new ArgumentException($"The chain with the name {link.ChainName} does not exist in the fabric.");
                    
                default:
                    throw new ArgumentException($"Unknown link type {link.Type}");
                    break;
            }
        }
        //[DebuggerStepThrough]
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
                
                expression = Expression.Lambda<Func<T, ValidationResult>>(temp,true,bag.Parameter)
                    .Compile();


                

            }
            
            chain.Compile(expression);
            return chain;
        }

        
    }
}