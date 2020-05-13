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
using System.IO;
using Conexoes;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Charting;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System.Configuration;
using Telerik.Windows.Controls.MaskedInput.Validation;
using LiveCharts;
using System.Drawing.Imaging;
using System.Runtime.Remoting;
using Conexoes.Macros;

namespace Montagem
{
    /// <summary>
    /// Interaction logic for JanelaObra2.xaml
    /// </summary>
    public partial class JanelaObra2 : ModernWindow
    {
        public static LiveCharts.Wpf.CartesianChart GetGrafico(List<Avanco> avanco, bool prev = true, bool real = true, bool desv = false, string LegendaY = "Porcentagem", string sub_titulo = "", Tipo_Grafico tipo = Tipo_Grafico.Linhas, double min = 0, double max = 100, double previsto = 0, double realizado = 0)
        {
            LiveCharts.Wpf.CartesianChart novografico = new LiveCharts.Wpf.CartesianChart();

            try
            {

                SeriesCollection SeriesCollection;
                string[] Labels;
                Func<double, string> YFormatter;
                SeriesCollection = new SeriesCollection();

                var verm = Brushes.LightSalmon.Clone();
                var azul = Brushes.LightBlue.Clone();
                var verd = Brushes.LightGreen.Clone();

                verm.Opacity = 0.5;
                azul.Opacity = 0.5;
                verd.Opacity = 0.5;
                switch (tipo)
                {
                    case Tipo_Grafico.Linhas:
                        if (desv)
                        {
                            SeriesCollection.Add(new LiveCharts.Wpf.ColumnSeries
                            {
                                Title = "Desv. " + sub_titulo,
                                Values = new ChartValues<double>(avanco.Select(x => x.desvio < 0 ? -x.desvio : 0)),
                                PointGeometry = LiveCharts.Wpf.DefaultGeometries.Square,
                                Stroke = Brushes.Red,
                                Fill = verm,
                                Opacity = 0.5,
                            });
                        }
                        if (prev)
                        {

                            SeriesCollection.Add(
                                  new LiveCharts.Wpf.LineSeries
                                  {

                                      Title = (previsto > 0 ? "(Tot.:" + previsto + ") " : "") + "Prev. " + sub_titulo,
                                      Values = new ChartValues<double>(avanco.Select(x => x.previsto)),
                                      PointGeometry = LiveCharts.Wpf.DefaultGeometries.Circle,
                                      Stroke = Brushes.Blue,
                                      Fill = azul,
                                      Opacity = 0.5
                                  });
                        }
                        if (real)
                        {
                            SeriesCollection.Add(new LiveCharts.Wpf.LineSeries
                            {
                                Title = (realizado > 0 ? "(Tot.:" + realizado + ") " : "") + "Real. " + sub_titulo,
                                Values = new ChartValues<double>(avanco.FindAll(x => x.data.Getdata() <= DateTime.Now).Select(x => x.realizado)),
                                PointGeometry = LiveCharts.Wpf.DefaultGeometries.Circle,
                                Stroke = Brushes.Green,
                                Fill = verd,
                                Opacity = 0.5
                            }); ;
                        }

                        break;
                    case Tipo_Grafico.Colunas:
                        if (prev)
                        {

                            SeriesCollection.Add(
                                  new LiveCharts.Wpf.ColumnSeries
                                  {

                                      Title = (previsto > 0 ? "(Tot.:" + previsto + ") " : "") + "Prev. " + sub_titulo,
                                      Values = new ChartValues<double>(avanco.Select(x => x.previsto)),
                                      PointGeometry = LiveCharts.Wpf.DefaultGeometries.Square,
                                      Stroke = Brushes.Blue,
                                      Fill = azul,
                                      Opacity = 0.5,
                                  });
                        }
                        if (real)
                        {
                            SeriesCollection.Add(new LiveCharts.Wpf.ColumnSeries
                            {
                                Title = (realizado > 0 ? "(Tot.:" + realizado + ") " : "") + "Real. " + sub_titulo,
                                Values = new ChartValues<double>(avanco.Select(x => x.realizado)),
                                PointGeometry = LiveCharts.Wpf.DefaultGeometries.Square,
                                Stroke = Brushes.Green,
                                Fill = verd,
                                Opacity = 0.5
                            });
                        }
                        if (desv)
                        {
                            SeriesCollection.Add(new LiveCharts.Wpf.ColumnSeries
                            {
                                Title = "Desv. " + sub_titulo,
                                Values = new ChartValues<double>(avanco.Select(x => x.desvio)),
                                PointGeometry = LiveCharts.Wpf.DefaultGeometries.Square,
                                Stroke = Brushes.Red,
                                Fill = verm,
                                Opacity = 0.5
                            });
                        }
                        break;
                }


                Labels = avanco.Select(x => x.data.ToString()).ToArray();
                YFormatter = value => value.ToString("C");
                novografico.AxisX.Add(new LiveCharts.Wpf.Axis
                {
                    Name = "xAxis",
                    Title = "Data",
                    FontSize = 10,
                    Foreground = System.Windows.Media.Brushes.Black,
                    MinValue = 0,
                    MaxValue = avanco.Count,
                    Labels = new String[avanco.Count],
                    LabelsRotation = 20
                });
                novografico.AxisY.Add(new LiveCharts.Wpf.Axis
                {
                    Name = "yAxis",
                    Title = LegendaY,
                    FontSize = 10,
                    Foreground = System.Windows.Media.Brushes.Black,
                    MinValue = min,
                    MaxValue = max,
                });

                novografico.Series = SeriesCollection;
                IList<string> list = new List<string>(avanco.Select(x => "Sem. " + x.data.semana.ToString().PadLeft(2, '0') + "/" + x.data.ano + "\n" + x.data.dia.ToString().PadLeft(2, '0') + "/" + x.data.mes));
                novografico.AxisX[0].Labels = list;
                novografico.LegendLocation = LegendLocation.Bottom;
                novografico.FontSize = 10;

            }
            catch (Exception)
            {

            }
            return novografico;
        }
        private void GerarGrafico()
        {


            var t0 = lob.GetTotal();
            var t1 = lob.GetTotalSemanaAnterior();
            var t2 = lob.GetTotalSemanaAnterior2();
            var t3 = lob.GetTotalSemanaAnterior3();

            this.lbl_desvio1.Content = "Desvio \n" + t1.data;
            this.lbl_desvio2.Content = "Desvio \n" + t2.data;
            this.lbl_desvio3.Content = "Desvio \n" + t3.data;
            var ss = this.lob.Subfases().Sum(x => x.GetPrevistoDistribuidoDias().Sum(y => y.valor));
            this.gauge_desvio.Value = t0.desvio;
            this.gauge_desvio1.Value = t1.desvio;
            this.gauge_desvio2.Value = t2.desvio;
            this.gauge_desvio3.Value = t3.desvio;

            this.gauge_desvio.GaugeActiveFill = getcordesvio(t0.desvio);
            this.gauge_desvio1.GaugeActiveFill = getcordesvio(t1.desvio);
            this.gauge_desvio2.GaugeActiveFill = getcordesvio(t2.desvio);
            this.gauge_desvio3.GaugeActiveFill = getcordesvio(t3.desvio);

            var avanco = lob.GetAvancos();

            if (avanco.Count == 0)
            {
                return;
            }








            try
            {
                var novografico = GetGrafico(avanco, true, true, true);
                novografico.Margin = new Thickness(5, 5, 5, 5);
                this.novo.Children.Clear();
                this.novo.Children.Add(novografico);


                this.novo_resumo.Children.Clear();
                var resumo = lob.GetTotal();
                var graf = GetGrafico(new List<Avanco> { resumo }, true, true, false, "%", "", Tipo_Grafico.Colunas);
                this.novo_resumo.Children.Add(graf);

                painel_recursos.Children.Clear();


                var tipos = lob.Subfases().Select(x => x.descricao).Distinct().ToList();

                painel_tarefas.Children.Clear();
                foreach (var s in tipos)
                {
                    var st = lob.GetAvancos(7, s);
                    if (st.Count > 0)
                    {
                        var max = st.Select(x => x.previsto).ToList();
                        max.AddRange(st.Select(x => x.realizado));
                        max = max.OrderBy(x => x).ToList();
                        foreach (var p in st)
                        {
                            p.previsto = Math.Round(p.previsto / max.Last() * 100, 2);
                            p.realizado = Math.Round(p.realizado / max.Last() * 100, 2);
                        }

                        var graff = GetGrafico(st, true, true, true, "%", s);
                        graff.Height = 350;
                        graff.Margin = new Thickness(5, 5, 5, 5);

                        Border pp = new Border();
                        pp.BorderThickness = new Thickness(1);
                        pp.BorderBrush = Brushes.LightGray;
                        pp.CornerRadius = new CornerRadius(5);
                        pp.Margin = new Thickness(5, 5, 5, 5);


                        pp.Child = graff;
                        painel_tarefas.Children.Add(pp);
                    }

                }





            }
            catch (Exception)
            {
            }

            try
            {
                painel_recursos.Children.Clear();
                foreach (var e in lob.GetEfetivos())
                {
                    var apon = e.GetAvancos();
                    var total = apon.Sum(x => x.previsto);
                    if (apon.Count > 0)
                    {
                        var max = apon.Max(x => x.max);
                        if (max > 0)
                        {
                            try
                            {
                                var graff = GetGrafico(apon, true, true, false, "Total", e.descricao, Tipo_Grafico.Colunas, 0, max, e.total_previsto, e.total_utilizado);
                                graff.Height = 350;
                                graff.Margin = new Thickness(5, 5, 5, 5);

                                Border pp = new Border();
                                pp.BorderThickness = new Thickness(1);
                                pp.BorderBrush = Brushes.LightGray;
                                pp.CornerRadius = new CornerRadius(5);
                                pp.Margin = new Thickness(5, 5, 5, 5);
                                pp.Child = graff;

                                painel_recursos.Children.Add(pp);
                            }
                            catch (Exception)
                            {

                            }
                        }



                    }

                }
            }
            catch (Exception)
            {
            }


        }
        public enum Tipo_Grafico
        {
            Linhas,
            Colunas
        }

        public Brush getcordesvio(double valor)
        {
            if (valor < -15)
            {
                return Brushes.Red;
            }
            else if (valor < -10)
            {
                return Brushes.Orange;
            }
            else if (valor < -5)
            {
                return Brushes.Yellow;
            }
            else
            {
                return Brushes.Green;
            }
        }




















        public Obra obra { get; set; } = new Obra();
        public Linha_de_Balanco lob { get; set; } = new Linha_de_Balanco();
        public JanelaObra2(Obra obra)
        {
            this.obra = obra;
            this.lob = this.obra.getLOB();

            InitializeComponent();
            this.DataContext = this.lob;
          

            var subs = this.lob.Subfases();

            

            GerarVisuais();
        }


        private void GerarVisuais()
        {
            this.lob.Verificar();
            this.GerarGrafico();
            this.lista.ItemsSource = null;
            this.lista.ItemsSource = this.lob.recursos__previstos;
            this.lista_etapas.ItemsSource = null;
            this.lista_etapas.ItemsSource = this.lob.Subfases();
            this.Title = this.obra.ToString();
            this.titulo.Content = this.obra.ToString();



        }

        private void importa_lob(object sender, RoutedEventArgs e)
        {
            var lob = Conexoes.Utilz.Abrir_String("xlsm", "Selecione o arquivo");
            if (lob != null)
            {
                if (File.Exists(lob))
                {
                    CriarBackup();
                    //Conexoes.Utilz.Copiar(lob, this.obra.diretorio + @"\LOB.XLSM");
                    bool tudook = true;
                    var ss = Excel.ImportarLOB(lob,this.obra.contrato, this.lob, out tudook);
                    if(tudook)
                    {
                        ss.Salvar(obra.diretorio);
                        this.lob = this.obra.getLOB();
                        Update();
                    }

                   
                }
            }
        }

        private void CriarBackup()
        {
            string backups = Conexoes.Utilz.CriarPasta(this.obra.diretorio, "BACKUP");
            string pasta = Conexoes.Utilz.CriarPasta(backups, "BACKUP_" + Conexoes.Utilz.GetPastas(backups).Count);
          
            Conexoes.Utilz.CopiarTudo(obra.diretorio, pasta, false);
        }

        private void abre_pasta(object sender, RoutedEventArgs e)
        {
            Conexoes.Utilz.Abrir(this.obra.diretorio);

        }

        private void salvar_tudo(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private void Update(bool salvar = true)
        {
            if(salvar)
            {
            this.lob.SalvarTudo();
            }
          this.lob =  this.lob.Carregar();
            this.GerarVisuais();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Conexoes.Utilz.Propriedades(obra, true);
            if (Conexoes.Utilz.Pergunta("Salvar alterações?"))
            {
                Update();

            }

            this.Title = obra.ToString();
        }

        private void editar_pesos_avanco_fisico(object sender, RoutedEventArgs e)
        {

        }



        private void exporta_avanco(object sender, RoutedEventArgs e)
        {
            
            
            this.lob.SalvarTudo();
           this.lob = this.lob.Carregar();
        retentar:
            if(this.lob.dias>GCM_Offline.Vars.max_dias)
            {
               if(!Conexoes.Utilz.Pergunta("Não será possível mostrar todas as datas das etapas, pois o cronograma passa de um ano. Deseja gerar mesmo assim?"))
                {
                    return;
                }
            }

            if (this.lob.fases.SelectMany(x=>x.fases).ToList().Count > GCM_Offline.Vars.max_etapas)
            {
                if (!Conexoes.Utilz.Pergunta("Não será possível mostrar todas as etapas, pois o cronograma passa do máximo possível [200]. Deseja gerar mesmo assim?"))
                {
                    return;
                }
            }
            var sel = this.lob.fases.SelectMany(x => x.fases).ToList().FindAll(x=>!x.inicio.valido | !x.fim.valido);

            if (sel.Count > 0)
            {
                if(Conexoes.Utilz.Pergunta("Há " + sel.Count + " etapas sem data definidas. Deseja ajusta-las?\nSe clicar em não, a linha de avanço ficará incorreta."))
                {
                    ApontarFases mm = new ApontarFases(sel);
                    mm.ShowDialog ();
                    this.lob.SalvarTudo();
                    goto retentar;
                    
                }
                else if(!Conexoes.Utilz.Pergunta("Deseja gerar a planilha mesmo assim?"))
                {
                    return;
                }
             
            }
            if (this.lob.fases.Count==0)
            {
                MessageBox.Show("Não há nenhuma etapa na linha de balanço atual. Importe uma linha de balanço.");
                return;
            }

            if(Conexoes.Utilz.Pergunta("Deseja calcular automaticamente o efetivo baseado na diária preenchida nas etapas para cada etapa?"))
            {
                AjustaEfetivo(this.lob.recursos__previstos);
            }

            Excel.ExportarApontamentos(this.lob, this.obra, true);
            
        }

        private void apontamento_global(object sender, RoutedEventArgs e)
        {
            var sel = this.lob.fases.SelectMany(x=>x.fases).ToList();

            if (sel.Count > 0)
            {
                ApontarFases mm = new ApontarFases(sel);
                mm.Show();
            }
        }

        private void importa_avanco(object sender, RoutedEventArgs e)
        {
            var arq  = Conexoes.Utilz.Abrir_String("xlsx", "Selecione o arquivo");
            if (arq != null)
            {
                bool status = false;
                var lob = Excel.ImportarApontamentos(arq,this.lob, out status);
                if(status)
                {
                    var ss = new OpcoesImportarApontamentos();
                    if(status)
                    {
                        lob.Ajustes();
                        CriarBackup();
                        this.lob.Getapontamentos().apontamentos.Clear();
                        this.lob.fases.Clear();
                        this.lob.recursos__previstos.Clear();
                        if(ss.atualiza_datas_cronograma)
                        {
                            if(lob.inicio_cronograma.valido)
                            {
                                this.lob.inicio_cronograma.SetData(lob.inicio_cronograma);
                            }
                            if(lob.fim_cronograma.valido)
                            {
                                this.lob.fim_cronograma.SetData(lob.fim_cronograma);
                            }
                        }
                        if (ss.apontamentos_etapas)
                        {
                        foreach(var s in lob.fases)
                            {
                               s.GetApontamentos();
                                this.lob.fases.Add(s);
                                this.lob.Getapontamentos().apontamentos.AddRange(s.fases.SelectMany(x=>x.GetApontamentos()));
                            }
                        }
                        if(ss.apontamentos_improdutividade)
                        {
                            foreach (var s in lob.recursos__previstos.FindAll(x=>x.tipo == Tipo_Recurso.Improdutividade))
                            {
                                s.GetApontamentos();
                                this.lob.recursos__previstos.Add(s);
                                this.lob.Getapontamentos().apontamentos.AddRange(s.GetApontamentos());
                            }
                        }
                        if (ss.apontamentos_recursos)
                        {

                            foreach (var s in lob.recursos__previstos.FindAll(x => x.tipo == Tipo_Recurso.Recurso))
                            {
                                s.GetApontamentos();
                                this.lob.recursos__previstos.Add(s);
                                this.lob.Getapontamentos().apontamentos.AddRange(s.GetApontamentos());
                            }
                        }
                        if (ss.apontamentos_supervisor)
                        {
                            foreach (var s in lob.recursos__previstos.FindAll(x => x.tipo == Tipo_Recurso.Supervisor))
                            {
                                s.GetApontamentos();
                                this.lob.recursos__previstos.Add(s);
                                this.lob.Getapontamentos().apontamentos.AddRange(s.GetApontamentos());
                            }
                        }
                        Update();
                        this.lob.Ajustes();
                        MessageBox.Show("Dados importados!");
                    }
                }

            }

        }

        private void abre_backups(object sender, RoutedEventArgs e)
        {
            var backs = Conexoes.Utilz.CriarPasta(this.obra.diretorio, "BACKUP");
            if(Directory.Exists(backs))
            {
            Conexoes.Utilz.Abrir(backs);

            }
        }

        private void ModernWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.lob.SalvarTudo();
        }

        private void limpa_apontamentos(object sender, RoutedEventArgs e)
        {
            if(Conexoes.Utilz.Pergunta("Tem certeza?"))
            {
                CriarBackup();
                Conexoes.Utilz.Apagar(this.lob.Getapontamentos().arquivo);
                Update(false);
            }
        }

        private void editar_recurso(object sender, RoutedEventArgs e)
        {
            GCM_Offline.Recurso sel = ((FrameworkElement)sender).DataContext as GCM_Offline.Recurso;
            if(sel==null)
            {
                return;
            }
            Conexoes.Utilz.Propriedades(sel);
        }

        private void excluir_recurso(object sender, RoutedEventArgs e)
        {
            GCM_Offline.Recurso sel = ((FrameworkElement)sender).DataContext as GCM_Offline.Recurso;
            if (sel == null)
            {
                return;
            }
           if(Conexoes.Utilz.Pergunta("Tem certeza que deseja excluir? Não é possível desfazer."))
            {
                this.lob.recursos__previstos.Remove(sel);
                Update(true);
            }
        }

        private void editar_apontamentos_recurso(object sender, RoutedEventArgs e)
        {
            GCM_Offline.Recurso sel = ((FrameworkElement)sender).DataContext as GCM_Offline.Recurso;
            if (sel == null) { return; }

            if (sel.previsto.Count > 0)
            {
                sel.GetApontamentos();
                MenuLancamentos mm = new MenuLancamentos(sel.previsto);
                mm.Title = "Previsto - " + sel.ToString();
                mm.Owner = this;
                mm.ShowDialog();

                Update();
            }

        }

        private void editar_apontamentos_recurso_realizado(object sender, RoutedEventArgs e)
        {
            GCM_Offline.Recurso sel = ((FrameworkElement)sender).DataContext as GCM_Offline.Recurso;
            if (sel == null) { return; }

            if (sel.GetApontamentos().Count > 0)
            {
                sel.GetApontamentos();
                MenuLancamentos mm = new MenuLancamentos(sel.GetApontamentos());
                mm.Title = "Apontamentos Realizado - " + sel.ToString();
                mm.Owner = this;
                mm.ShowDialog();

                Update();
            }
        }

        private void editar_etapa(object sender, RoutedEventArgs e)
        {
            GCM_Offline.Fase sel = ((FrameworkElement)sender).DataContext as GCM_Offline.Fase;
            if (sel == null) { return; }

            if (sel.fases.Count > 0)
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

        private void adicionar_apontamento(object sender, RoutedEventArgs e)
        {
            Fase pp = ((FrameworkElement)sender).DataContext as Fase;
            if (pp == null)
            {
                return;
            }
            var s = Funcoes.apontamento(pp.ToString() + " - Adicionar apontamento");
            if (s != null)
            {

                pp.AddApontamento(s.data, s.valor, s.descricao, this.lob.Getapontamentos());
                Update();
            }
        }

        private void editar_etapa_dados(object sender, RoutedEventArgs e)
        {
            Fase pp = ((FrameworkElement)sender).DataContext as Fase;
            Conexoes.Utilz.Propriedades(pp);
        }

        private void ajusta_pesos_etapas(object sender, RoutedEventArgs e)
        {
            this.lob.AjustaPesosEtapas(this.lob.Subfases());
            Update();
        }

        private void editar_data_inicio(object sender, RoutedEventArgs e)
        {
            var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
            if (pcs.Count == 0)
            {
                pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
            }
            var dt = pcs[0].inicio;
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
                    p.inicio = new Data(data.Getdata());
                }
                Update();
            }
        }

        private void editar_data_fim(object sender, RoutedEventArgs e)
        {
            var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
            if (pcs.Count == 0)
            {
                pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
            }
            var dt = pcs[0].fim;
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

        private void editar_peso(object sender, RoutedEventArgs e)
        {
            var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
            if (pcs.Count == 0)
            {
                pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
            }
            var peso = Conexoes.Utilz.Double(Conexoes.Utilz.Prompt("Digite o valor", pcs[0].peso_fase.ToString()));
            if (peso > 0 && peso <= 100)
            {
                foreach (var p in pcs)
                {
                    p.peso_fase = peso;
                }
                Update();
            }
        }

        private void edita_efetivo(object sender, RoutedEventArgs e)
        {
            var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
            if (pcs.Count == 0)
            {
                pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
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
            var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
            if (pcs.Count == 0)
            {
                pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
            }
            var peso = Conexoes.Utilz.Prompt("Digite o valor", pcs[0].equipe.ToString());
            if (peso != null)
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
            var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
            if (pcs.Count == 0)
            {
                pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
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

        private void edita_peso_fase(object sender, RoutedEventArgs e)
        {
            var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
            if (pcs.Count == 0)
            {
                pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
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

        private void edita_etapa(object sender, RoutedEventArgs e)
        {
            var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
            if (pcs.Count == 0)
            {
                pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
         
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
            var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();

            if (pcs.Count == 0)
            {
                pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
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
            var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
            if(pcs.Count==0)
            {
                pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
            }
            if (pcs.Count > 0)
            {
                if (Conexoes.Utilz.Pergunta("Tem certeza que deseja deletar os itens selecionados?"))
                {
                    foreach (var s in pcs)
                    {
                        s.pai.fases.Remove(s);
                    }
                    Update();
                }
            }
        }

        private void adicionar_apontamento_fase(object sender, RoutedEventArgs e)
        {
            var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
            if(pcs.Count==0)
            {
                pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
            }
            if (pcs.Count > 0)
            {
                var s = Funcoes.apontamento("Adicionar apontamento");
                if (s != null)
                {

                    foreach (var pp in pcs)
                    {
                        pp.SomarApontamento(s.data, s.valor, this.lob.Getapontamentos());
                    }
                    Update();
                }
            }
        }

        private void editar_cronograma_inicio(object sender, RoutedEventArgs e)
        {
            var dt = new Data(Conexoes.Utilz.SelecionarData(this.lob.inicio_cronograma.Getdata(), DateTime.Now.AddYears(-5), DateTime.Now.AddYears(5)));
            if(dt.valido)
            {
            this.lob.inicio_cronograma = dt;
            }
           
        }

        private void editar_cronograma_fim(object sender, RoutedEventArgs e)
        {
            var dt = new Data(Conexoes.Utilz.SelecionarData(this.lob.fim_cronograma.Getdata(), DateTime.Now.AddYears(-5), DateTime.Now.AddYears(5)));
            if (dt.valido)
            {
                this.lob.fim_cronograma = dt;
            }
        }

        private void editar_equipe(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<Recurso>().ToList();
            if (pcs.Count == 0)
            {
            pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.recursos__previstos, null, "Selecione", true, this);
            if (pcs.Count == 0) { return; }
            }
            var peso = Conexoes.Utilz.Prompt("Digite o valor", pcs[0].equipe.ToString());
            if (peso != null)
            {
                foreach (var p in pcs)
                {
                    p.equipe = peso;

                }

                //Update();
            }
        }

        private void apagar_recursos(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<Recurso>().ToList();
            if (pcs.Count == 0)
            {
            pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.recursos__previstos, null, "Selecione", true, this);
            if (pcs.Count == 0) { return; }
            }

            if (pcs.Count>0)
            {
                if(Conexoes.Utilz.Pergunta("Tem certeza que deseja excluir os recursos selecionados?"))
                {
                    foreach(var s in pcs)
                    {
                        this.lob.recursos__previstos.Remove(s);
                    }
                    Update();
                }
            }
        }

        private void soma_dias(object sender, RoutedEventArgs e)
        {
            var pcs = lista_etapas.SelectedItems.Cast<GCM_Offline.Fase>().ToList();
            if (pcs.Count == 0)
            {
                pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
            }

            var  pp = Funcoes.apontamento("Digite o valor", new Apontamento(), true, false, false, "");

            if (pp.valor > 0)
            {
                if (Conexoes.Utilz.Pergunta("Tem certeza que deseja somar " + pp.valor + " dias nos itens selecionados?"))
                {
                    foreach (var p in pcs)
                    {
                        p.inicio = new Data(p.inicio.Getdata().AddDays(+pp.valor));
                    }
                }
                Update();
            }

        }

        private void diminui_dias(object sender, RoutedEventArgs e)
        {
            var pcs = lista_etapas.SelectedItems.Cast<GCM_Offline.Fase>().ToList();
            if (pcs.Count == 0)
            {
                pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
            }

            var pp = Funcoes.apontamento("Digite o valor", new Apontamento(), true, false, false, "");

            if (pp.valor > 0)
            {
                if (Conexoes.Utilz.Pergunta("Tem certeza que deseja diminuir " + pp.valor + " dias nos itens selecionados?"))
                {
                    foreach (var p in pcs)
                    {
                        p.inicio = new Data(p.inicio.Getdata().AddDays(-pp.valor));
                    }
                }
                Update();
            }
        }

        private void soma_dias_fim(object sender, RoutedEventArgs e)
        {
            var pcs = lista_etapas.SelectedItems.Cast<GCM_Offline.Fase>().ToList();
            if (pcs.Count == 0)
            {
                pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
            }

            var pp = Funcoes.apontamento("Digite o valor", new Apontamento(), true, false, false, "");

            if (pp.valor > 0)
            {
                if (Conexoes.Utilz.Pergunta("Tem certeza que deseja somar " + pp.valor + " dias nos itens selecionados?"))
                {
                    foreach (var p in pcs)
                    {
                        p.fim = new Data(p.fim.Getdata().AddDays(pp.valor));
                    }
                }
                Update();
            }
        }

        private void diminui_dias_fim(object sender, RoutedEventArgs e)
        {
            var pcs = lista_etapas.SelectedItems.Cast<GCM_Offline.Fase>().ToList();
            if (pcs.Count == 0)
            {
                pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
            }

            var pp = Funcoes.apontamento("Digite o valor", new Apontamento(), true, false, false, "");

            if (pp.valor > 0)
            {
                if (Conexoes.Utilz.Pergunta("Tem certeza que deseja diminuir " + pp.valor + " dias nos itens selecionados?"))
                {
                    foreach (var p in pcs)
                    {
                        p.fim = new Data(p.fim.Getdata().AddDays(-pp.valor));
                    }
                }
                Update();
            }
        }

        private void editar_apontamentos_previstos(object sender, RoutedEventArgs e)
        {
            var pcs = lista_etapas.SelectedItems.Cast<GCM_Offline.Recurso>().ToList();
            if (pcs.Count == 0)
            {
                pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.recursos__previstos, null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
            }

            foreach (var sel in pcs)
            {
                sel.GetApontamentos();
               
            }
            MenuLancamentos mm = new MenuLancamentos(pcs.SelectMany(x=>x.previsto).ToList());
            mm.Title = "Editar Recursos Previstos";
            mm.Owner = this;
            mm.ShowDialog();

            Update();
        }

        private void editar_apontamentos_realizados(object sender, RoutedEventArgs e)
        {
            var pcs = lista_etapas.SelectedItems.Cast<GCM_Offline.Recurso>().ToList();
            if (pcs.Count == 0)
            {
                pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.recursos__previstos, null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
            }

            foreach (var sel in pcs)
            {
                sel.GetApontamentos();

            }
            MenuLancamentos mm = new MenuLancamentos(pcs.SelectMany(x => x.GetApontamentos()).ToList());
            mm.Title = "Editar Recursos Realizados";
            mm.Owner = this;
            mm.ShowDialog();

            Update();
        }

        private void envia_ftp_avanco(object sender, RoutedEventArgs e)
        {

        }

        private void editar_pep(object sender, RoutedEventArgs e)
        {

        }

        private void edita_inicio_cronograma(object sender, RoutedEventArgs e)
        {
            var data = new Data(Conexoes.Utilz.SelecionarData(this.lob.inicio_cronograma.Getdata(), DateTime.Now.AddYears(-4), DateTime.Now.AddYears(4)));
            if(data.valido)
            {
                this.lob.inicio_cronograma.SetData(data);
            }
        }

        private void edita_fim_cronograma(object sender, RoutedEventArgs e)
        {
            var data = new Data(Conexoes.Utilz.SelecionarData(this.lob.fim_cronograma.Getdata(), DateTime.Now.AddYears(-4), DateTime.Now.AddYears(4)));
            if (data.valido)
            {
                this.lob.fim_cronograma.SetData(data);
            }
        }
        public void AjustaEfetivo(List<Recurso> pcs)
        {
            if (pcs.Count == 0)
            {
                var efets = this.lob.recursos__previstos.FindAll(x => x.descricao.ToUpper().Contains("EFETIVO")).ToList();

                if (efets.Count == 0)
                {
                    MessageBox.Show("Nenhum efetivo encontrado na lista de recursos. no campo 'Recurso' deve conter a palavra 'Efetivo' para o sistema reconhecer.");
                    return;
                }
                pcs = Conexoes.Utilz.SelecionarObjetos(efets, null, "Selecione", true, this);
                if (pcs.Count == 0) { return; }
            }
            pcs = pcs.FindAll(x => x.descricao.ToUpper().Contains("EFETIVO")).ToList();
            if (pcs.Count == 0) { return; }
            foreach (var pc in pcs)
            {
                pc.GetDiarias_Efetivo();
            }
            MessageBox.Show("Dados atualizados!");
            Update();
        }
        private void ajustar_efetivo(object sender, RoutedEventArgs e)
        {
            var pcs = lista.SelectedItems.Cast<Recurso>().ToList().FindAll(x=>x.descricao.ToUpper().Contains("EFETIVO")).ToList();
            AjustaEfetivo(pcs);
        
        }
    }
}
