
using Conexoes.Orcamento;
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
    /// Interação lógica para MenuLancamentos.xam
    /// </summary>
    public partial class MenuLancamentos : Window
    {
        public List<GCM_Offline.Apontamento> apontamentos { get; set; } = new List<GCM_Offline.Apontamento>();
        public MenuLancamentos(List<GCM_Offline.Apontamento> apontamentos)
        {
            this.apontamentos = apontamentos;
            InitializeComponent();
            this.lista.ItemsSource = this.apontamentos;
        }

        private void editar(object sender, RoutedEventArgs e)
        {
            GCM_Offline.Apontamento pp = ((FrameworkElement)sender).DataContext as GCM_Offline.Apontamento;
            if (pp == null)
            {
                return;
            }
            var ss = Funcoes.apontamento("Editar Apontamento", pp,true,false,true,"Equipe de Montagem");
            if(ss!=null)
            {
                pp.Copiar(ss);
            }

        }

        private void soma_valores(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<GCM_Offline.Apontamento>().ToList();
            if (pcs.Count == 0)
            {

                pcs = Conexoes.Utilz.SelecionarObjetos(this.apontamentos,true, "Selecione", this);
                if (pcs.Count == 0) { return; }
            }

            Apontamento pp = Funcoes.apontamento("Digite o valor", new Apontamento(), true, false, false, "");

            if(pp.valor>0)
            {
                if(Conexoes.Utilz.Pergunta("Tem certeza que deseja somar " + pp.valor + " nos itens selecionados?"))
                {
                    foreach(var p in pcs)
                    {
                        p.valor = p.valor + pp.valor;
                    }
                }
            }
        }

        private void diminui_valores(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<GCM_Offline.Apontamento>().ToList();
            if (pcs.Count == 0)
            {

                pcs = Conexoes.Utilz.SelecionarObjetos(this.apontamentos, true, "Selecione",  this);
                if (pcs.Count == 0) { return; }
            }

            Apontamento pp = Funcoes.apontamento("Digite o valor", new Apontamento(), true, false, false, "");

            if (pp.valor > 0)
            {
                if (Conexoes.Utilz.Pergunta("Tem certeza que deseja diminuir " + pp.valor + " nos itens selecionados?\nSe o valor for maior que o valor existente, será zerado."))
                {
                    foreach (var p in pcs)
                    {
                        p.valor = (p.valor - pp.valor)>=0? (p.valor - pp.valor):0;
                    }
                }
            }
        }

        private void soma_dias(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<GCM_Offline.Apontamento>().ToList();
            if (pcs.Count == 0)
            {

                pcs = Conexoes.Utilz.SelecionarObjetos(this.apontamentos, true, "Selecione", this);
                if (pcs.Count == 0) { return; }
            }


            Apontamento pp = Funcoes.apontamento("Digite o valor", new Apontamento(), true, false, false, "");

            if (pp.valor > 0)
            {
                if (Conexoes.Utilz.Pergunta("Tem certeza que deseja adicionar " + pp.valor + " dias nos itens selecionados?"))
                {
                    foreach (var p in pcs)
                    {
                        p.data = new Data(p.data.Getdata().AddDays(pp.valor));
                    }
                }
            }
        }

        private void diminui_dias(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<GCM_Offline.Apontamento>().ToList();
            if (pcs.Count == 0)
            {

                pcs = Conexoes.Utilz.SelecionarObjetos(this.apontamentos, true, "Selecione", this);
                if (pcs.Count == 0) { return; }
            }

            Apontamento pp = Funcoes.apontamento("Digite o valor", new Apontamento(), true, false, false, "");

            if (pp.valor > 0)
            {
                if (Conexoes.Utilz.Pergunta("Tem certeza que deseja diminuir " + pp.valor + " dias nos itens selecionados?"))
                {
                    foreach (var p in pcs)
                    {
                        p.data = new Data(p.data.Getdata().AddDays(-pp.valor));
                    }
                }
            }
        }

        private void editar_equipe(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<GCM_Offline.Apontamento>().ToList();
            if (pcs.Count == 0)
            {

                pcs = Conexoes.Utilz.SelecionarObjetos(this.apontamentos, true, "Selecione", this);
                if (pcs.Count == 0) { return; }
            }

            var valor = Conexoes.Utilz.Prompt("Digite","",pcs[0].responsavel);

            if (valor!=null)
            {
                if (Conexoes.Utilz.Pergunta("Tem certeza que deseja editar a equipe dos itens selecionados?"))
                {
                    foreach (var p in pcs)
                    {
                        p.responsavel = valor;
                    }
                }
            }
        }

        private void edita_descricao(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<GCM_Offline.Apontamento>().ToList();
            if (pcs.Count == 0)
            {

                pcs = Conexoes.Utilz.SelecionarObjetos(this.apontamentos, true, "Selecione", this);
                if (pcs.Count == 0) { return; }
            }

            var valor = Conexoes.Utilz.Prompt("Digite", "", pcs[0].descricao);

            if (valor != null)
            {
                if (Conexoes.Utilz.Pergunta("Tem certeza que deseja editar a descrição dos itens selecionados?"))
                {
                    foreach (var p in pcs)
                    {
                        p.descricao = valor;
                    }
                }
            }
        }
    }
}
