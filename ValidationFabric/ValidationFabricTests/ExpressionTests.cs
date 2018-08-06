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
        public void RecursiveCompileTest()
        {
            var fab=new ValidationFabric<Test>();

            var chain= new ValidationChain<Test>().AddLink(x => true).AddLink(x=>true).AddLink(x => true).AddLink(x => true)
                .AddLink(x => true).AddLink(x => true).AddLink(x => true).AddLink(x => true)
                .AddLink(x => true).AddLink(x => true).AddLink(x => true).AddLink(x => true)
                .AddLink(x => true).AddLink(x => true).AddLink(x => true).AddLink(x => true)
                .AddLink(x => true).AddLink(x => true).AddLink(x => true).AddLink(x => true)
                .AddLink(x => true).AddLink(x => true).AddLink(x => true).AddLink(x => true)
                .AddLink(x => true).AddLink(x => true).AddLink(x => true).AddLink(x => true)
                .AddLink(x => true).AddLink(x => true).AddLink(x => true).AddLink(x => true)
                .AddLink(x => true).AddLink(x => true).AddLink(x => true).AddLink(x => true)
                .AddLink(x => true).AddLink(x => true).AddLink(x => true).AddLink(x => true)
                .AddLink(x => true).AddLink(x => true).AddLink(x => true).AddLink(x => true)
                .AddLink(x => true).AddLink(x => true).AddLink(x => true).AddLink(x => true)
                .AddLink(x => true).AddLink(x => true).AddLink(x => true).AddLink(x => true)
                .AddLink(x => true).AddLink(x => true).AddLink(x => true).AddLink(x => true)
                .AddLink(x => true).AddLink(x => true).AddLink(x => true).AddLink(x => true)
                .AddLink(x => true).AddLink(x => true).AddLink(x => true).AddLink(x => true)
                .AddLink(x => true).AddLink(x => true).AddLink(x => true).AddLink(x => true)
                .AddLink(x => true).AddLink(x => true).AddLink(x => true).AddLink(x => true);
            //var chain2 = new ValidationChain<Test>().AddLink(x => true).AddErrorMessage("c101");
            //fab.AddChain(chain).AddChain(chain2);

            chain.CompileRecursive(fab);
            var res=chain.Invoke(null);


            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
                res = chain.Invoke(null);
            sw.Stop();
            var total = sw.ElapsedMilliseconds;
        }


        [Fact]
        public void RecursiveCompileChainNameLinkTest()
        {
            var fab=new ValidationFabric<Test>();
            fab.AddChain(ValidationChain.EmptyChain<Test>("c1").AddLink(x => true))
                .AddChain(ValidationChain.EmptyChain<Test>("c2").AddLink(x => true).AddChain("c1"))
                .AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => true).AddChain("c2"))
                .AddChain(ValidationChain.EmptyChain<Test>("c4").AddLink(x => true).AddChain("c3"))
                .AddChain(ValidationChain.EmptyChain<Test>("c5").AddLink(x => true).AddChain("c4"))
                .AddChain(ValidationChain.EmptyChain<Test>("c6").AddLink(x => true).AddChain("c5"))
                .AddChain(ValidationChain.EmptyChain<Test>("c7").AddLink(x => true).AddChain("c6"));
            fab["c7"].CompileRecursive(fab);

            var result=fab["c7"].Invoke(null);



            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
                result = fab["c7"].Invoke(null);
            sw.Stop();
            var total = sw.ElapsedMilliseconds;

        }


        [Fact]
        public void RecursiveCompileLogicalOrLinkTest()
        {
            var fab = new ValidationFabric<Test>();
            fab.AddChain(ValidationChain.EmptyChain<Test>("c1").AddLink(x => true))
                .AddChain(ValidationChain.EmptyChain<Test>("c2").AddLink(x => true))
                .AddChain(ValidationChain.EmptyChain<Test>("c3").AddLink(x => true))
                .AddChain(ValidationChain.EmptyChain<Test>("c4").AddLink(x => true))
                .AddChain(ValidationChain.EmptyChain<Test>("c5").AddLink(x => true))
                .AddChain(ValidationChain.EmptyChain<Test>("c6").AddLink(x => true)
                    .AddLink(ValidationLink<Test>.Expression(x => true) ^
                             ValidationLink<Test>.Expression(x => true)))
                .AddChain(ValidationChain.EmptyChain<Test>("c7").AddLink(x => true)
                    .AddLink(ValidationLink<Test>.Expression(x=>true) | 
                             ValidationLink<Test>.Expression(x => true)));
            //fab["c7"].CompileRecursive(fab);

            //var result = fab["c7"].Invoke2(null);

            fab["c6"].CompileRecursive(fab);

            var result = fab["c6"].Invoke(null);




            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
                result = fab["c6"].Invoke(null);
            sw.Stop();
            var total = sw.ElapsedMilliseconds;

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

            Assert.Throws<AccessViolationException>(() =>
            {
                var result = fabric["chain2"].Invoke(new object());
            });


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

                var r11 = fab.Validate(t);
                var r21 = fab.Validate(t);
                var r31 = fab.Validate(t);
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
}
