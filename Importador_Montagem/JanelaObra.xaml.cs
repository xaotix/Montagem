using FirstFloor.ModernUI.Windows.Controls;
using GCM_Offline;
using GCM_Online;
using LiveCharts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Charting;

namespace Importador_Montagem
{
    /// <summary>
    /// Interaction logic for JanelaObra.xaml
    /// </summary>
    public partial class JanelaObra : ModernWindow
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
                                      LineSmoothness = 0,
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




            this.lbl_desvio.Content = "Desvio \n" + t0.data;
            this.lbl_desvio1.Content = "Desvio \n" + t1.data;
            this.lbl_desvio2.Content = "Desvio \n" + t2.data;
            this.lbl_desvio3.Content = "Desvio \n" + t3.data;
            var ss = this.lob.Subfases().Sum(x => x.GetPrevistoDistribuidoDias().Sum(y => y.previsto));

            this.gauge_desvio.Value = t0.desvio;
            this.gauge_desvio1.Value = t1.desvio;
            this.gauge_desvio2.Value = t2.desvio;
            this.gauge_desvio3.Value = t3.desvio;

            this.gauge_dias_atraso.Value = lob.dias_atraso();



            this.gauge_desvio.GaugeActiveFill = getcordesvio(t0.desvio);
            this.gauge_desvio1.GaugeActiveFill = getcordesvio(t1.desvio);
            this.gauge_desvio2.GaugeActiveFill = getcordesvio(t2.desvio);
            this.gauge_desvio3.GaugeActiveFill = getcordesvio(t3.desvio);

            this.gauge_dias_atraso.GaugeActiveFill = getcordesvio(-lob.dias_atraso());

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
                    List<Avanco> st = new List<Avanco>();
                    st.AddRange(lob.GetAvancos(7, s));
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
                foreach (var e in lob.GetEfetivosERecursos())
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
            if (valor < -30)
            {
                return Brushes.DarkRed;
            }
            else if (valor < -25)
            {
                return Brushes.Red;
            }
            else if (valor < -20)
            {
                return Brushes.Red;
            }
            else if (valor < -15)
            {
                return Brushes.Orange;
            }
            else if (valor < -10)
            {
                return Brushes.Yellow;
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













        public Contrato lob_online { get; set; } = new Contrato();
        public JanelaObra(Contrato contrato)
        {
            this.lob_online = contrato;
            InitializeComponent();
          
            Update();
        }
        public Linha_de_Balanco lob { get; set; } = new Linha_de_Balanco();
        private void Update()
        {
            this.Title = this.lob_online.ToString();
            this.lob = this.lob_online.Getlob();
            this.GerarGrafico();
        }


        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var arq = Conexoes.Utilz.Abrir_String("xlsx","Selecione o arquivo");
            if(File.Exists(arq))
            {
                if (Conexoes.Utilz.Pergunta("Tem certeza que deseja importar o arquivo " + arq + " na obra \n" + this.lob_online.ToString()))
                {
                    bool status = false;
                    var pp = GCM_Offline.Excel.ImportarApontamentos(arq, this.lob_online.Getlob(), out status);
                    if (status)
                    {

                        this.lob_online.ImportarLob(pp);
                        this.lob_online.GetSubEtapas(true);
                        MessageBox.Show("Dados importados!");

                        Update();
                    }
                }
               
            }
  
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            if(Conexoes.Utilz.Pergunta("Você tem certeza que deseja apagar?"))
            {
            dbase.ApagarLancamentos(this.lob_online);
            }
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            if (Conexoes.Utilz.Pergunta("Você tem certeza que deseja apagar?"))
            {
                dbase.ApagarEtapas(this.lob_online);
            }
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            bool status = false;
            var psp = Conexoes.Utilz.Prompt(this.lob_online, out status);
            if(status)
            {
                this.lob_online.Salvar();
                Update();
            }
        }
    }
}
