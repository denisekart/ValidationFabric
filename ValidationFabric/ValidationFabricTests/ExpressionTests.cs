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

            var chain= new ValidationChain<Test>().Add(x => true).Add(x=>true).Add(x => true).Add(x => true)
                .Add(x => true).Add(x => true).Add(x => true).Add(x => true)
                .Add(x => true).Add(x => true).Add(x => true).Add(x => true)
                .Add(x => true).Add(x => true).Add(x => true).Add(x => true)
                .Add(x => true).Add(x => true).Add(x => true).Add(x => true)
                .Add(x => true).Add(x => true).Add(x => true).Add(x => true)
                .Add(x => true).Add(x => true).Add(x => true).Add(x => true)
                .Add(x => true).Add(x => true).Add(x => true).Add(x => true)
                .Add(x => true).Add(x => true).Add(x => true).Add(x => true)
                .Add(x => true).Add(x => true).Add(x => true).Add(x => true)
                .Add(x => true).Add(x => true).Add(x => true).Add(x => true)
                .Add(x => true).Add(x => true).Add(x => true).Add(x => true)
                .Add(x => true).Add(x => true).Add(x => true).Add(x => true)
                .Add(x => true).Add(x => true).Add(x => true).Add(x => true)
                .Add(x => true).Add(x => true).Add(x => true).Add(x => true)
                .Add(x => true).Add(x => true).Add(x => true).Add(x => true)
                .Add(x => true).Add(x => true).Add(x => true).Add(x => true)
                .Add(x => true).Add(x => true).Add(x => true).Add(x => true);
            //var chain2 = new ValidationChain<Test>().Add(x => true).AddError("c101");
            //fab.Add(chain).Add(chain2);

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
            fab.Add(ValidationChain.Create<Test>("c1").Add(x => true))
                .Add(ValidationChain.Create<Test>("c2").Add(x => true).Add("c1"))
                .Add(ValidationChain.Create<Test>("c3").Add(x => true).Add("c2"))
                .Add(ValidationChain.Create<Test>("c4").Add(x => true).Add("c3"))
                .Add(ValidationChain.Create<Test>("c5").Add(x => true).Add("c4"))
                .Add(ValidationChain.Create<Test>("c6").Add(x => true).Add("c5"))
                .Add(ValidationChain.Create<Test>("c7").Add(x => true).Add("c6"));
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
            fab.Add(ValidationChain.Create<Test>("c1").Add(x => true))
                .Add(ValidationChain.Create<Test>("c2").Add(x => true))
                .Add(ValidationChain.Create<Test>("c3").Add(x => true))
                .Add(ValidationChain.Create<Test>("c4").Add(x => true))
                .Add(ValidationChain.Create<Test>("c5").Add(x => true))
                .Add(ValidationChain.Create<Test>("c6").Add(x => true)
                    .Add(ValidationLink<Test>.Create(x => true) ^
                             ValidationLink<Test>.Create(x => true)))
                .Add(ValidationChain.Create<Test>("c7").Add(x => true)
                    .Add(ValidationLink<Test>.Create(x=>true) | 
                             ValidationLink<Test>.Create(x => true)));
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
            fabric["chain0"] = new ValidationChain<object>().Add(x => true).AddError("c101");
            fabric["chain1"] = new ValidationChain<object>().Add(x => true).AddError("c11")
                .Add(x => false);
            fabric["chain2"] = new ValidationChain<object>().Add(x => true).Add("chain0")
                .Add("chain1").AddError("chainerr");


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
            
            fab.Add(ValidationChain.Create<Test>("c1").SetMember(x => x.P1).SetCondition(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1")
                .Add(x => x.P2 == "3").AddError("P2 is not 3"));
            fab.Add(ValidationChain.Create<Test>("c2").SetMember(x => x.P2).Add(x => x.P1 == "1")
                .Add("c1"));
            fab.Add(ValidationChain.Create<Test>().SetMember(x => x.P3).Add(x => x.P1 == "1").Add("c1"));
            fab.Add(ValidationChain.Create<Test>("c3").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
            fab.Add(ValidationChain.Create<Test>("c3").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
            fab.Add(ValidationChain.Create<Test>("c3").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
            fab.Add(ValidationChain.Create<Test>("c3").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
            fab.Add(ValidationChain.Create<Test>("c3").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
            fab.Add(ValidationChain.Create<Test>("c3").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
            fab.Add(ValidationChain.Create<Test>("c3").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
            fab.Add(ValidationChain.Create<Test>("c3").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
            fab.Add(ValidationChain.Create<Test>("c3").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
            fab.Add(ValidationChain.Create<Test>("c3").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
            fab.Add(ValidationChain.Create<Test>("c3").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
            fab.Add(ValidationChain.Create<Test>("c3").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
            fab.Add(ValidationChain.Create<Test>("c3").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
            fab.Add(ValidationChain.Create<Test>("c3").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
            fab.Add(ValidationChain.Create<Test>("c3").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
            fab.Add(ValidationChain.Create<Test>("c3").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
            fab.Add(ValidationChain.Create<Test>("c3").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
            fab.Add(ValidationChain.Create<Test>("non").SetMember(x => x).SetCondition(x => false).Add(x => x.P1 == "1").Add("c1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1").Add(x => x.P1 == "1"));
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
        //    ValidationChainExtensions.CreateWrapper<Test>(test => ValidationResult.Success,
        //        test => ValidationResult.Success, new ValidationChainExtensions.ExpressionBag<Test>());

        //}
    }
}
