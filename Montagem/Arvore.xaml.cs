using GCM_Offline;
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

namespace Montagem
{
    /// <summary>
    /// Interaction logic for Arvore.xaml
    /// </summary>
    public partial class Arvore : Window
    {
        public Arvore(List<Avanco> avancos)
        {
            InitializeComponent();
            this.lista.ItemsSource = avancos;
        }
    }
}
