using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GCM_Offline;

namespace Montagem
{
    /// <summary>
    /// Interaction logic for Editar_Pesos_Avanco_Fisico.xaml
    /// </summary>
    public partial class Editar_Pesos_Avanco_Fisico : ModernWindow
    {
        public Linha_de_Balanco lob { get; set; } = new Linha_de_Balanco();
        public Editar_Pesos_Avanco_Fisico(Linha_de_Balanco lob)
        {
            this.lob = lob;
            InitializeComponent();
            this.lista_custos.ItemsSource = this.lob.recursos_custo;
            this.lista_pesos.ItemsSource = this.lob.fases_pesos_avanco_fisico;
        }
    }
}
