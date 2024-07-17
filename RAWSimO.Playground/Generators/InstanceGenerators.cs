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
            // Generate original MaTi set
            InstanceIO.WriteInstance("MaTiTiny.xinst", InstanceGenerator.GenerateMaTiLayoutTiny(new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiSmall.xinst", InstanceGenerator.GenerateMaTiLayoutSmall(new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiMedium.xinst", InstanceGenerator.GenerateMaTiLayoutMedium(new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiLarge.xinst", InstanceGenerator.GenerateMaTiLayoutLarge(new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiHuge.xinst", InstanceGenerator.GenerateMaTiLayoutHuge(new SettingConfiguration(), new ControlConfiguration()));
            // Generate alternative MaTi set
            InstanceIO.WriteInstance("MaTiPico.xinst", InstanceGenerator.GenerateMaTiLayoutPico(new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiNano.xinst", InstanceGenerator.GenerateMaTiLayoutNano(new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiMicro.xinst", InstanceGenerator.GenerateMaTiLayoutMicro(new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiMilli.xinst", InstanceGenerator.GenerateMaTiLayoutMilli(new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiCenti.xinst", InstanceGenerator.GenerateMaTiLayoutCenti(new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiDeca.xinst", InstanceGenerator.GenerateMaTiLayoutDeca(new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiHecto.xinst", InstanceGenerator.GenerateMaTiLayoutHecto(new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiKilo.xinst", InstanceGenerator.GenerateMaTiLayoutKilo(new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiMega.xinst", InstanceGenerator.GenerateMaTiLayoutMega(new SettingConfiguration(), new ControlConfiguration()));
            InstanceIO.WriteInstance("MaTiGiga.xinst", InstanceGenerator.GenerateMaTiLayoutGiga(new SettingConfiguration(), new ControlConfiguration()));
        }
    }
}
