using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValidationFabric;
using Xunit;

namespace ValidationFabricTests
{
    public class ValidationFabricTests
    {
        [Fact]
        public void CreateAndCompileFabricWithAllTypesOfChains()
        {
            ValidationFabric<Test> fabric = new ValidationFabric<Test>()
                .AddChain(ValidationChain.EmptyChain<Test>("c1").AddLink(x => true))
                .AddChain(ValidationChain.EmptyChain<Test>("c2").AddChain("c1"))
                .AddChain(ValidationChain.EmptyChain<Test>("c3")
                    .AddLink(ValidationLink<Test>.Expression(x => true) | ValidationLink<Test>.Expression(x => true)))
                .AddChain(ValidationChain.EmptyChain<Test>("c4")
                    .AddLink(ValidationLink<Test>.Expression(x => true) ^ ValidationLink<Test>.Expression(x => true)))
                .AddChain(ValidationChain.EmptyChain<Test>("c5")
                    .AddLink(ValidationLink<Test>.Expression(x => true) ^ ValidationLink<Test>.Expression(x => true)))
                .AddChain(ValidationChain.EmptyChain<Test>("c6")
                    .AddLink(ValidationLink<Test>.Expression(x => true) ^ ValidationLink<Test>.Expression(x => true)))
                .AddChain(ValidationChain.EmptyChain<Test>("c7")
                    .AddLink(ValidationLink<Test>.Expression(x => true) ^ ValidationLink<Test>.Expression(x => true)));
            fabric.CompileAheadOfTime();
        }
    }
}
