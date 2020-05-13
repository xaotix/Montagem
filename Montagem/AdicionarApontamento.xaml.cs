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
    /// Interação lógica para AdicionarApontamento.xam
    /// </summary>
    public partial class AdicionarApontamento : Window
    {
        public Apontamento apontamento { get; set; } = new Apontamento();
        public AdicionarApontamento(bool efetivo = false, bool valor = true, bool responsavel = false, Apontamento apontamento = null, bool data = true)
        {
            if (apontamento != null)
            {
                this.apontamento.data = apontamento.data;
                this.apontamento.descricao = apontamento.descricao;
                this.apontamento.valor = apontamento.valor;
                this.apontamento.efetivo = apontamento.efetivo;
                this.apontamento.responsavel = apontamento.responsavel;
            }
            InitializeComponent();
            if(!efetivo)
            {
                txt_efetivo.Visibility = Visibility.Collapsed;
                lbl_efetivo.Visibility = Visibility.Collapsed;
            }
            if(!valor)
            {
                txt_valor.Visibility = Visibility.Collapsed;
                lbl_valor.Visibility = Visibility.Collapsed;
            }
            if (!data)
            {
                txt_data.Visibility = Visibility.Collapsed;
                lbl_data.Visibility = Visibility.Collapsed;
            }
            if (!responsavel)
            {
                txt_responsavel.Visibility = Visibility.Collapsed;
                lbl_responsavel.Visibility = Visibility.Collapsed;
            }
            if(!this.apontamento.data.valido)
            {
                this.apontamento.data = new Data(DateTime.Now);
            }
            this.DataContext = this.apontamento;
        }

        private void confirmar(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void cantelar(object sender, RoutedEventArgs e)
        {
            try
            {
                this.DialogResult = false;

            }
            catch (Exception)
            {

            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
         
        }
    }
}
