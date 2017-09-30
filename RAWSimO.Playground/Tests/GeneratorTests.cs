using RAWSimO.Core.Configurations;
using RAWSimO.Core.Generator;
using RAWSimO.Core.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RAWSimO.Playground.Tests
{
    public class GeneratorTests
    {
        public static void TestGenerateDefaultSimpleItemConfig()
        {
            SimpleItemGeneratorConfiguration config = OrderGenerator.GenerateSimpleItemConfiguration(new OrderGenerator.SimpleItemGeneratorPreConfiguration()
            {
                ItemDescriptionCount = 100,
                DefaultWeight = 1,
                DefaultCoWeight = 1,
                ProbabilityWeightNormalMu = 1,
                ProbabilityWeightNormalSigma = 3,
                ItemWeightLB = 1,
                ItemWeightUB = 7,
                ItemWeightMu = 2,
                ItemWeightSigma = 1,
                GivenCoWeights = 1
            });
            InstanceIO.WriteSimpleItemGeneratorConfigFile("simpleitemgeneratorconfig.xml", config);
        }
    }
}
