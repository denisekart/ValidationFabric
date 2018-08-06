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
                .Add(ValidationChain.Create<Test>("c1").Add(x => true))
                .Add(ValidationChain.Create<Test>("c2").Add("c1"))
                .Add(ValidationChain.Create<Test>("c3")
                    .Add(ValidationLink<Test>.Create(x => true) | ValidationLink<Test>.Create(x => true)))
                .Add(ValidationChain.Create<Test>("c4")
                    .Add(ValidationLink<Test>.Create(x => true) ^ ValidationLink<Test>.Create(x => true)))
                .Add(ValidationChain.Create<Test>("c5")
                    .Add(ValidationLink<Test>.Create(x => true) ^ ValidationLink<Test>.Create(x => true)))
                .Add(ValidationChain.Create<Test>("c6")
                    .Add(ValidationLink<Test>.Create(x => true) ^ ValidationLink<Test>.Create(x => true)))
                .Add(ValidationChain.Create<Test>("c7")
                    .Add(ValidationLink<Test>.Create(x => true) ^ ValidationLink<Test>.Create(x => true)));
            fabric.CompileAheadOfTime();
        }
    }
}
