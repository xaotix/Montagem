using Conexoes;
using FirstFloor.ModernUI.Windows.Controls;
using GCM_Offline;
using GCM_Online;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using System;
using System.Collections.Generic;
using System.IO;
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
using Telerik.Windows.Data;

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
            Conexoes.Utilz.Propriedades(p, out st, "Preencha os campos",this);
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
                if(Conexoes.Utilz.Pergunta("Tem certeza que deseja excluir as obras selecionadas?"))
                {
                    foreach (var s in sel)
                    {
                        dbase.Apagar(s);
                    }
                    Update();
                }
        
            }
        }
        public List<Contrato> abertos { get; set; } = new List<Contrato>();
        private void lista_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Contrato p = lista.SelectedItem as Contrato;

            if (p.status == Status_Montagem.NAO_IMPORTADA)
            {
                MessageBox.Show("Pedido sem apontamento de montagem. Entrar em contato com engenheiro de obra.");
                return;
            }

            if(p!=null)
            {
                if (abertos.Find(x => x.id == p.id) == null)
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
            //this.Update();
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

        private void importar(object sender, RoutedEventArgs e)
        {

            var arq = Conexoes.Utilz.Abrir_String("xlsx", "Selecione o arquivo");
            Contrato lob_online = new Contrato();
            if (File.Exists(arq))
            {

                bool status = false;



                var pp = GCM_Offline.Excel.ImportarApontamentos(arq, lob_online.Getlob(), out status);
                if(pp.pedido=="")
                {
                    MessageBox.Show("Campo pedido está em branco. (aba relatório)\nOperação abortada");
                    return;
                }
                if(!status)
                {
                    MessageBox.Show("Operação abortada.");
                    return;
                }
                if (Conexoes.Utilz.Pergunta("Importar pedido " + pp.pedido + "?"))
                {
                    var igual = this.obras.Find(x => x.contrato.ToUpper() == pp.pedido.ToUpper());
                    lob_online.contrato = pp.pedido;
                    lob_online.area = pp.area_total;
                    lob_online.descricao = pp.descricao_excel;
                    lob_online.engenheiro = pp.engenheiro_excel;
                    lob_online.gerente = pp.gerente;
                    lob_online.status = pp.status;
                    lob_online.ultima_importacao = new GCM_Offline.Data(DateTime.Now);
                    if(igual!=null)
                    {
                        lob_online = igual;
                    }
                    else
                    {
                        lob_online.Salvar();
                    }
                    if (status)
                    {
          
                        lob_online.ImportarLob(pp);
                
                     
                    }

                    MessageBox.Show("Dados importados!");

                    Update();
                }

            }
        }

        private void relatorio_excel(object sender, RoutedEventArgs e)
        {
            var dt = new Data(Conexoes.Utilz.SelecionarData(DateTime.Now, DateTime.Now.AddYears(-1), DateTime.Now.AddYears(1)));
            var dest = Conexoes.Utilz.SalvarArquivo("xlsx");
            if (dest != "")
            {
                string msg = "";
                GCM_Online.Excel.SalvarResumo(dest,dt, out msg, true);
            }
        }

        private void nova_linha_de_balanco(object sender, RoutedEventArgs e)
        {
            var dest = Conexoes.Utilz.SalvarArquivo("xlsm");
            if (dest != "" && dest != null)
            {
                var ss = Conexoes.Utilz.Copiar(GCM_Offline.Vars.template_lob, dest);
                if (ss)
                {
                    Conexoes.Utilz.Abrir(dest);
                }
            }
        }

        private void importar_pasta(object sender, RoutedEventArgs e)
        {
            var pasta = Conexoes.Utilz.SelecionarPasta("");
            if(!Directory.Exists(pasta))
            {
                return;
            }
            DirectoryInfo directoryInfo = new DirectoryInfo(pasta);
            var arqs = directoryInfo.GetFiles("*.xlsx", SearchOption.AllDirectories).OrderByDescending(t => t.LastWriteTime).ToList();
     
            List<Report> reports = new List<Report>();
            List<Linha_de_Balanco> ls = new List<Linha_de_Balanco>();
            if(arqs.Count==0 )
            {
                MessageBox.Show("Nenhum arquivo .Xlsx encontrado na pasta " + pasta);
                return;
            }

            if(!Conexoes.Utilz.Pergunta("Tem certeza que deseja importar os " + arqs.Count + " arquivos encontrados na pasta " + pasta  + "?"))
            {
                return;
            }
            foreach(var arq in arqs)
            {

                bool status = false;


                Contrato lob_online = new Contrato();

                var pp = GCM_Offline.Excel.ImportarApontamentos(arq.FullName, lob_online.Getlob(), out status);

                var iguals = ls.Find(x => x.pedido == pp.pedido);


                if (pp.pedido.Length!=13)
                {
                   reports.Add(new Report(arq.FullName,"Campo pedido está em branco. (aba relatório)\narquivo ignorado", TipoReport.Crítico));
                    status = false;
                }
                else if(iguals!=null)
                {
                    reports.Add(new Report(arq.FullName, "Já foi importado outro arquivo para a mesma obra. " + iguals.ToString(), TipoReport.Crítico));
                    status = false;
                }


                ls.Add(pp);


                if (!status)
                {
                    reports.Add(new Report(arq.FullName, "\n importação abortada. " + pp.msgerro, TipoReport.Crítico));
                }
                if (status)
                {
                    var igual = this.obras.Find(x => x.contrato == pp.pedido);
                    lob_online.contrato = pp.pedido;
                    lob_online.area = pp.area_total;
                    lob_online.descricao = pp.descricao_excel;
                    lob_online.engenheiro = pp.engenheiro_excel;
                    lob_online.gerente = pp.gerente;
                    lob_online.status = pp.status;
                    lob_online.ultima_importacao = new GCM_Offline.Data(DateTime.Now);
                    if (igual != null)
                    {
                        lob_online.id = igual.id;
                    }
                    else
                    {
                        lob_online.Salvar();
                    }
                    if (status)
                    {
                        lob_online.ImportarLob(pp);
                        reports.Add(new Report(arq.FullName, "Arquivo importado, obra: " + pp.ToString()));
                    }

                }

            }

            if(arqs.Count>0)
            {
                MessageBox.Show("Dados importados!");
                Update();
                Conexoes.Utilz.ShowReports(reports);
            }

        }

        private void ModernWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Title = System.Windows.Forms.Application.ProductName + " V." + System.Windows.Forms.Application.ProductVersion;
        }

        private void mudar_status(object sender, RoutedEventArgs e)
        {
            var sel = lista.SelectedItems.Cast<Contrato>().ToList();
            if (sel.Count == 0) { return; }
            var sts = Enum.GetValues(typeof(GCM_Offline.Status_Montagem)).Cast<GCM_Offline.Status_Montagem>().ToList().Select(x=>x.ToString()).ToList();
            string sels = Conexoes.Utilz.SelecionarObjeto(sts, null, "Selecione");
            if(sels != null)
            {
                var st = Conexoes.Utilz.StringParaEnum<GCM_Offline.Status_Montagem>(sels);
                foreach(var s in sel)
                {
                    s.status = st;
                    s.Salvar();
                }
                Update();
            }
        }
    }
}
