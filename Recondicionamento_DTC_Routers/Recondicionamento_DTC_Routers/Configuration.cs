using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Recondicionamento_DTC_Routers
{
    public static class Configuration
    {
        public static ConfigurationsValues configurationValues = new ConfigurationsValues();
        

        public class ConfigurationsValues
        {
            

           public static string _ConfigFW = GetSlnRoot();
           public static string _log = GetSlnRoot();
           public static string _report = GetSlnRoot();
           public static string _ip = @"10.127.159.38";
           public static string _routerPort = @"8011";
           public static string _routerPortRS232 = @"1232";
           public static string _routerPortRS485 = @"1485";
           public static string _routerUser = @"admin";
           // string _routerPass = @"Passwd@02"; //ziv
           public static string _routerPass = @"admin"; //teldat
           public static string _dtcPort = @"80";
           public static string _dtcUser = @"admin";
           public static string _dtcPass = @"passwd02";
           public static string _ns_emi = @"KFM2200000026";
           public static string _portRS232 = @"1232";
           public static string _portRS485 = @"1485";

            public string ip { get => _ip; set => _ip = value; }
            public string routerPort { get => _routerPort; set => _routerPort = value; }
            public string routerUser { get => _routerUser; set => _routerUser = value; }
            public string routerPass { get => _routerPass; set => _routerPass = value; }
            public string Path_ConfigFW { get => _ConfigFW; set => _ConfigFW = value; }
            public string Path_log { get => _log; set => _log = value; }
            public string Path_report { get => _report; set => _report = value; }
            public string portRS232 { get => _portRS232; set => _portRS232 = value; }
            public string portRS485 { get => _portRS485; set => _portRS485 = value; }
            public string dtcPort { get => _dtcPort; set => _dtcPort = value; }
            public string dtcUser { get => _dtcUser; set => _dtcUser = value; }
            public string dtcPass { get => _dtcPass; set => _dtcPass = value; }


            public string ns_emi { get => _ns_emi; set => _ns_emi = value; }

        }




        public static void SaveSettings()
        {
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(configurationValues.GetType());
            using (var tw = new StreamWriter("Configurations.xml"))
            {
                x.Serialize(tw, configurationValues);
            }

        }

        public static void LoadSettings()
        {
            try
            {
                string filename = "Configurations.xml";
                var fileInfo = new FileInfo(filename);
                if (fileInfo.Exists == true)
                {
                    XmlSerializer xs = new XmlSerializer(typeof(ConfigurationsValues));
                    using (var sr = new StreamReader(filename))
                    {
                        configurationValues = (ConfigurationsValues)xs.Deserialize(sr);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static string GetSlnRoot()
        {
            // começa no bin\Debug\netX\
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            while (dir != null)
            {
                // marcador 1: .sln (ideal)
                var sln = dir.GetFiles("*.sln").FirstOrDefault();
                if (sln != null)
                    return dir.FullName + "\\";

                // marcador 2 (se não houver sln ao lado): pastas que tu tens na root
                if (Directory.Exists(Path.Combine(dir.FullName, "Firmware")) &&
                    Directory.Exists(Path.Combine(dir.FullName, "Configuracao")))
                    return dir.FullName + "\\";

                dir = dir.Parent;
            }

            // fallback: não crasha
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string GetResultadosDir()
        {
            var baseDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            for (var d = baseDir; d != null; d = d.Parent)
            {
                // tenta achar a root pela existência de um .sln
                if (d.GetFiles("*.sln").Any())
                {
                    var res = Path.Combine(d.FullName, "resultados");
                    Directory.CreateDirectory(res);
                    return res;
                }
            }

            // fallback: se não encontrar .sln, cria resultados ao lado do EXE
            var fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resultados");
            Directory.CreateDirectory(fallback);
            return fallback;
        }

        public static string TryGetEmiIdDc()
        {
            try
            {
                var cv = Configuration.configurationValues;
                if (cv == null) return "";

                var p = cv.ns_emi;
                return p;
            }
            catch
            {
                return "";
            }
        }


    }
}
