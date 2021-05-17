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
using OfficeOpenXml.ConditionalFormatting;
using Microsoft.Office.Interop.Outlook;
using System.Drawing.Drawing2D;
using System.Windows.Threading;
using Application = System.Windows.Application;
using System.Threading;

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
            novografico.DisableAnimations = true;
  


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
                                      LineSmoothness = 0,
                                      Opacity = 0.5
                                  });
                        }
                        if (real)
                        {
                            SeriesCollection.Add(new LiveCharts.Wpf.LineSeries
                            {
                                Title = (realizado > 0 ? "(Tot.:" + realizado + ") " : "") + "Real. " + sub_titulo,
                                Values = new ChartValues<double>(avanco.FindAll(x => x.data.Getdata() <= x._data_max.Getdata()).Select(x => x.realizado)),
                                PointGeometry = LiveCharts.Wpf.DefaultGeometries.Circle,
                                Stroke = Brushes.Green,
                                Fill = verd,
                                LineSmoothness = 0,
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
                                LabelsPosition = BarLabelPosition.Top,
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
                novografico.Update();

            }
            catch (System.Exception)
            {

            }
            return novografico;
        }
        private void GerarGrafico()
        {
            try
            {
                AtualizaListas();

                var t0 = lob.GetTotal(this.lob._data_max);
                var t1 = lob.GetTotalSemanaAnterior(this.lob._data_max);
                var t2 = lob.GetTotalSemanaAnterior2(this.lob._data_max);
                var t3 = lob.GetTotalSemanaAnterior3(this.lob._data_max);

                this.lob.Ajustes();

                this.lbl_previsto.Content = "Previsto: " + Math.Round(t0.previsto, 2) + "%";
                this.lbl_realizado.Content = "Realizado: " + Math.Round(t0.realizado, 2) + "%";
                this.prg_previsto.Value =Math.Round(t0.previsto, 2);
                this.prg_realizado.Value = Math.Round(t0.realizado, 2);

                this.lbl_previsto1.Content = "Previsto: " + Math.Round(t1.previsto, 2) + "%";
                this.lbl_realizado1.Content = "Realizado: " + Math.Round(t1.realizado, 2) + "%";
                this.prg_previsto1.Value = Math.Round(t1.previsto, 2);
                this.prg_realizado1.Value = Math.Round(t1.realizado, 2);

                this.lbl_previsto2.Content = "Previsto: " + Math.Round(t2.previsto, 2) + "%";
                this.lbl_realizado2.Content = "Realizado: " + Math.Round(t2.realizado, 2) + "%";
                this.prg_previsto2.Value = Math.Round(t2.previsto, 2);
                this.prg_realizado2.Value = Math.Round(t2.realizado, 2);

                this.lbl_previsto3.Content = "Previsto: " + Math.Round(t3.previsto, 2) + "%";
                this.lbl_realizado3.Content = "Realizado: " + Math.Round(t3.realizado, 2) + "%";
                this.prg_previsto3.Value = Math.Round(t3.previsto, 2);
                this.prg_realizado3.Value = Math.Round(t3.realizado, 2);

                this.lbl_desvio.Content = "Desvio " + t0.data;
                this.lbl_desvio1.Content = "Desvio " + t1.data;
                this.lbl_desvio2.Content = "Desvio " + t2.data;
                this.lbl_desvio3.Content = "Desvio " + t3.data;
                this.lbl_dias_atraso.Content = "Dias em atraso:";
                var ss = this.lob.Subfases().Sum(x => x.GetPrevistoDistribuidoDias().Sum(y => y.previsto));

                this.gauge_desvio.Value = t0.desvio;
                this.gauge_desvio1.Value = t1.desvio;
                this.gauge_desvio2.Value = t2.desvio;
                this.gauge_desvio3.Value = t3.desvio;

                this.gauge_dias_atraso.Value = lob.dias_atraso(this.lob._data_max);



                //this.gauge_desvio.GaugeActiveFill = getcordesvio(t0.desvio);
                //this.gauge_desvio1.GaugeActiveFill = getcordesvio(t1.desvio);
                //this.gauge_desvio2.GaugeActiveFill = getcordesvio(t2.desvio);
                //this.gauge_desvio3.GaugeActiveFill = getcordesvio(t3.desvio);
                //this.gauge_dias_atraso.GaugeActiveFill = getcordesvio(-lob.dias_atraso(this.lob._data_max));

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
                    var resumo = lob.GetTotal(this.lob._data_max);
                    var graf = GetGrafico(new List<Avanco> { resumo }, true, true, false, "%", "", Tipo_Grafico.Colunas);
                    this.novo_resumo.Children.Add(graf);



                    var tipos = lob.Subfases().Select(x => x.descricao).Distinct().ToList();

                    painel_tarefas.Children.Clear();
                    foreach (var e in tipos)
                    {
                        List<Avanco> st = new List<Avanco>();
                        st.AddRange(lob.GetAvancos(7, e));
                        if (st.Count > 0)
                        {
                            var max = st.Select(x => x.previsto).ToList();
                            max.AddRange(st.Select(x => x.realizado));
                            max = max.OrderBy(x => x).ToList();
                            double maximo = max.Last();
                            foreach (var p in st)
                            {
                                p.previsto = Math.Round(p.previsto / maximo * 100, 2);
                                p.realizado = Math.Round(p.realizado / maximo * 100, 2);
                                p.avancos.Clear();
                                p.descricao = "";
                            }

                            var graff = GetGrafico(st, true, true, true, "%", e);
                            graff.Height = 200;
                            graff.Margin = new Thickness(5, 5, 5, 5);

                            Border pp = new Border();
                            StackPanel panel = new StackPanel();
                            pp.BorderThickness = new Thickness(1);
                            pp.BorderBrush = Brushes.LightGray;
                            pp.CornerRadius = new CornerRadius(5);
                            pp.Margin = new Thickness(5, 5, 5, 5);
                            pp.Child = panel;
                            System.Windows.Controls.Label label = new System.Windows.Controls.Label();
                            label.Margin = new Thickness(5, 5, 2, 2);
                            label.FontWeight = FontWeights.Bold;
                            label.Content = e.ToString();
                            label.FontSize = 10;
                            System.Windows.Controls.Separator sep = new Separator();
                            sep.BorderBrush = Brushes.LightGray;
                            sep.BorderThickness = new Thickness(1);

                            panel.Children.Add(label);
                            panel.Children.Add(sep);
                            panel.Children.Add(graff);

                            painel_tarefas.Children.Add(pp);
                        }

                    }





                }
                catch (System.Exception)
                {
                }

                try
                {
                    painel_recursos.Children.Clear();
                    foreach (var e in lob.GetRecursos())
                    {
                        var apon = e.GetAvancosAcumulados();
                        var total = apon.Sum(x => x.previsto);
                        if (apon.Count > 0)
                        {
                            var max = apon.Max(x => x.max);
                            if (max > 0)
                            {
                                try
                                {
                                    var graff = GetGrafico(apon, true, true, false, "Total", e.descricao, Tipo_Grafico.Colunas, 0, max, e.total_previsto, e.total_utilizado);
                                    graff.Height = 200;
                                    graff.Margin = new Thickness(5, 5, 5, 5);

                                    Border pp = new Border();
                                    StackPanel panel = new StackPanel();
                                    pp.BorderThickness = new Thickness(1);
                                    pp.BorderBrush = Brushes.LightGray;
                                    pp.CornerRadius = new CornerRadius(5);
                                    pp.Margin = new Thickness(5, 5, 5, 5);
                                    pp.Child = panel;
                                    System.Windows.Controls.Label label = new System.Windows.Controls.Label();
                                    label.Margin = new Thickness(5, 5, 2, 2);
                                    label.Content = e.ToString();
                                    label.FontWeight = FontWeights.Bold;
                                    label.FontSize = 10;
                                    System.Windows.Controls.Separator sep = new Separator();
                                    sep.BorderBrush = Brushes.LightGray;
                                    sep.BorderThickness = new Thickness(1);

                                    panel.Children.Add(label);
                                    panel.Children.Add(sep);
                                    panel.Children.Add(graff);

                                    painel_recursos.Children.Add(pp);
                                }
                                catch (System.Exception)
                                {

                                }
                            }



                        }

                    }

                    painel_efetivo.Children.Clear();
                    foreach (var e in lob.Getefetivos())
                    {
                        var apon = e.GetAvancosAcumulados();
                        var total = apon.Sum(x => x.previsto);
                        if (apon.Count > 0)
                        {
                            var max = apon.Max(x => x.max);
                            if (max > 0)
                            {
                                try
                                {
                                    var graff = GetGrafico(apon, true, true, false, "Total", e.equipe, Tipo_Grafico.Colunas, 0, max, e.total_previsto, e.total_utilizado);
                                    graff.Height = 200;
                                    graff.Margin = new Thickness(5, 5, 5, 5);

                                    Border pp = new Border();
                                    StackPanel panel = new StackPanel();
                                    pp.BorderThickness = new Thickness(1);
                                    pp.BorderBrush = Brushes.LightGray;
                                    pp.CornerRadius = new CornerRadius(5);
                                    pp.Margin = new Thickness(5, 5, 5, 5);
                                    pp.Child = panel;
                                    System.Windows.Controls.Label label = new System.Windows.Controls.Label();
                                    label.Margin = new Thickness(5, 5, 2, 2);
                                    label.Content = "Efetivo equipe " + e.equipe.ToUpper() != "INDEFINIDO" ? ("[Equipe: " + e.equipe + "]") : " de montagem";
                                    label.FontWeight = FontWeights.Bold;
                                    label.FontSize = 10;
                                    System.Windows.Controls.Separator sep = new Separator();
                                    sep.BorderBrush = Brushes.LightGray;
                                    sep.BorderThickness = new Thickness(1);

                                    panel.Children.Add(label);
                                    panel.Children.Add(sep);
                                    panel.Children.Add(graff);

                                    painel_efetivo.Children.Add(pp);
                                }
                                catch (System.Exception)
                                {

                                }
                            }



                        }

                    }
                }
                catch (System.Exception)
                {
                }
            }
            catch (System.Exception)
            {


            }
           


        }

        private void AtualizaListas()
        {
            this.restricoes.ItemsSource = null;
            this.restricoes.ItemsSource = this.lob.restricoes;

            this.observacoes.ItemsSource = null;
            this.observacoes.ItemsSource = this.lob.observacoes;

            this.planosdeacao.ItemsSource = null;
            this.planosdeacao.ItemsSource = this.lob.planosdeacao;


            this.lista_etapas.ItemsSource = null;
            this.lista_etapas.ItemsSource = this.lob.Subfases();

        }

        public enum Tipo_Grafico
        {
            Linhas,
            Colunas
        }

        //public Brush getcordesvio(double valor)
        //{
        //    if (valor < -30)
        //    {
        //        return Brushes.DarkRed;
        //    }
        //   else if (valor < -25)
        //    {
        //        return Brushes.Red;
        //    }
        //    else if (valor < -20)
        //    {
        //        return Brushes.Red;
        //    }
        //    else if (valor < -15)
        //    {
        //        return Brushes.Orange;
        //    }
        //    else if (valor < -10)
        //    {
        //        return Brushes.Yellow;
        //    }
        //    else if (valor < -5)
        //    {
        //        return Brushes.Yellow;
        //    }
        //    else
        //    {
        //        return Brushes.Green;
        //    }
        //}


        public Obra obra { get; set; } = new Obra();
        public Linha_de_Balanco lob { get; set; } = new Linha_de_Balanco();
        public JanelaObra2(Obra obra)
        {
            this.obra = obra;
            this.lob = this.obra.getLOB();

            InitializeComponent();
            this.DataContext = this.lob;
          

            var subs = this.lob.Subfases();


            this.data_padrao.SelectedDate = DateTime.Now;


            //GerarVisuais();
        }


        private void GerarVisuais()
        {

            if(this.data_padrao.SelectedDate!=null)
            {
                this.lob._data_max = new Data((DateTime)this.data_padrao.SelectedDate);
            }
            this.GerarGrafico();
            //this.lista.ItemsSource = null;
            //this.lista.ItemsSource = this.lob.recursos__previstos.FindAll(x=>x.total_previsto >0 | x.realizado>0);
            //this.lista_etapas.ItemsSource = null;
            //this.lista_etapas.ItemsSource = this.lob.Subfases();
            this.Title = this.obra.ToString() +  "- Montagem [v" + System.Windows.Forms.Application.ProductVersion + "]";
            //this.titulo.Content = this.obra.ToString();



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
            this.lob.Ajustes();

            if (salvar)
            {
            this.lob.SalvarTudo();
            }
          this.lob =  this.lob.Carregar();
            this.lob.Verificar();
            this.GerarVisuais();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Conexoes.Utilz.Propriedades(obra, true);
            if (Conexoes.Utilz.Pergunta("Salvar alterações?"))
            {
               this.obra.Salvar();

            }

            this.Title = obra.ToString();
        }

        //private void editar_pesos_avanco_fisico(object sender, RoutedEventArgs e)
        //{

        //}



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

            this.lob.CalcularEfetivosPrevistos();

            //if(Conexoes.Utilz.Pergunta("Deseja calcular automaticamente o efetivo baseado na diária preenchida nas etapas para cada etapa?"))
            //{


            //}

            if (Conexoes.Utilz.Pergunta("Deseja editar a data de início real? \nAo alterar esse valor, você pode definir uma data anterior ao início do cronograma e o programa criará uma planilha com mais colunas à esquerda da data de início."))
            {
                Editar_Inicio_Real();
            }
            Excel.ExportarApontamentos(this.lob, this.obra, true);
            
        }

        //private void apontamento_global(object sender, RoutedEventArgs e)
        //{
        //    var sel = this.lob.fases.SelectMany(x=>x.fases).ToList();

        //    if (sel.Count > 0)
        //    {
        //        ApontarFases mm = new ApontarFases(sel);
        //        mm.Show();
        //    }
        //}

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
                        this.lob.Getapontamentos(true).apontamentos.Clear();
                        lob.CalcularEfetivosPrevistos();
                        this.lob.fases.Clear();
                        this.lob.recursos__previstos.Clear();
                        this.lob.descricao_excel = lob.descricao_excel;
                        this.lob.emissao = lob.emissao;
                        this.lob.engenheiro_excel = lob.engenheiro_excel;
                        this.lob.fim_cronograma = lob.fim_cronograma;
                        this.lob.gerente = lob.gerente;
                        this.lob.inicio_cronograma = lob.inicio_cronograma;
                        this.lob.inicio_real = lob.inicio_real;
                        this.lob.motivo_desvio = lob.motivo_desvio;
                        this.lob.status = lob.status;
                        this.lob.versao_planilha = lob.versao_planilha;
                        if(this.obra.engenheiro == "")
                        {
                            this.obra.engenheiro = this.lob.engenheiro_excel;
                        }
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

                        if (ss.apontamentos_recursos)
                        {

                            foreach (var s in lob.recursos__previstos)
                            {
                                s.GetApontamentos();
                                this.lob.recursos__previstos.Add(s);
                                this.lob.Getapontamentos().apontamentos.AddRange(s.GetApontamentos());
                            }
                        }

                        if (ss.planosdeacao)
                        {
                            this.lob.planosdeacao.AddRange(lob.planosdeacao);
                            this.lob.planosdeacao = this.lob.planosdeacao.GroupBy(x => x.acao.ToUpper().Replace(" ", "").Replace("_", "")).Select(x => x.First()).ToList();
                        }
                        if (ss.observacoes)
                        {
                            this.lob.observacoes.AddRange(lob.observacoes);
                            this.lob.observacoes = this.lob.observacoes.GroupBy(x => x.descricao.ToUpper().Replace(" ", "").Replace("_", "")).Select(x => x.First()).ToList();
                        }
                        if (ss.restricoes)
                        {
                            this.lob.restricoes.AddRange(lob.restricoes);
                            this.lob.restricoes = this.lob.restricoes.GroupBy(x => x.descricao.ToUpper().Replace(" ", "").Replace("_", "")).Select(x => x.First()).ToList();
                        }

                        Update();

                        MessageBox.Show("Dados importados!");
                    }
                }
                else
                {
                    MessageBox.Show(lob.msgerro);
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

        //private void editar_recurso(object sender, RoutedEventArgs e)
        //{
        //    GCM_Offline.Recurso sel = ((FrameworkElement)sender).DataContext as GCM_Offline.Recurso;
        //    if(sel==null)
        //    {
        //        return;
        //    }
        //    Conexoes.Utilz.Propriedades(sel);
        //}

        //private void excluir_recurso(object sender, RoutedEventArgs e)
        //{
        //    GCM_Offline.Recurso sel = ((FrameworkElement)sender).DataContext as GCM_Offline.Recurso;
        //    if (sel == null)
        //    {
        //        return;
        //    }
        //   if(Conexoes.Utilz.Pergunta("Tem certeza que deseja excluir? Não é possível desfazer."))
        //    {
        //        this.lob.recursos__previstos.Remove(sel);
        //        Update(true);
        //    }
        //}

        //private void editar_apontamentos_recurso(object sender, RoutedEventArgs e)
        //{
        //    GCM_Offline.Recurso sel = ((FrameworkElement)sender).DataContext as GCM_Offline.Recurso;
        //    if (sel == null) { return; }

        //    if (sel.previsto.Count > 0)
        //    {
        //        sel.GetApontamentos();
        //        MenuLancamentos mm = new MenuLancamentos(sel.previsto);
        //        mm.Title = "Previsto - " + sel.ToString();
        //        mm.Owner = this;
        //        mm.ShowDialog();

        //        Update();
        //    }

        //}

        //private void editar_apontamentos_recurso_realizado(object sender, RoutedEventArgs e)
        //{
        //    GCM_Offline.Recurso sel = ((FrameworkElement)sender).DataContext as GCM_Offline.Recurso;
        //    if (sel == null) { return; }

        //    if (sel.GetApontamentos().Count > 0)
        //    {
        //        sel.GetApontamentos();
        //        MenuLancamentos mm = new MenuLancamentos(sel.GetApontamentos());
        //        mm.Title = "Apontamentos Realizado - " + sel.ToString();
        //        mm.Owner = this;
        //        mm.ShowDialog();

        //        Update();
        //    }
        //}

        //private void editar_etapa(object sender, RoutedEventArgs e)
        //{
        //    GCM_Offline.Fase sel = ((FrameworkElement)sender).DataContext as GCM_Offline.Fase;
        //    if (sel == null) { return; }

        //    if (sel.fases.Count > 0)
        //    {
        //        ApontarFases mm = new ApontarFases(sel);
        //        mm.Show();
        //    }
        //    else
        //    {
        //        //Conexoes.Utilz.Propriedades(sel, true, true);
        //        sel.GetApontamentos();
        //        MenuLancamentos mm = new MenuLancamentos(sel.GetApontamentos());
        //        mm.Title = "Apontamentos - " + sel.ToString();
        //        mm.Owner = this;
        //        mm.ShowDialog();

        //        Update();
        //    }
        //}

        //private void adicionar_apontamento(object sender, RoutedEventArgs e)
        //{
        //    Fase pp = ((FrameworkElement)sender).DataContext as Fase;
        //    if (pp == null)
        //    {
        //        return;
        //    }
        //    var s = Funcoes.apontamento(pp.ToString() + " - Adicionar apontamento");
        //    if (s != null)
        //    {

        //        pp.AddApontamento(s.data, s.valor, s.descricao, this.lob.Getapontamentos());
        //        Update();
        //    }
        //}

        //private void editar_etapa_dados(object sender, RoutedEventArgs e)
        //{
        //    Fase pp = ((FrameworkElement)sender).DataContext as Fase;

        //    Conexoes.Utilz.Propriedades(pp,true);
        //    this.Update(false);
        //}

        //private void ajusta_pesos_etapas(object sender, RoutedEventArgs e)
        //{
        //    this.lob.AjustaPesosEtapas(this.lob.Subfases());
  
        //    Update();
        //    MessageBox.Show("Dados ajustados!");
        //}

        //private void editar_data_inicio(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
        //    if (pcs.Count == 0)
        //    {
        //        pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(),true,  "Selecione", this);
        //        if (pcs.Count == 0) { return; }
        //    }
        //    var dt = pcs[0].inicio;
        //    if (!dt.valido)
        //    {
        //        dt = new Data(DateTime.Now);
        //    }
        //    var data = new Data(Conexoes.Utilz.SelecionarData(dt.Getdata(), dt.Getdata().AddYears(-2), dt.Getdata().AddYears(2)));
        //    //var peso = Conexoes.Utilz.Double(Conexoes.Utilz.Prompt("Digite o valor", pcs[0].peso_fase.ToString()));
        //    if (data.valido)
        //    {
        //        foreach (var p in pcs)
        //        {
        //            p.inicio = new Data(data.Getdata());
        //        }
        //        Update();
        //    }
        //}

        //private void editar_data_fim(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
        //    if (pcs.Count == 0)
        //    {
        //        pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), true, "Selecione", this);
        //        if (pcs.Count == 0) { return; }
        //    }
        //    var dt = pcs[0].fim;
        //    if (!dt.valido)
        //    {
        //        dt = new Data(DateTime.Now);
        //    }
        //    var data = new Data(Conexoes.Utilz.SelecionarData(dt.Getdata(), dt.Getdata().AddYears(-2), dt.Getdata().AddYears(2)));
        //    //var peso = Conexoes.Utilz.Double(Conexoes.Utilz.Prompt("Digite o valor", pcs[0].peso_fase.ToString()));
        //    if (data.valido)
        //    {
        //        foreach (var p in pcs)
        //        {
        //            p.fim = new Data(data.Getdata());
        //        }
        //        Update();
        //    }
        //}

        //private void editar_peso(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
        //    if (pcs.Count == 0)
        //    {
        //        pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), true, "Selecione",  this);
        //        if (pcs.Count == 0) { return; }
        //    }
        //    var peso = Conexoes.Utilz.Double(Conexoes.Utilz.Prompt("Digite o valor", pcs[0].peso_fase.ToString()));
        //    if (peso > 0 && peso <= 100)
        //    {
        //        foreach (var p in pcs)
        //        {
        //            p.peso_fase = peso;
        //        }
        //        Update();
        //    }
        //}

        //private void edita_efetivo(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
        //    if (pcs.Count == 0)
        //    {
        //        pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), true, "Selecione", this);
        //        if (pcs.Count == 0) { return; }
        //    }
        //    var peso = Conexoes.Utilz.Double(Conexoes.Utilz.Prompt("Digite o valor", pcs[0].efetivo.ToString()));
        //    if (peso >= 0)
        //    {
        //        foreach (var p in pcs)
        //        {
        //            p.efetivo = peso;
        //        }
        //        //Update();
        //    }
        //}

        //private void edita_montador(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
        //    if (pcs.Count == 0)
        //    {
        //        pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), true, "Selecione", this);
        //        if (pcs.Count == 0) { return; }
        //    }
        //    var peso = Conexoes.Utilz.Prompt("Digite o valor", pcs[0].equipe.ToString());
        //    if (peso != null)
        //    {
        //        foreach (var p in pcs)
        //        {
        //            p.equipe = peso;
        //        }
        //        //Update();
        //    }
        //}

        //private void edita_area(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
        //    if (pcs.Count == 0)
        //    {
        //        pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), true, "Selecione", this);
        //        if (pcs.Count == 0) { return; }
        //    }
        //    var peso = Conexoes.Utilz.Double(Conexoes.Utilz.Prompt("Digite o valor", pcs[0].efetivo.ToString()));
        //    if (peso >= 0)
        //    {
        //        foreach (var p in pcs)
        //        {
        //            p.area = peso;
        //        }
        //        //Update();
        //    }
        //}

        //private void edita_peso_fase(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
        //    if (pcs.Count == 0)
        //    {
        //        pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), true, "Selecione", this);
        //        if (pcs.Count == 0) { return; }
        //    }
        //    var peso = Conexoes.Utilz.Double(Conexoes.Utilz.Prompt("Digite o valor", pcs[0].peso_fase.ToString()));
        //    if (peso >= 0)
        //    {
        //        foreach (var p in pcs)
        //        {
        //            p.peso_fase = peso;
        //        }
        //        //Update();
        //    }
        //}

        //private void edita_etapa(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
        //    if (pcs.Count == 0)
        //    {
        //        pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), true, "Selecione", this);
        //        if (pcs.Count == 0) { return; }
         
        //    }
        //    var peso = Conexoes.Utilz.Prompt("Digite o valor", pcs[0].descricao.ToString());
        //    if (peso != null)
        //    {
        //        foreach (var p in pcs)
        //        {
        //            p.descricao = peso;

        //            p.GetApontamentos();
        //        }

        //        //Update();
        //    }
        //}

        //private void edita_cod(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();

        //    if (pcs.Count == 0)
        //    {
        //        pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), true, "Selecione", this);
        //        if (pcs.Count == 0) { return; }
        //    }
        //    var peso = Conexoes.Utilz.Prompt("Digite o valor", pcs[0].cod.ToString());
        //    if (peso != null)
        //    {
        //        foreach (var p in pcs)
        //        {
        //            p.cod = peso;
        //            p.GetApontamentos();
        //        }

        //        //Update();
        //    }
        //}

        //private void remover(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
        //    if(pcs.Count==0)
        //    {
        //        pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), true, "Selecione", this);
        //        if (pcs.Count == 0) { return; }
        //    }
        //    if (pcs.Count > 0)
        //    {
        //        if (Conexoes.Utilz.Pergunta("Tem certeza que deseja deletar os itens selecionados?"))
        //        {
        //            foreach (var s in pcs)
        //            {
        //                s.pai.fases.Remove(s);
        //            }
        //            Update();
        //        }
        //    }
        //}

        //private void adicionar_apontamento_fase(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista_etapas.SelectedItems.Cast<Fase>().ToList();
        //    if(pcs.Count==0)
        //    {
        //        pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), true, "Selecione", this);
        //        if (pcs.Count == 0) { return; }
        //    }
        //    if (pcs.Count > 0)
        //    {
        //        var s = Funcoes.apontamento("Adicionar apontamento");
        //        if (s != null)
        //        {

        //            foreach (var pp in pcs)
        //            {
        //                pp.SomarApontamento(s.data, s.valor, this.lob.Getapontamentos());
        //            }
        //            Update();
        //        }
        //    }
        //}

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

        //private void editar_equipe(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista.SelectedItems.Cast<Recurso>().ToList();
        //    if (pcs.Count == 0)
        //    {
        //    pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.recursos__previstos, true, "Selecione", this);
        //    if (pcs.Count == 0) { return; }
        //    }
        //    var peso = Conexoes.Utilz.Prompt("Digite o valor", pcs[0].equipe.ToString());
        //    if (peso != null)
        //    {
        //        foreach (var p in pcs)
        //        {
        //            p.equipe = peso;

        //        }

        //        //Update();
        //    }
        //}

        //private void apagar_recursos(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista.SelectedItems.Cast<Recurso>().ToList();
        //    if (pcs.Count == 0)
        //    {
        //    pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.recursos__previstos, true, "Selecione", this);
        //    if (pcs.Count == 0) { return; }
        //    }

        //    if (pcs.Count>0)
        //    {
        //        if(Conexoes.Utilz.Pergunta("Tem certeza que deseja excluir os recursos selecionados?"))
        //        {
        //            foreach(var s in pcs)
        //            {
        //                this.lob.recursos__previstos.Remove(s);
        //            }
        //            Update();
        //        }
        //    }
        //}

        //private void soma_dias(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista_etapas.SelectedItems.Cast<GCM_Offline.Fase>().ToList();
        //    if (pcs.Count == 0)
        //    {
        //        pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), true, "Selecione", this);
        //        if (pcs.Count == 0) { return; }
        //    }

        //    var  pp = Funcoes.apontamento("Digite o valor", new Apontamento(), true, false, false, "");

        //    if (pp.valor > 0)
        //    {
        //        if (Conexoes.Utilz.Pergunta("Tem certeza que deseja somar " + pp.valor + " dias nos itens selecionados?"))
        //        {
        //            foreach (var p in pcs)
        //            {
        //                p.inicio = new Data(p.inicio.Getdata().AddDays(+pp.valor));
        //            }
        //        }
        //        Update();
        //    }

        //}

        //private void diminui_dias(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista_etapas.SelectedItems.Cast<GCM_Offline.Fase>().ToList();
        //    if (pcs.Count == 0)
        //    {
        //        pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), true, "Selecione",  this);
        //        if (pcs.Count == 0) { return; }
        //    }

        //    var pp = Funcoes.apontamento("Digite o valor", new Apontamento(), true, false, false, "");

        //    if (pp.valor > 0)
        //    {
        //        if (Conexoes.Utilz.Pergunta("Tem certeza que deseja diminuir " + pp.valor + " dias nos itens selecionados?"))
        //        {
        //            foreach (var p in pcs)
        //            {
        //                p.inicio = new Data(p.inicio.Getdata().AddDays(-pp.valor));
        //            }
        //        }
        //        Update();
        //    }
        //}

        //private void soma_dias_fim(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista_etapas.SelectedItems.Cast<GCM_Offline.Fase>().ToList();
        //    if (pcs.Count == 0)
        //    {
        //        pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), true, "Selecione", this);
        //        if (pcs.Count == 0) { return; }
        //    }

        //    var pp = Funcoes.apontamento("Digite o valor", new Apontamento(), true, false, false, "");

        //    if (pp.valor > 0)
        //    {
        //        if (Conexoes.Utilz.Pergunta("Tem certeza que deseja somar " + pp.valor + " dias nos itens selecionados?"))
        //        {
        //            foreach (var p in pcs)
        //            {
        //                p.fim = new Data(p.fim.Getdata().AddDays(pp.valor));
        //            }
        //        }
        //        Update();
        //    }
        //}

        //private void diminui_dias_fim(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista_etapas.SelectedItems.Cast<GCM_Offline.Fase>().ToList();
        //    if (pcs.Count == 0)
        //    {
        //        pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.Subfases(), true, "Selecione",  this);
        //        if (pcs.Count == 0) { return; }
        //    }

        //    var pp = Funcoes.apontamento("Digite o valor", new Apontamento(), true, false, false, "");

        //    if (pp.valor > 0)
        //    {
        //        if (Conexoes.Utilz.Pergunta("Tem certeza que deseja diminuir " + pp.valor + " dias nos itens selecionados?"))
        //        {
        //            foreach (var p in pcs)
        //            {
        //                p.fim = new Data(p.fim.Getdata().AddDays(-pp.valor));
        //            }
        //        }
        //        Update();
        //    }
        //}

        //private void editar_apontamentos_previstos(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista_etapas.SelectedItems.Cast<GCM_Offline.Recurso>().ToList();
        //    if (pcs.Count == 0)
        //    {
        //        pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.recursos__previstos, true, "Selecione", this);
        //        if (pcs.Count == 0) { return; }
        //    }

        //    foreach (var sel in pcs)
        //    {
        //        sel.GetApontamentos();
               
        //    }
        //    MenuLancamentos mm = new MenuLancamentos(pcs.SelectMany(x=>x.previsto).ToList());
        //    mm.Title = "Editar Recursos Previstos";
        //    mm.Owner = this;
        //    mm.ShowDialog();

        //    Update();
        //}

        //private void editar_apontamentos_realizados(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista_etapas.SelectedItems.Cast<GCM_Offline.Recurso>().ToList();
        //    if (pcs.Count == 0)
        //    {
        //        pcs = Conexoes.Utilz.SelecionarObjetos(this.lob.recursos__previstos, true, "Selecione",  this);
        //        if (pcs.Count == 0) { return; }
        //    }

        //    foreach (var sel in pcs)
        //    {
        //        sel.GetApontamentos();

        //    }
        //    MenuLancamentos mm = new MenuLancamentos(pcs.SelectMany(x => x.GetApontamentos()).ToList());
        //    mm.Title = "Editar Recursos Realizados";
        //    mm.Owner = this;
        //    mm.ShowDialog();

        //    Update();
        //}

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
                pcs = Conexoes.Utilz.SelecionarObjetos(efets, true, "Selecione", this);
                if (pcs.Count == 0) { return; }
            }
            pcs = pcs.FindAll(x => x.descricao.ToUpper().Contains("EFETIVO")).ToList();

            var equipes = this.lob.Subfases().Select(x => x.equipe).Distinct().ToList();
            foreach (var eq in equipes)
            {
                var igual = this.lob.recursos__previstos.Find(x => x.equipe.ToUpper().Replace(" ", "") == eq.ToUpper().Replace(" ", ""));
                if (igual == null)
                {
                    var indef = this.lob.recursos__previstos.Find(x => x.equipe.ToUpper().Replace(" ", "") == "INDEFINIDO");
                    if (indef != null)
                    {
                        indef.equipe = eq;
                    }
                }
            }

            if (pcs.Count == 0) { return; }
            foreach (var pc in pcs)
            {
                pc.GetDiarias_Efetivo();
            }
            MessageBox.Show("Dados atualizados!");
            Update();
        }
        //private void ajustar_efetivo(object sender, RoutedEventArgs e)
        //{
        //    var pcs = lista.SelectedItems.Cast<Recurso>().ToList().FindAll(x=>x.descricao.ToUpper().Contains("EFETIVO")).ToList();
        //    AjustaEfetivo(pcs);
        
        //}

        //private void ver_avanco(object sender, RoutedEventArgs e)
        //{
        //    Funcoes.VerNodesAvancos(this.lob.GetAvancosAcumulados());
        //}

        //private void ver_avanco_subs(object sender, RoutedEventArgs e)
        //{
        //    Funcoes.VerNodesAvancos(this.lob.GetAvancosSubEtapas());

        //}

        private void Excluir_Restricao(object sender, RoutedEventArgs e)
        {
            var pp = ((FrameworkElement)sender).DataContext;

           
            if(pp is Restricao)
            {
                this.lob.restricoes.Remove(pp as Restricao);
            }
            else if (pp is Observacao)
            {
                this.lob.observacoes.Remove(pp as Observacao);
            }
            else if (pp is PlanoDeAcao)
            {
                this.lob.planosdeacao.Remove(pp as PlanoDeAcao);
            }
            AtualizaListas();
        }

        private void add_restricao(object sender, RoutedEventArgs e)
        {
            Restricao pp = new Restricao();
            pp.data = new Data(DateTime.Now);
            bool status = false;

            var peps = this.lob.Subfases().Select(x => x.pep).Distinct().ToList();
            if(peps.Count>0)
            {
                string sle = Conexoes.Utilz.SelecionarObjeto(peps, null, "Selecione");
                if(sle!=null)
                {
                    pp.pep = sle;
                }
            }


            Conexoes.Utilz.Propriedades(pp, out status, "Preencha", this);
            if(status)
            {
                this.lob.restricoes.Add(pp);
                this.lob.restricoes = this.lob.restricoes.OrderBy(x => x.data.Getdata()).ToList();
                this.AtualizaListas();
            }
        }

        private void Editar_Restricao(object sender, RoutedEventArgs e)
        {
            var pp = ((FrameworkElement)sender).DataContext;
                bool status = false;
            if(pp is Restricao)
            {

                Conexoes.Utilz.Propriedades(pp, out status, "Preencha", this);
                if (status)
                {
                    this.AtualizaListas();
                }
            }
            else if(pp is Observacao)
            {
                Conexoes.Utilz.Propriedades(pp, out status, "Preencha", this);
                if (status)
                {
                    this.AtualizaListas();
                }
            }
            else if (pp is PlanoDeAcao)
            {
                Conexoes.Utilz.Propriedades(pp, out status, "Preencha", this);
                if (status)
                {
                    this.AtualizaListas();
                }
            }
        }

        private void add_observacao(object sender, RoutedEventArgs e)
        {
            Observacao pp = new Observacao();
            pp.data = new Data(DateTime.Now);
            bool status = false;



            Conexoes.Utilz.Propriedades(pp, out status, "Preencha", this);
            if (status)
            {
                this.lob.observacoes.Add(pp);
                this.lob.observacoes = this.lob.observacoes.OrderBy(x => x.data.Getdata()).ToList();
                this.AtualizaListas();
            }
        }

        [STAThread]
        private void tira_foto(object sender, RoutedEventArgs e)
        {
            scrol.ScrollToTop();
            if (this.lob._data_max==null)
            {
                this.lob._data_max = new Data(DateTime.Now);
            }
            var pasta = Conexoes.Utilz.CriarPasta(this.obra.diretorio, "EXPORTAR");
            var imagem_status = pasta + this.obra.contrato + "." + this.lob._data_max.Getdata().ToShortDateString().Replace("/", "-") + "_status.png";
            var report = pasta + this.obra.contrato + "." + this.lob._data_max.Getdata().ToShortDateString().Replace("/", "-") + "_report.xlsx";

            Conexoes.Utilz.Apagar(imagem_status);
            Conexoes.Utilz.Apagar(report);
            GCM_Offline.Excel.ExportarApontamentos(this.lob, this.obra, false, report);

            tela.Background = Brushes.White;
            tela.UpdateLayout();
            Conexoes.Utilz.GerarFoto(tela, imagem_status);
            tela.Background = Brushes.Transparent;
            tela.UpdateLayout();

            Task.Factory.StartNew(() =>
            {
                retentar:
                int tentativas = 0;
                try
                {
                    Microsoft.Office.Interop.Outlook.Application outlookApp = new Microsoft.Office.Interop.Outlook.Application();
                    Microsoft.Office.Interop.Outlook._MailItem oMailItem = (Microsoft.Office.Interop.Outlook._MailItem)outlookApp.CreateItem(Microsoft.Office.Interop.Outlook.OlItemType.olMailItem);
                    Microsoft.Office.Interop.Outlook.Inspector oInspector = oMailItem.GetInspector;
                    Microsoft.Office.Interop.Outlook.Recipients oRecips = (Microsoft.Office.Interop.Outlook.Recipients)oMailItem.Recipients;

                    List<string> lstAllRecipients = new List<string>();

                    foreach (String recipient in lstAllRecipients)
                    {
                        Microsoft.Office.Interop.Outlook.Recipient oRecip = (Microsoft.Office.Interop.Outlook.Recipient)oRecips.Add(recipient);
                        oRecip.Resolve();
                    }

                    oMailItem.Subject = this.obra.ToString() + " Relatório de Montagem - Emissão: " + this.lob._data_max;
                    oMailItem.BodyFormat = Microsoft.Office.Interop.Outlook.OlBodyFormat.olFormatHTML;
                  


                    if (File.Exists(report))
                    {
                        var attachment = oMailItem.Attachments.Add(report, Microsoft.Office.Interop.Outlook.OlAttachmentType.olByValue, oMailItem.Body.Length, Type.Missing);
                    }


                    string novo ="Gerado no Painel de Obras - Módulo " + System.Windows.Forms.Application.ProductName + " v." + System.Windows.Forms.Application.ProductVersion + "<br>Resumo:<br><br>";
                
                    novo = AddImagem(imagem_status, oMailItem, novo);

                    oMailItem.HTMLBody = oMailItem.HTMLBody.Replace("</body>", novo);
                    //oMailItem.Save();
                    oMailItem.Display(true);

                }
                catch (System.Exception ex)
                {
                   
                    if(tentativas<5)
                    {
                        Conexoes.Utilz.Matar("OUTLOOK.EXE");
                        Thread.Sleep(2000);
                        tentativas++;
                        goto retentar;
                    }


                    MessageBox.Show("Deixe na aba 'Dados e Resumo' e tente novamente.\n\n\n\nTentativas:" + tentativas + "\n\n\n" + ex.Message + "\n\n" + ex.StackTrace);
                }
            });



        }

        private static string AddImagem(string imagem_status, _MailItem oMailItem, string novo)
        {
            //Attach image
            var attachment = oMailItem.Attachments.Add(imagem_status, Microsoft.Office.Interop.Outlook.OlAttachmentType.olByValue, oMailItem.Body.Length, Type.Missing);

            string imageCid = Conexoes.Utilz.getNome(imagem_status) + Conexoes.Utilz.RandomString(4);

            attachment.PropertyAccessor.SetProperty(
              "http://schemas.microsoft.com/mapi/proptag/0x3712001E"
             , imageCid
             );


            string banner = string.Format(@"<br/><img src=""cid:{0}""></a></body>", imageCid);
            novo = novo + banner;
            return novo;
        }

        private void ModernWindow_Loaded(object sender, RoutedEventArgs e)
        {
            scrol.ScrollToBottom();
            scrol.UpdateLayout();
            scrol.ScrollToTop();




            this.lob.Verificar();

        }

        //private void Adicionar_Restricao(object sender, RoutedEventArgs e)
        //{

        //}

        private void add_plano_acao(object sender, RoutedEventArgs e)
        {
            PlanoDeAcao pp = new PlanoDeAcao();
            pp.data = new Data(DateTime.Now);
            bool status = false;



            Conexoes.Utilz.Propriedades(pp, out status, "Preencha", this);
            if (status)
            {
                this.lob.planosdeacao.Add(pp);
                this.lob.planosdeacao = this.lob.planosdeacao.OrderBy(x => x.data.Getdata()).ToList();
                this.AtualizaListas();
            }
        }

        private void atualizar(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                this.GerarVisuais();
            }
            catch (System.Exception)
            {

   
            }
        }

        private void mostra_log(object sender, RoutedEventArgs e)
        {
            Conexoes.Utilz.Abrir(GCM_Offline.Vars.versionamento);


        }

        private void edita_data_inicio_real(object sender, RoutedEventArgs e)
        {
            Editar_Inicio_Real();
        }

        private void Editar_Inicio_Real()
        {
            this.lob.inicio_real = new Data(Conexoes.Utilz.SelecionarData(lob.inicio_real.Getdata(), lob.inicio.Getdata().AddDays(-120), lob.inicio.Getdata()));
        }
    }
}
