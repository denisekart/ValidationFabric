using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
            ValidationChain<object> chain = new ValidationChain<object>();
            chain.AddLink(x => true).AddErrorMessage("test");
            var invoker = chain.CompileTree(null);
            var result = invoker.Invoke(new object());
            //invoker.InvokeTest(new object());
            Assert.Equal(ValidationResult.Success, result);
        }


        [Fact]
        public void ExpressionCompileMultipleLinksSuccessTest()
        {
            ValidationChain<object> chain = new ValidationChain<object>();
            chain.AddLink(x => true).AddErrorMessage("test").AddLink(x => true).AddErrorMessage("ok");
            var invoker = chain.CompileTree(null);
            var result = invoker.Invoke(new object());
            Assert.Equal(ValidationResult.Success, result);
        }



        [Fact]
        public void FabricSimpleValidation()
        {
            ValidationFabric<object> fabric = new ValidationFabric<object>();
            fabric["chain0"] = new ValidationChain<object>().AddLink(x => true).AddErrorMessage("c101");
            fabric["chain1"] = new ValidationChain<object>().AddLink(x => true).AddErrorMessage("c11")
                .AddLink(x => false);
            fabric["chain2"] = new ValidationChain<object>().AddLink(x => true).AddChain("chain0")
                .AddChain("chain1").AddErrorMessage("chainerr");


            //c12 c11 c21

            var result = fabric["chain2"].Invoke(new object());


        }

        [Fact]
        public void CompleteActivationTest()
        {
            var t = new Test
            {
                P1 = "1",
                P2 = "2",
                P3 = 3
            };

            ValidationFabric<Test> fab = new ValidationFabric<Test>();
            fab.AddChain(ValidationChain.EmptyChain<Test>("c1").SetMember(x => x.P1).SetCondition(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1")
                .AddLink(x => x.P2 == "3").AddErrorMessage("P2 is not 3"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c2").SetMember(x => x.P2).AddLink(x => x.P1 == "1")
                .AddChain("c1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>().SetMember(x => x.P3).AddLink(x => x.P1 == "1").AddChain("c1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            fab.AddChain(ValidationChain.EmptyChain<Test>("non").SetMember(x => x).SetCondition(x => false).AddLink(x => x.P1 == "1").AddChain("c1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1").AddLink(x => x.P1 == "1"));
            var r01 = fab.Validate(t, x => x.P1);
            var r02 = fab.Validate(t, x => x.P2);
            var r03 = fab.Validate(t, x => x.P3);
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < 10; i++)
            {
                var r1 = fab.Validate(t, x => x.P1);
                var r2 = fab.Validate(t, x => x.P2);
                var r3 = fab.Validate(t, x => x.P3);

            }


            sw.Stop();
            var ms = sw.ElapsedMilliseconds;
            Debug.WriteLine("TIME=== " + ms);
        }



        //[Fact]
        //public void Foo()
        //{
        //    ValidationChainCompiler.CreateWrapper<Test>(test => ValidationResult.Success,
        //        test => ValidationResult.Success, new ValidationChainCompiler.ExpressionBag<Test>());

        //}
    }

    public class Test
    {
        public string P1 { get; set; }
        public string P2 { get; set; }
        public int P3 { get; set; }
    }
}
