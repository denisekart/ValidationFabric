using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using ValidationFabric;
using Xunit;

namespace ValidationFabricTests
{
    public class ExpressionTests
    {
        [Fact]
        public void ExpressionCompileSimpleTest()
        {
            ValidationChain<object> chain=new ValidationChain<object>();
            chain.AddLink(x => true).AddErrorMessage("test");
            var invoker=chain.CompileTree(null);
            var result = invoker.Invoke(new object());
            Assert.Equal(ValidationResult.Success,result);
        }

        [Fact]
        public void ExpressionCompileSimpleTestFailure()
        {
            ValidationChain<object> chain = new ValidationChain<object>();
            chain.AddLink(x => false).AddErrorMessage("test");
            var invoker = chain.CompileChain(null);
            var result = invoker.Invoke(new object());
            Assert.Equal(ValidationResult.Failure("test"), result);
        }

        [Fact]
        public void ExpressionCompileMultipleLinksSuccessTest()
        {
            ValidationChain<object> chain = new ValidationChain<object>();
            chain.AddLink(x => false).AddErrorMessage("test").AddLink(x=>false).AddErrorMessage("ok");
            var invoker = chain.CompileTree(null);
            var result = invoker.Invoke(new object());
            Assert.Equal(ValidationResult.Success, result);
        }



        [Fact]
        public void FabricSimpleValidation()
        {
            ValidationFabric<object> fabric=new ValidationFabric<object>();
            fabric["chain0"] = new ValidationChain<object>().AddLink(x => true).AddErrorMessage("c101");
            fabric["chain1"] = new ValidationChain<object>().AddLink(x => true).AddErrorMessage("c11")
                .AddLink(x => false).AddErrorMessage("c12");
            fabric["chain2"] = new ValidationChain<object>().AddLink(x=>true).AddChain("chain0")
                .AddChain("chain1").AddErrorMessage("chainerr");
            fabric.CompileAOT();
            var r1 = fabric["chain1"].Invoke(null);
            var r2 = fabric["chain2"].Invoke(null);


            //c12 c11 c21

            var result=fabric["chain2"].Invoke(new object());
        }

        [Fact]
        public void CreateExpressionWithVariablesTest()
        {
            var p1 = Expression.Variable(typeof(ValidationResult), "vr");
            var px = Expression.Parameter(typeof(object), "entity");
            LabelTarget lbl = Expression.Label(typeof(ValidationResult), "ret");
            Expression<Func<object,ValidationResult >> par = x => ValidationResult.Success;
            var block = Expression.Block(
                new ParameterExpression[] {p1},
                Expression.Assign(p1, Expression.Invoke(par, px)),
                Expression.Assign(p1,Expression.Constant(ValidationResult.Indeterminate)));
            
            var result = Expression.Lambda(block,px);
            var tresult = Expression.Lambda<Func<object, ValidationResult>>(block, px).Compile();
            var act = tresult.Invoke(new object());
        }

        [Fact]
        public void CreateExpressionWithVariablesTest2()
        {
            var tmp = Expression.Variable(typeof(ValidationResult), "tmp_res");
            var param = Expression.Parameter(typeof(object), "e");
            var ret = Expression.Parameter(typeof(ValidationResult));
            Expression<Func<object, ValidationResult>> next = x => ValidationResult.Success;
            var block = Expression.Block(
                new ParameterExpression[] { tmp },
                Expression.Assign(tmp, Expression.Invoke(next, param)),
                Expression.IfThenElse(
                    Expression.Equal(tmp,Expression.Constant(ValidationResult.Success)),
                    Expression.Assign(tmp, Expression.Constant(ValidationResult.Failure("ok"))),
                    Expression.Assign(tmp, Expression.Constant(ValidationResult.Failure("not ok")))),
                tmp);
            

            var lambda = Expression.Lambda<Func<object, ValidationResult>>(block, param).Compile();
            var result = lambda.Invoke(new object());
        }
    }
}
