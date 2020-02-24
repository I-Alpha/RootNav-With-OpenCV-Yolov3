using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime;
using System.Reflection;
using System.IO;

namespace RootNav.Core.MixtureModels
{
    [Serializable]
    public class EMConfiguration
    {
        public string Name { get; set; }
        public int InitialClassCount { get; set; }
        public int MaximumClassCount { get; set; }
        public int ExpectedRootClassCount { get; set; }
        public int PatchSize { get; set; }
        public double BackgroundPercentage { get; set; }
        public double BackgroundExcessSigma { get; set; }
        public double[] Weights { get; set; }
        public Object Default { get; set; }

        public static EMConfiguration[] LoadFromXML()
        {
            /* // Load from embedded resource
            System.IO.Stream s = Assembly.GetCallingAssembly().GetManifestResourceStream("Rootnav.Core.MixtureModels.Configurations.xml");
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(typeof(EMConfiguration[]));
            EMConfiguration[] configs =  x.Deserialize(s) as EMConfiguration[];
            return configs;
            */

            /* Load from file in directory */
            System.IO.Stream s = new FileStream(Directory.GetCurrentDirectory() + "\\Configurations.xml", FileMode.Open);
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(typeof(EMConfiguration[]));
            EMConfiguration[] configs = x.Deserialize(s) as EMConfiguration[];
            return configs;
        }

        public static int DefaultIndex(EMConfiguration[] configs)
        {
            for (int i = 0; i < configs.Length; i++)
            {
                if (configs[i].Default != null)
                {
                    return i;
                }
            }
            return 0;
        }

    }
}
