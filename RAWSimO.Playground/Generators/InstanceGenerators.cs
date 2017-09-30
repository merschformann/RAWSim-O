using RAWSimO.Core;
using RAWSimO.Core.Configurations;
using RAWSimO.Core.Generator;
using RAWSimO.Core.IO;
using RAWSimO.Core.Randomization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Playground.Generators
{
    class InstanceGenerators
    {
        public static void GenerateMaTiInstances()
        {
            // Generate MaTi instances
            RandomizerSimple random = new RandomizerSimple(0);
            // Generate original MaTi set
            Instance tiny = InstanceGenerator.GenerateMaTiLayoutTiny(random, new SettingConfiguration(), new ControlConfiguration());
            Instance small = InstanceGenerator.GenerateMaTiLayoutSmall(random, new SettingConfiguration(), new ControlConfiguration());
            Instance medium = InstanceGenerator.GenerateMaTiLayoutMedium(random, new SettingConfiguration(), new ControlConfiguration());
            Instance large = InstanceGenerator.GenerateMaTiLayoutLarge(random, new SettingConfiguration(), new ControlConfiguration());
            Instance huge = InstanceGenerator.GenerateMaTiLayoutHuge(random, new SettingConfiguration(), new ControlConfiguration());
            InstanceIO.WriteInstance("MaTiTiny.xinst", tiny);
            InstanceIO.WriteInstance("MaTiSmall.xinst", small);
            InstanceIO.WriteInstance("MaTiMedium.xinst", medium);
            InstanceIO.WriteInstance("MaTiLarge.xinst", large);
            InstanceIO.WriteInstance("MaTiHuge.xinst", huge);
            // Generate alternative MaTi set
            InstanceIO.WriteInstance("MaTiPico.xinst", InstanceGenerator.GenerateMaTiLayoutPico(random, new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiNano.xinst", InstanceGenerator.GenerateMaTiLayoutNano(random, new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiMicro.xinst", InstanceGenerator.GenerateMaTiLayoutMicro(random, new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiMilli.xinst", InstanceGenerator.GenerateMaTiLayoutMilli(random, new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiCenti.xinst", InstanceGenerator.GenerateMaTiLayoutCenti(random, new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiDeca.xinst", InstanceGenerator.GenerateMaTiLayoutDeca(random, new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiHecto.xinst", InstanceGenerator.GenerateMaTiLayoutHecto(random, new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiKilo.xinst", InstanceGenerator.GenerateMaTiLayoutKilo(random, new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiMega.xinst", InstanceGenerator.GenerateMaTiLayoutMega(random, new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiGiga.xinst", InstanceGenerator.GenerateMaTiLayoutGiga(random, new SettingConfiguration(), new ControlConfiguration()));
        }
    }
}
