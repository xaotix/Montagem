using FirstFloor.ModernUI.Windows.Controls;
using GCM_Offline;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;

namespace Montagem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        public List<Obra> obras { get; set; } = new List<Obra>();
        public MainWindow()
        {
            InitializeComponent();
            this.Title = "Apontamento Montagem v." + System.Windows.Forms.Application.ProductVersion;
            UpdateObras();

        }

        private void UpdateObras()
        {
            this.lista.ItemsSource = null;
            this.obras.Clear();
            var obs = Conexoes.Utilz.GetPastas(GCM_Offline.Vars.Raiz);
            foreach (var obra in obs)
            {
                var pp = new Obra().Carregar(obra);
                this.obras.Add(pp);
            }
            this.lista.ItemsSource = this.obras;
        }



        private void criar_obra(object sender, RoutedEventArgs e)
        {
            var obra = new Obra();

        retentar:
            string contrato = "";
            contrato = Conexoes.Utilz.Prompt("Digite o pedido da obra", "10-123456.P00", contrato, false, "", false, 13);
            if (contrato.Length != 13)
            {
                if (Conexoes.Utilz.Pergunta("Pedido inválido. Deve conter 13 Caracteres. Tentar novamente?"))
                    goto retentar;
            }
            obra.contrato = contrato;
            bool status = false;
            Conexoes.Utilz.Prompt(obra,out status,"Nova Obra");
            if(!status)
            {
                return;
            }
            if (obra.nome_obra.Length == 0)
            {
                if (Conexoes.Utilz.Pergunta("Nome da obra em branco. Tentar novamente?"))
                    goto retentar;
            }

            if (obra.contrato.Length != 13)
            {
                if (Conexoes.Utilz.Pergunta("Pedido inválido. Deve conter 13 Caracteres. Tentar novamente?"))
                    goto retentar;
            }
            if(this.obras.Find(x=>x.contrato.ToUpper() == obra.contrato.ToUpper())!=null)
            {
                if (Conexoes.Utilz.Pergunta("Já existe uma obra com este pedido. Tentar novamente?"))
                    goto retentar;
            }

            else if (obra.contrato.Length == 0)
            {
                if (Conexoes.Utilz.Pergunta("Contrato em branco. Tentar novamente?"))
                    goto retentar;
            }

            else if (Directory.Exists(GCM_Offline.Vars.Raiz + obra.contrato))
            {
                if (Conexoes.Utilz.Pergunta("Já existe uma obra com esse contrato. Tentar novamente?"))
                    goto retentar;
            }

            else if (Conexoes.Utilz.Pergunta("Deseja criar a obra " + obra.ToString()))
            {
                var pasta = Conexoes.Utilz.CriarPasta(GCM_Offline.Vars.Raiz, obra.contrato);
                obra.Salvar(pasta);
                UpdateObras();
            }
            
        }

        private void excluir(object sender, RoutedEventArgs e)
        {
            var sel = lista.SelectedItems.Cast<Obra>().ToList();
            if(sel.Count>0)
            {
                if(Conexoes.Utilz.Pergunta("Tem certeza que deseja excluir as obras selecionadas?"))
                {
                    foreach(var s in sel)
                    {
                        var dir = new DirectoryInfo(s.diretorio);
                        //dir.Attributes = dir.Attributes & ~FileAttributes.ReadOnly;
                        dir.Delete(true);
                    }
                    UpdateObras();
                }
            }
        }

        private void abre_pasta(object sender, RoutedEventArgs e)
        {
            Conexoes.Utilz.Abrir(GCM_Offline.Vars.Raiz);
        }

        private void editar_obra(object sender, RoutedEventArgs e)
        {
            var sel = lista.SelectedItems.Cast<Obra>().ToList();
            if(sel.Count>0)
            {
                Conexoes.Utilz.Propriedades(sel[0], true);
                sel[0].Salvar(sel[0].diretorio);
                UpdateObras();
            }
        }

        private void lista_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var ss = lista.SelectedItems.Cast<Obra>().ToList();
            if(ss.Count>0)
            {
              
                JanelaObra2 mm = new JanelaObra2(ss[0]);
                mm.Closed += Mm_Closed;
                this.Visibility = Visibility.Collapsed;
                mm.Show();
            }
        }

        private void Mm_Closed(object sender, EventArgs e)
        {
            this.Visibility = Visibility.Visible;
        }

        private void ModernWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var arq = Conexoes.Utilz.Abrir_String();
            if(File.Exists(arq))
            {
                Conexoes.Utilz.VerXML(arq);
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            var dest = Conexoes.Utilz.SalvarArquivo("xlsm");
            if(dest!="" && dest!=null)
            {
             var ss =   Conexoes.Utilz.Copiar(Vars.template_lob, dest);
                if(ss)
                {
                    Conexoes.Utilz.Abrir(dest);
                }
            }
        }
    }
}