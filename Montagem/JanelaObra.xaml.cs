using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
#pragma warning disable CS0105 // A diretiva using para "System.Windows" apareceu anteriormente neste namespace
using System.Windows;
#pragma warning restore CS0105 // A diretiva using para "System.Windows" apareceu anteriormente neste namespace
using System.Windows.Media.Imaging;
using Telerik.Windows.Controls.GanttView;
using Telerik.Windows.Controls.Scheduling;
using Telerik.Windows.Controls;
using System.Drawing.Printing;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using GCM;
using Telerik.Windows.Controls.Timeline;

namespace Montagem
{
    /// <summary>
    /// Interaction logic for JanelaObra.xaml
    /// </summary>
    public partial class JanelaObra : ModernWindow
    {
        public Obra obra { get; set; } = new Obra();
        public Linha_de_Balanco lob { get; set; } = new Linha_de_Balanco();
        public Linha_de_Balanco lob_apontamentos { get; set; } = new Linha_de_Balanco();
        public JanelaObra(Obra obra)
        {
            this.obra = obra;

            InitializeComponent();
            this.Title = this.obra.ToString();
            updateCalendario();

        }


        public void AddData(List<Item> lista, string Titulo, DateTime inicio, DateTime fim, System.Windows.Media.Color cor, object objeto)
        {
            fim = fim.AddHours(23);
            lista.Add(new Item() { Titulo = Titulo, Date = inicio, Duration = fim - inicio, cor = new SolidColorBrush(cor) { Opacity = 1 }, DateFim = fim, objeto = objeto  });

        }

        private void updateCalendario()
        {
            if(obra!=null)
            {
                if(Directory.Exists(obra.diretorio))
                {

                    this.lob = new Linha_de_Balanco().Carregar(obra.diretorio);

                    lob.Ajustes();
                    calendario.ItemsSource = null;
                    this.calendario_datas.SelectionMode = System.Windows.Controls.SelectionMode.Extended;
                    calendario_datas.SelectedDates.Clear();

                    if (this.lob.fases.Count > 0)
                    {
                        calendario.PeriodStart = this.lob.inicio.Getdata().AddDays(-2);
                        calendario.PeriodEnd = this.lob.fim.Getdata().AddDays(2);
                        calendario.VisiblePeriodStart = calendario.PeriodStart;
                        calendario.VisiblePeriodEnd = calendario.PeriodEnd;
                        calendario.AutoSort = false;
                        calendario.ScrollMode = Telerik.Windows.Controls.TimeBar.ScrollMode.ScrollAndZoom;
                        //calendario.MinZoomRange = calendario.PeriodEnd - calendario.PeriodStart;
                        calendario.MinWidth = calendario.MinZoomRange.TotalMinutes / 30;

                        
                        List<Item> datas = new List<Item>();


                        var cor1 = Colors.LightBlue;
                        var cor2 = Colors.Green;

                        //AddData(datas, "Detalhamento", this.Obra.ei, this.Obra.ef, cor_detalhamento);
                        foreach (var etapa in lob.fases)
                        {
                            AddData(datas, etapa.ToString(), etapa.inicio.Getdata(), etapa.fim.Getdata(), cor1, etapa);
                            //foreach (var s in etapa.fases)
                            //{
                            //    AddData(datas, s.cod, s.inicio.Getdata(), s.fim.Getdata(), cor2,s);

                            //}
                        }



                        calendario.ItemsSource = datas;

                    
                        this.calendario.PeriodStart = this.lob.inicio.Getdata().AddDays(-7);

                        this.calendario.PeriodEnd = this.lob.fim.Getdata().AddDays(7);
                        this.calendario_datas.DisplayDate = this.lob.inicio.Getdata();
                        this.calendario_datas.DisplayDateStart = this.lob.inicio.Getdata().AddMonths(-3);
                        this.calendario_datas.DisplayDateEnd = this.lob.fim.Getdata().AddMonths(3);
                        this.calendario_datas.SelectionMode = System.Windows.Controls.SelectionMode.Extended;
                        this.calendario_datas.SelectedDates.Clear();
                        foreach (var s in Conexoes.Utilz.GetRangeDatas(this.lob.inicio.Getdata().AddDays(-7), this.lob.fim.Getdata().AddDays(7), false, false))
                        {
                            this.calendario_datas.SelectedDates.Add(s);

                        }
                        this.calendario_datas.IsTodayHighlighted = true;

                        this.calendario_datas.SelectableDateStart = this.lob.inicio.Getdata().AddDays(-7);
                        this.calendario_datas.SelectableDateEnd = this.lob.fim.Getdata().AddDays(+7);
                        
                        
                        this.calendario_datas.ViewsHeaderVisibility = Visibility.Visible;

                        this.calendario_datas.HeaderVisibility = Visibility.Collapsed;
                        this.calendario_datas.IsReadOnly = true;

                        this.calendario_datas.AreWeekNamesVisible = true;
                        this.calendario_datas.AreWeekNumbersVisible = true;
                        this.calendario_datas.Columns = this.lob.meses + 1;
                        this.grid_calendario.Width = this.calendario_datas.Width + 100;




                        this.calendario.AutoSort = false;
                    }
                }
                else
                {
                    MessageBox.Show("Pasta não encontrada: " + obra.diretorio);
                }
            }



        }

        private void importa_lob(object sender, RoutedEventArgs e)
        {
            var lob = Conexoes.Utilz.Abrir_String("xlsm", "Selecione o arquivo");
            if(lob!=null)
            {
                if(File.Exists(lob))
                {
                    var ss = Excel.CarregarLinhaDeBalanco(lob);
                    ss.Salvar(obra.diretorio);
                    MessageBox.Show("Linha de Balanço Importada!");
                    updateCalendario();
                }
            }
        }

        private void abre_pasta(object sender, RoutedEventArgs e)
        {
            Conexoes.Utilz.Abrir(this.obra.diretorio);
        }







        public TimeSpan pxlenght = new TimeSpan();





        private void ModernWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {


        }

        private void ModernWindow_Loaded(object sender, RoutedEventArgs e)
        {
           // Setdados();

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Conexoes.Utilz.Propriedades(obra, true);
            if (Conexoes.Utilz.Pergunta("Salvar alterações?"))
            {
                obra.Salvar(obra.diretorio);
            }

            this.Title = obra.ToString();
        }

        private void editar_pesos_avanco_fisico(object sender, RoutedEventArgs e)
        {
            Editar_Pesos_Avanco_Fisico mm = new Editar_Pesos_Avanco_Fisico(this.lob);
            mm.Closed += atualiza;
            mm.Show();
        }

        private void atualiza(object sender, EventArgs e)
        {
            if(Conexoes.Utilz.Pergunta("Salvar alterações?"))
            {
                lob.Salvar(obra.diretorio);
            }
            updateCalendario();
        }

        private void editar(object sender, RoutedEventArgs e)
        {
           TimelineDataItem pp = ((FrameworkElement)sender).DataContext as TimelineDataItem;
            if(pp==null)
            {
                return;
            }

            Item ps = pp.DataItem as Item;
            if(ps==null)
            {
                return;
            }

            GCM.Fase sel = ps.objeto as GCM.Fase;
            if (sel == null) { return; }

            ApontarFases mm = new ApontarFases(sel);
            mm.Show();
            
        }

        private void salvar_tudo(object sender, RoutedEventArgs e)
        {
            this.lob.SalvarTudo();
        }

        private void ModernWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.lob.SalvarTudo();
        }

        private void apontamento_global(object sender, RoutedEventArgs e)
        {
            var sel = Conexoes.Utilz.SelecionarObjetos<Fase>(this.lob.fases, null, "Selecione", true);

            if(sel.Count>0)
            {
                ApontarFases mm = new ApontarFases(sel.SelectMany(x=>x.fases).ToList());
                mm.Show();
            }
        }
    }
}
