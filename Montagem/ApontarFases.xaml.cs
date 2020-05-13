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
    /// Interação lógica para ApontarFases.xam
    /// </summary>
    public partial class ApontarFases : Window
    {
        public GCM_Offline.Fase fase { get; set; } = new GCM_Offline.Fase();
        public ApontarFases(GCM_Offline.Fase fase)
        {
            this.fase = fase;
            this.fase.GetApontamentos();
            InitializeComponent();
            this.Title = "[Apontamentos]" + this.fase.descricao;
            lista.ItemsSource = this.fase.fases;
        }
        public ApontarFases(List<GCM_Offline.Fase> fases)
        {
            
            this.fase = new Fase();
            this.fase.fases = fases;

            this.fase.GetApontamentos();
            InitializeComponent();
            this.Title = "[Apontamentos]" + this.fase.descricao;
            lista.ItemsSource = this.fase.fases;
        }

        private void editar(object sender, RoutedEventArgs e)
        {
            GCM_Offline.Fase sel = ((FrameworkElement)sender).DataContext as GCM_Offline.Fase;
            if (sel == null) { return; }

            if(sel.fases.Count>0)
            {
            ApontarFases mm = new ApontarFases(sel);
                mm.Show();
            }
            else
            {
                //Conexoes.Utilz.Propriedades(sel, true, true);
                sel.GetApontamentos();
                MenuLancamentos mm = new MenuLancamentos(sel.GetApontamentos());
                mm.Title = "Apontamentos - " + sel.ToString();
                mm.Owner = this;
                mm.ShowDialog();

                Update();
            }
           

        }

        private void adicionar(object sender, RoutedEventArgs e)
        {
            Fase pp = ((FrameworkElement)sender).DataContext as Fase;
            if(pp==null)
            {
                return;
            }
            var s = Funcoes.apontamento(pp.ToString() + " - Adicionar apontamento");
            if(s!=null)
            {

                pp.AddApontamento(s.data,s.valor,s.descricao, this.fase.lob.Getapontamentos());
                Update();
            }

        }

        private void adicionar_somando(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<Fase>().ToList();
            if(pcs.Count>0)
            {
                var s = Funcoes.apontamento("Adicionar apontamento");
                if (s!=null)
                {

                    foreach (var pp in pcs)
                    {
                        pp.SomarApontamento(s.data, s.valor, this.fase.lob.Getapontamentos());
                    }
                    Update();
                }
            }
        }

        private void Update()
        {
            this.lista.ItemsSource = null;
            this.fase.GetApontamentos();

            this.lista.ItemsSource = this.fase.fases;
        }

        private void editar_peso(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<Fase>().ToList();
            if(pcs.Count==0)
            {
                return;
            }
            var peso = Conexoes.Utilz.Double(Conexoes.Utilz.Prompt("Digite o valor",pcs[0].peso_fase.ToString()));
            if(peso>0 && peso <=100)
            {
                foreach(var p in pcs)
                {
                    p.peso_fase = peso;
                }
                Update();
            }
        }

        private void edita_efetivo(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<Fase>().ToList();
            if (pcs.Count == 0)
            {
                return;
            }
            var peso = Conexoes.Utilz.Double(Conexoes.Utilz.Prompt("Digite o valor", pcs[0].efetivo.ToString()));
            if (peso >= 0)
            {
                foreach (var p in pcs)
                {
                    p.efetivo = peso;
                }
                //Update();
            }
        }

        private void edita_montador(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<Fase>().ToList();
            if (pcs.Count == 0)
            {
                return;
            }
            var peso = Conexoes.Utilz.Prompt("Digite o valor", pcs[0].equipe.ToString());
            if (peso!=null)
            {
                foreach (var p in pcs)
                {
                    p.equipe = peso;
                }
                //Update();
            }
        }

        private void edita_area(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<Fase>().ToList();
            if (pcs.Count == 0)
            {
                return;
            }
            var peso = Conexoes.Utilz.Double(Conexoes.Utilz.Prompt("Digite o valor", pcs[0].efetivo.ToString()));
            if (peso >= 0)
            {
                foreach (var p in pcs)
                {
                    p.area = peso;
                }
                //Update();
            }
        }

        private void edita_etapa(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<Fase>().ToList();
            if (pcs.Count == 0)
            {
                return;
            }
            var peso = Conexoes.Utilz.Prompt("Digite o valor", pcs[0].descricao.ToString());
            if (peso != null)
            {
                foreach (var p in pcs)
                {
                    p.descricao = peso;

                    p.GetApontamentos();
                }

                //Update();
            }
        }

        private void edita_cod(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<Fase>().ToList();
            if (pcs.Count == 0)
            {
                return;
            }
            var peso = Conexoes.Utilz.Prompt("Digite o valor", pcs[0].cod.ToString());
            if (peso != null)
            {
                foreach (var p in pcs)
                {
                    p.cod = peso;
                    p.GetApontamentos();
                }

                //Update();
            }
        }

        private void remover(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<Fase>().ToList();
            if(pcs.Count>0)
            {
                if(Conexoes.Utilz.Pergunta("Tem certeza que deseja deletar os itens selecionados?"))
                {
                    foreach(var s in pcs)
                    {
                        s.pai.fases.Remove(s);
                    }
                    Update();
                }
            }
        }

        private void edita_peso_fase(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<Fase>().ToList();
            if (pcs.Count == 0)
            {
                return;
            }
            var peso = Conexoes.Utilz.Double(Conexoes.Utilz.Prompt("Digite o valor", pcs[0].peso_fase.ToString()));
            if (peso >= 0)
            {
                foreach (var p in pcs)
                {
                    p.peso_fase = peso;
                }
                //Update();
            }
        }

        private void ajusta_pesos(object sender, RoutedEventArgs e)
        {
            this.fase.lob.AjustaPesosEtapas(this.fase.fases);
            Update();
        }

        private void editar_data_inicio(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<Fase>().ToList();
            if (pcs.Count == 0)
            {
                return;
            }
            var dt = pcs[0].inicio;
            if(!dt.valido)
            {
                dt = new Data(DateTime.Now);
            }
           var data = new Data(Conexoes.Utilz.SelecionarData(dt.Getdata(), dt.Getdata().AddYears(-2), dt.Getdata().AddYears(2)));
            //var peso = Conexoes.Utilz.Double(Conexoes.Utilz.Prompt("Digite o valor", pcs[0].peso_fase.ToString()));
            if (data.valido)
            {
                foreach (var p in pcs)
                {
                    p.inicio = new Data(data.Getdata());
                }
                Update();
            }
        }

        private void editar_data_fim(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<Fase>().ToList();
            if (pcs.Count == 0)
            {
                return;
            }
            var dt = pcs[0].fim ;
            if (!dt.valido)
            {
                dt = new Data(DateTime.Now);
            }
            var data = new Data(Conexoes.Utilz.SelecionarData(dt.Getdata(), dt.Getdata().AddYears(-2), dt.Getdata().AddYears(2)));
            //var peso = Conexoes.Utilz.Double(Conexoes.Utilz.Prompt("Digite o valor", pcs[0].peso_fase.ToString()));
            if (data.valido)
            {
                foreach (var p in pcs)
                {
                    p.fim = new Data(data.Getdata());
                }
                Update();
            }
        }
    }
}
