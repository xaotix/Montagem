using FirstFloor.ModernUI.Windows.Controls;
using GCM_Online;
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

namespace Importador_Montagem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        public List<Contrato> obras { get; set; } = new List<Contrato>();
        public MainWindow()
        {
            InitializeComponent();
            Update();
        }

        private void Update()
        {
            this.obras = dbase.obras(true);
            this.lista.ItemsSource = null;
            this.lista.ItemsSource = this.obras;
        }

        private void criar_obra(object sender, RoutedEventArgs e)
        {
            Contrato p = new Contrato();
            retentar:
            bool st = false;
            Conexoes.Utilz.Prompt(p, out st, "Preencha os campos",this);
            if (st)
            {
                if(p.contrato=="")
                {
                    if(Conexoes.Utilz.Pergunta("Falta preencher o campo Pedido. Tentar novamente?"))
                    {
                        goto retentar;
                    }
                }
                if (p.contrato.Length != 13)
                {
                    if (Conexoes.Utilz.Pergunta("O campo pedido deve conter 13 caracteres. Tentar novamente?"))
                    {
                        goto retentar;
                    }
                }
                if(this.obras.Find(x=>x.contrato.ToUpper() == p.contrato.ToUpper())!=null)
                {
                    if (Conexoes.Utilz.Pergunta("Já existe uma obra cadastrada com este pedido. Tentar novamente?"))
                    {
                        goto retentar;
                    }
                }
                if (p.descricao == "")
                {
                    
                }
                if (this.obras.Find(x=>x.contrato == p.contrato)!=null)
                {
                    if (Conexoes.Utilz.Pergunta("Já existe uma obra com este contrato. Tentar novamente?"))
                    {
                        goto retentar;
                    }
                }
                p.Salvar();
                Update();
            }

        }

        private void apagar_obra(object sender, RoutedEventArgs e)
        {
            var sel = lista.SelectedItems.Cast<Contrato>().ToList();
            if(sel.Count>0)
            {
                foreach(var s in sel)
                {
                    dbase.Apagar(s);
                }
                Update();
            }
        }
        public List<Contrato> abertos { get; set; } = new List<Contrato>();
        private void lista_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Contrato p = lista.SelectedItem as Contrato;
           
            if(p!=null)
            {
                if (abertos.Find(x => x == p) == null)
                {
                    abertos.Add(p);
                    JanelaObra pps = new JanelaObra(p);
                    pps.Closed += Pps_Closed;
                    pps.Show();
                }
                else
                {
                    MessageBox.Show("Obra " + p.ToString() + " já está aberta em outra janela.");
                }
         
            }
        }

        private void Pps_Closed(object sender, EventArgs e)
        {
            JanelaObra ob = sender as JanelaObra;
            abertos.Remove(ob.lob_online);
        }

        private void ModernWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void visualiza_xml(object sender, RoutedEventArgs e)
        {
            string ss = Conexoes.Utilz.Abrir_String("*.*","","");

            Conexoes.Utilz.VerXML(ss);
        }
    }
}
