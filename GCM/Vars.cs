using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCM_Offline
{
    public class Vars
    {
        public static string versionamento
        {
            get
            {
                return System.Windows.Forms.Application.StartupPath + @"\versionamento.txt";
            }
        }
        public static string template_avanco
        {
            get
            {
                return System.Windows.Forms.Application.StartupPath + @"\template_saida.xlsx";
            }
        }

        public static string template_lob
        {
            get
            {
                return System.Windows.Forms.Application.StartupPath + @"\Modelo_LOB.xlsm";
            }
        }

        public static string Raiz { get; set; } = Conexoes.Utilz.CriarPasta(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\","OBRAS_MEDABIL");
        public static string RaizFTP { get; set; } = "/ENGENHEIROS_DE_OBRAS";

        public static int CompRandom { get; set; } = 25;
        public class LOB
        {
        public static int linha0 = 15;
           
        }
        public static int max_etapas { get; set; } = 200;
        public static int max_dias { get; set; } = 600;
    }
}
