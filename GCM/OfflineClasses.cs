using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Conexoes.Macros;
using Telerik.Windows.Controls.GridView.GridView;
using OfficeOpenXml;
using DB;
using System.Security.Permissions;
using System.Security.Cryptography;
using Ionic.BZip2;
using System.Windows.Input;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using Conexoes;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Collections.ObjectModel;
using Xceed.Wpf.Toolkit;
using System.ComponentModel.Design;

namespace GCM_Offline
{
    public enum Status_Montagem
    {
        EM_ANDAMENTO,
        CONCLUÍDA,
        ENTREGUE,
        DESMOBILIZADA,
        TRANCADA,
        NAO_IMPORTADA,
    }
    [Serializable]
    public class Linha_de_Balanco : INotifyPropertyChanged
    {
        public List<Restricao> restricoes { get; set; } = new List<Restricao>();
        public List<Observacao> observacoes { get; set; } = new List<Observacao>();
        public int dias_atraso()
        {
            var tot = GetTotal();

            if(this.fim_cronograma.Getdata()<DateTime.Now && tot.realizado<100)
            {
                return (DateTime.Now - this.fim_cronograma.Getdata()).Days;
            }
            return 0;
        }
        public List<Avanco> GetAvancosSubEtapas()
        {
            return this.Subfases().Select(x => new Avanco(x.inicio_real, x.GetPrevistoDistribuidoDias(), "De:" + x.inicio_real + " até " + x.fim_real + " " + x.ToString())).ToList();
        }
        public List<Avanco> GetAvancosAcumulados()
        {
            var d0 = this.inicio_real.Getdata();
            var d1 = this.fim_real.Getdata();
            List<Avanco> retorno = new List<Avanco>();
            while(d0<d1.AddDays(1))
            {
                retorno.Add(GetAvanco(new Data(d0)));
                d0 = d0.AddDays(1);
            }
            return retorno;
        }
        public Avanco GetTotal()
        {
            return GetAvanco(new Data(DateTime.Now));
        }
        public Avanco GetTotalSemanaAnterior()
        {
            var primeiro = Conexoes.Utilz.PrimeiroDiaDaSemana(DateTime.Now);
            return GetAvanco(new Data(primeiro.AddDays(-7)));
        }
        public Avanco GetTotalSemanaAnterior2()
        {
            var primeiro = Conexoes.Utilz.PrimeiroDiaDaSemana(DateTime.Now);
            return GetAvanco(new Data(primeiro.AddDays(-14)));
        }
        public Avanco GetTotalSemanaAnterior3()
        {
            var primeiro = Conexoes.Utilz.PrimeiroDiaDaSemana(DateTime.Now);
            return GetAvanco(new Data(primeiro.AddDays(-21)));
        }
        public Avanco GetTotalSemanaAnterior4()
        {
            var primeiro = Conexoes.Utilz.PrimeiroDiaDaSemana(DateTime.Now);
            return GetAvanco(new Data(primeiro.AddDays(-28)));
        }
        public Avanco GetAvanco(Data data = null, List<Fase> fases = null,string descricao = null)
        {
            if(data==null)
            {
                data = this.fim_real;
            }
            var previsto = this.GetPrevistoAcumulado(data, fases, descricao);
            var realizado = this.GetRealizadoAcumulado(data,fases,descricao);
            List<Avanco> pp = new List<Avanco>();
            pp.Add(previsto);
            pp.Add(realizado);
            return new Avanco(data, pp);
        }
        #region property
        [Browsable(false)]
        public event PropertyChangedEventHandler PropertyChanged;
        [Browsable(false)]
        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        [Browsable(false)]
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
        private Status_Montagem _Status { get; set; } = Status_Montagem.EM_ANDAMENTO;
        public Status_Montagem status
        {
            get
            {
                return _Status;
            }
            set
            {
                _Status = value;
                NotifyPropertyChanged("Status");
            }
        }
        public bool Verificar()
        {
            bool retorno = true;
            if (dias > GCM_Offline.Vars.max_dias)
            {
                Xceed.Wpf.Toolkit.MessageBox.Show("Não será possível mostrar todas as datas das etapas, pois o cronograma passa de um ano.");
                retorno = false;
            }
            var recs = this.GetTodosRecursos();
            var subs = Subfases();
            var pesos = subs.Sum(x => x.peso_fase);
            if (pesos > 1.05 && subs.Count > 0)
            {
                MessageBox.Show("Soma dos pesos das etapas [" + pesos + "] passa de 1.\n Vá até a aba lob e verifique a soma da coluna 'M' - PesoAtividade. A Soma deve dar 1.");
                retorno = false;
            }
            if (pesos < 0.99 && subs.Count > 0)
            {
                MessageBox.Show("Soma dos pesos das etapas [" + pesos + "] é inferior a 1.\n Vá até a aba lob e verifique a soma da coluna 'M' - PesoAtividade. A Soma deve dar 1.");
                retorno = false;
            }

            if (subs.Count > GCM_Offline.Vars.max_etapas)
            {
                MessageBox.Show("Não será possível mostrar todas as etapas, pois o cronograma passa do máximo possível [200].\nReduza a quantidade de etapas. Crie duas obras.");
                retorno = false;
            }

            var mais_que_100 = subs.FindAll(x => x.TotalApontado > 101);

            if (mais_que_100.Count > 0)
            {
                MessageBox.Show("Há [" + mais_que_100.Count + "] etapas com valores de apontamento acima de 100%." + (mais_que_100.Count < 10 ? ("\nEtapas: " + string.Join("\n", mais_que_100.Select(x => x.ToString() + " - " + x.TotalApontado + "%"))) : "") + "" +
                    "\nVá até a aba [Avanço] e procure as etapas onde a coluna 'I' esteja acima de 100% e corrija os apontamentos.");
                retorno = false;
            }

            var sem_peso = subs.FindAll(x => x.peso_fase == 0);
            if(sem_peso.Count>0)
            {
                MessageBox.Show("Há [" + sem_peso.Count + "] etapas com peso de etapa zerado." + (sem_peso.Count < 10 ? ("\nEtapas: " + string.Join("\n", sem_peso.Select(x => x.ToString()))) : "") + "" +
                  "\nVá até a aba [LOB] e procure as etapas onde a coluna 'M' [PesoAtividade] com o peso zerado.");
                retorno = false;
            }


            var sem_pep = subs.FindAll(x => x.pep == "");
            if (sem_pep.Count > 0)
            {
                MessageBox.Show("Há [" + sem_pep.Count + "] etapas sem o SAP PEP definido. \nVá até a aba LOB e ajuste a coluna 'O' - Código PEP SAP");
                retorno = false;
            }

            var sem_cod = subs.FindAll(x => x.cod == "");
            if (sem_cod.Count > 0)
            {
                MessageBox.Show("Há [" + sem_cod.Count + "] etapas sem o código preenchido.\nVá até a aba LOB e ajuste a coluna 'D' - Código");
                retorno = false;
            }

            var rec_sem_desc = recs.FindAll(x => x.descricao == "");
            if (rec_sem_desc.Count > 0)
            {
                MessageBox.Show("Há [" + rec_sem_desc.Count + "] recursos sem a descrição preenchida.\n Vá até a aba [Recursos] e ajuste as linhas com a coluna 'E' em branco.");
                retorno = false;
            }

            var inicio_problema = subs.FindAll(x => !x.inicio.valido);
            if (inicio_problema.Count > 0)
            {
                MessageBox.Show("Há [" + inicio_problema.Count + "] etapas com a data inicial inválida ou não definida.\nVá até a aba LOB e ajuste as datas.");
                retorno = false;
            }
            var fim_problema = subs.FindAll(x => !x.fim.valido);
            if (fim_problema.Count > 0)
            {
                MessageBox.Show("Há [" + fim_problema.Count + "] etapas com a data final inválida ou não definida.\nVá até a aba LOB e ajuste as datas.");
                retorno = false;
            }

            var datas_maiores = subs.FindAll(x => x.fim.valido && x.inicio.valido).FindAll(x => x.fim.Getdata() < x.inicio.Getdata());
            if (fim_problema.Count > 0)
            {
                MessageBox.Show("Há [" + fim_problema.Count + "] etapas com a data final inferior a data inicial.\nVá até a aba LOB e ajuste as datas.");
                retorno = false;
            }


            if (subs.Count == 0)
            {
                MessageBox.Show("Não há nenhuma etapa na linha de balanço atual. Importe uma linha de balanço.\nVá até a aba LOB e ajuste as etapas.");
                retorno = false;
            }

            return retorno;
        }
        public List<Recurso> GetEfetivosERecursos()
        {
            return this.recursos__previstos;
        }

        [Browsable(false)]
        public int dias
        {
            get
            {
                return (this.fim.Getdata() - this.inicio.Getdata()).Days;
            }
        }
        [Browsable(false)]
        public Data inicio
        {
            get
            {
                return inicio_real.Getdata() > inicio_cronograma.Getdata() ? inicio_real : inicio_cronograma;
            }
        }
        [Browsable(false)]
        public Data fim
        {
            get
            {
                return fim_real.Getdata() > fim_cronograma.Getdata() ? fim_real : fim_cronograma;
            }
        }
        [Browsable(false)]
        public Data inicio_cronograma
        {
            get
            {
                return _inicio_cronograma;
            }
            set
            {
                _inicio_cronograma = value;
                NotifyPropertyChanged("inicio_cronograma");
            }
        }
        private Data _inicio_cronograma { get; set; } = new Data();
        [Browsable(false)]
        public Data fim_cronograma
        {
            get
            {
                return _fim_cronograma;
            }
            set
            {
                _fim_cronograma = value;
                NotifyPropertyChanged("fim_cronograma");
            }
        }
        private Data _fim_cronograma { get; set; } = new Data();
        [Browsable(false)]
        public Data inicio_real
        {
            get
            {
                if (_inicio_real.valido)
                {
                    return _inicio_real;
                }
                else if (_inicio_cronograma.valido)
                {
                    return _inicio_cronograma;
                }
                return new Data(DateTime.Now);
            }
            set
            {
                _inicio_real = value;
                NotifyPropertyChanged("inicio_real");
            }
        }
        private Data _inicio_real { get; set; } = new Data();
        [Browsable(false)]
        public Data fim_real
        {
            get
            {
                return _fim_real;
            }
            set
            {
                _fim_real = value;
                NotifyPropertyChanged("fim_real");
            }
        }
        private Data _fim_real { get; set; } = new Data();
        [Browsable(false)]
        public Data emissao
        {
            get
            {
                return _emissao;
            }
            set
            {
                _emissao = value;
                NotifyPropertyChanged("emissao");
            }
        }
        private Data _emissao { get; set; } = new Data();
        [Category("Obra")]
        [DisplayName("Gerente de Montagem")]
        public string gerente
        {
            get
            {
                return _gerente;
            }
            set
            {
                _gerente = value;
                NotifyPropertyChanged("gerente");
            }
        }
        private string _gerente { get; set; } = "";


        [Category("Obra")]
        [DisplayName("Pedido")]
        public string pedido
        {
            get
            {
                return _pedido;
            }
            set
            {
                _pedido = value;
                NotifyPropertyChanged("pedido");
            }
        }
        private string _pedido { get; set; } = "00-000000.P00";
        public void AjustaPesosEtapas(List<Fase> fases = null)
        {
            if (fases == null)
            {
                fases = this.fases.SelectMany(x => x.fases).ToList();
            }
            var max = fases.Sum(x => x.peso_fase);
            foreach (var item in fases)
            {
                item.peso_fase = item.peso_fase / max;
            }
        }
        public double area_total
        {
            get
            {
                return this.fases.Sum(x => x.area);
            }
        }
        public void SalvarTudo()
        {
            
            this.Salvar(this.diretorio);
            this.Getapontamentos().Salvar(this.diretorio);
            this.GetDiario().Salvar(this.diretorio);
        }
        public void SetPesoAvanco(Fase fase)
        {
            if (fase.peso_fase > 0)
            {
                return;
            }
            var peso = this.fases_pesos_avanco_fisico.Find(x => x.cod == fase.cod);
            if (peso != null)
            {
                fase.peso_fase = peso.peso_fase * 100;
            }
        }

        [XmlIgnore]
        private Apontamentos _apontamentos { get; set; }
        public Apontamentos Getapontamentos()
        {
            if (_apontamentos == null)
            {
                if (Directory.Exists(diretorio))
                {
                    _apontamentos = new Apontamentos().Carregar(this.diretorio);

                    return _apontamentos;
                }
                _apontamentos = new Apontamentos();
            }
            else if (_apontamentos.diretorio.Length == 0)
            {
                if (Directory.Exists(diretorio))
                {
                    _apontamentos = new Apontamentos().Carregar(this.diretorio);
                    return _apontamentos;
                }
            }
            return _apontamentos;
        }

        [XmlIgnore]
        private Diario _diario { get; set; }
        public Diario GetDiario()
        {
            if (_diario == null)
            {
                if (Directory.Exists(diretorio))
                {
                    _diario = new Diario().Carregar(this.diretorio);
                    return _diario;
                }
                return new Diario();
            }
            return _diario;
        }
        public string arquivo
        {
            get
            {
                return (this.diretorio.EndsWith(@"\") ? this.diretorio : this.diretorio + @"\") + nomearq;
            }
        }
        public int meses
        {
            get
            {
                if (this.inicio.Getdata() < new DateTime(2001, 01, 01) | this.fim.Getdata() < new DateTime(2001, 01, 01))
                {
                    return 0;
                }

                return Conexoes.Utilz.GetMeses(this.inicio.Getdata().AddDays(-7), this.fim.Getdata().AddDays(7));
            }
        }
        [XmlIgnore]
        public string nomearq { get; set; } = "lob.cfg";
        public string diretorio { get; set; } = "";
        public Linha_de_Balanco Carregar(string diretorio = null)
        {
            this._apontamentos = null;
            if (diretorio == null)
            {
                diretorio = this.diretorio;
            }
            if (!diretorio.EndsWith(@"\"))
            {
                diretorio = diretorio + @"\";
            }
            var arq = diretorio + nomearq;
            if (File.Exists(arq))
            {
                DateTime min = new DateTime(2001, 01, 01);

                var carregar = string.Join("", Conexoes.Utilz.LerArquivo(arq, Encoding.GetEncoding(1252)));
                if (carregar.Length > 0)
                {
                    var s = Conexoes.Utilz.LerSerializado<Linha_de_Balanco>(carregar);
                    if (s != null)
                    {
                        s.Ajustes();

                        s.diretorio = diretorio;
                        return s;
                    }
                }
            }
            return new Linha_de_Balanco();
        }
        public void Salvar(string diretorio = null)
        {
            if (diretorio == null)
            {
                diretorio = this.diretorio;
            }
            if (diretorio.Length == 0)
            {
                return;
            }
            if (!Directory.Exists(diretorio))
            {

                return;
            }
            var s = Conexoes.Utilz.RetornarSerializado<Linha_de_Balanco>(this);
            if (!diretorio.EndsWith(@"\"))
            {
                diretorio = diretorio + @"\";
            }
            string arquivo = diretorio + nomearq;
            Conexoes.Utilz.GravarArquivo(arquivo, new List<string> { s }, Encoding.GetEncoding(1252));
        }
        public List<Data> GetDatasApontamentos()
        {
            if (this.Getapontamentos() == null)
            {
                return new List<Data>();
            }
            if (this.Getapontamentos().apontamentos.Count > 0)
            {
                return this.Getapontamentos().apontamentos.Select(x => x.data).ToList().OrderBy(x => x.Getdata()).ToList().FindAll(x => x.Getdata() > new DateTime(2001, 01, 01)).GroupBy(x => x.datastr).Select(x => x.First()).ToList();

            }
            return new List<Data>();
        }
        public Data fimRecursos()
        {
            var previsto = this.recursos__previstos.Select(x => x.fim).ToList();
            if (previsto.Count > 0)
            {
                var min = previsto.ToList().FindAll(x => x.valido).ToList().OrderBy(x => x.Getdata()).ToList();
                if (min.Count > 0)
                {
                    return min.Last();
                }
            }

            return this.fim;
        }
        public Data inicioRecursos()
        {
            var previsto = this.recursos__previstos.Select(x => x.inicio).ToList();
            if (previsto.Count > 0)
            {
                var min = previsto.ToList().FindAll(x => x.valido).ToList().OrderBy(x => x.Getdata()).ToList();
                if (min.Count > 0)
                {
                    return min.First();
                }
            }

            return this.inicio;
        }
        public void Ajustes()
        {
            this.restricoes = this.restricoes.OrderByDescending(x => x.data.Getdata()).ToList();
            this.observacoes = this.observacoes.OrderByDescending(x => x.data.Getdata()).ToList();
            //ajusta as datas das fases.
            if (this.fases.Count > 0)
            {
                //vincula as fases com o lob
                for (int a = 0; a < this.fases.Count; a++)
                {
                    this.fases[a].lob = this;
                    this.fases[a].fases = this.fases[a].fases.OrderBy(x => x.inicio.Getdata()).ToList();

                    for (int b = 0; b < this.fases[a].fases.Count; b++)
                    {
                        //atualiza o peso do acanço
                        this.SetPesoAvanco(this.fases[a].fases[b]);
                        this.fases[a].fases[b].lob = this;
                        this.fases[a].fases[b].pai = this.fases[a];
                        this.fases[a].fases[b].GetApontamentos();
                        for (int c = 0; c < this.fases[a].fases[b].fases.Count; c++)
                        {
                            this.fases[a].fases[b].fases[c].lob = this;
                            this.fases[a].fases[b].fases[c].pai = this.fases[a].fases[b];
                            for (int d = 0; d < this.fases[a].fases[b].fases[c].fases.Count; c++)
                            {
                                this.fases[a].fases[b].fases[c].fases[d].lob = this;
                                this.fases[a].fases[b].fases[c].fases[d].pai = this.fases[a].fases[b].fases[c];
                                for (int e = 0; e < this.fases[a].fases[b].fases[c].fases[d].fases.Count; c++)
                                {
                                    this.fases[a].fases[b].fases[c].fases[d].fases[e].lob = this;
                                    this.fases[a].fases[b].fases[c].fases[d].fases[e].pai = this.fases[a].fases[b].fases[c].fases[d];
                                }
                            }
                        }
                    }
                }
                this.fases = this.fases.OrderBy(x => x.inicio.Getdata()).ToList(); 

                //vincula os pesos com o lob
                for (int a = 0; a < this.fases_pesos_avanco_fisico.Count; a++)
                {
                    this.fases_pesos_avanco_fisico[a].lob = this;
                    for (int b = 0; b < this.fases_pesos_avanco_fisico[a].fases.Count; b++)
                    {
                        this.fases_pesos_avanco_fisico[a].fases[b].lob = this;
                        this.fases_pesos_avanco_fisico[a].fases[b].pai = this.fases[a];

                        for (int c = 0; c < this.fases_pesos_avanco_fisico[a].fases[b].fases.Count; c++)
                        {
                            this.fases_pesos_avanco_fisico[a].fases[b].fases[c].pai = this.fases[a].fases[b];
                            this.fases_pesos_avanco_fisico[a].fases[b].fases[c].lob = this;
                            for (int d = 0; d < this.fases_pesos_avanco_fisico[a].fases[b].fases[c].fases.Count; c++)
                            {
                                this.fases_pesos_avanco_fisico[a].fases[b].fases[c].fases[d].lob = this;
                                this.fases_pesos_avanco_fisico[a].fases[b].fases[c].fases[d].pai = this.fases[a].fases[b].fases[c];

                                for (int e = 0; e < this.fases_pesos_avanco_fisico[a].fases[b].fases[c].fases[d].fases.Count; c++)
                                {

                                    this.fases_pesos_avanco_fisico[a].fases[b].fases[c].fases[d].fases[e].lob = this;
                                    this.fases_pesos_avanco_fisico[a].fases[b].fases[c].fases[d].fases[e].pai = this.fases[a].fases[b].fases[c].fases[d];
                                }
                            }
                        }
                    }
                }

                //vincula os recursos com o lob
                for (int i = 0; i < recursos_custo.Count; i++)
                {
                    recursos_custo[i].lob = this;
                }
                for (int i = 0; i < recursos__previstos.Count; i++)
                {
                    recursos__previstos[i].lob = this;
                    recursos__previstos[i].GetApontamentos();
                    var custo = this.recursos_custo.Find(x => x.chave == recursos__previstos[i].chave);
                    if (custo != null)
                    {
                        recursos__previstos[i].diaria_util = custo.diaria_util;
                        recursos__previstos[i].custo_mensal = custo.custo_mensal;

                    }
                }
                foreach (var fase in this.fases)
                {
                    fase.SetInicios();
                }
                DateTime min = new DateTime(2001, 01, 01);
                var ini = this.fases.FindAll(x => x.inicio.Getdata() > new DateTime(2001, 01, 01)).Select(x => x.inicio).ToList();
                var ff = this.fases.FindAll(x => x.fim.Getdata() > new DateTime(2001, 01, 01)).Select(x => x.fim).ToList();

                var datas_lancamentos = this.GetDatasApontamentos();
                if (datas_lancamentos.Count > 0)
                {

                    ini.Add(datas_lancamentos.First());
                    ff.Add(datas_lancamentos.Last());
                }
                ini.Add(inicioRecursos());
                ff.Add(fimRecursos());

                if (ini.Count > 0)
                {
                    this.inicio_cronograma = new Data(ini.Min(x => x.Getdata()));

                    for (int i = 0; i < this.fases.Count; i++)
                    {
                        if (this.fases[i].inicio.Getdata() < min)
                        {
                            this.fases[i].inicio = this.inicio;
                        }
                        for (int b = 0; b < this.fases[i].fases.Count; b++)
                        {
                            if (this.fases[i].fases[b].inicio.Getdata() < min)
                            {
                                this.fases[i].fases[b].inicio = this.fases[i].inicio;
                            }
                        }
                    }
                }
                if (ff.Count > 0)
                {
                    this.fim_cronograma = new Data(ff.Max(x => x.Getdata()));
                    for (int i = 0; i < this.fases.Count; i++)
                    {
                        if (this.fases[i].fim.Getdata() < min)
                        {
                            this.fases[i].fim = this.fim;
                        }
                        for (int b = 0; b < this.fases[i].fases.Count; b++)
                        {
                            if (this.fases[i].fases[b].fim.Getdata() < min)
                            {
                                this.fases[i].fases[b].fim = this.fases[i].fim;
                            }
                        }
                    }
                }


            }



            //adiciona recursos que nao estão com dados de custos
            var s = recursos__previstos.FindAll(x => recursos_custo.Find(y => y.chave == x.chave) == null);
            foreach (var pps in s)
            {
                Recurso r = new Recurso();
                r.descricao = pps.descricao;
                recursos_custo.Add(r);
            }




            if (this.fases.Count > 0)
            {
                this.inicio_real = new Data(this.fases.Min(x => x.inicio_real.Getdata()));
                this.fim_real = new Data(this.fases.Max(x => x.fim_real.Getdata()));
                var subs = this.Subfases();
                if (subs.Count > 0)
                {
                    this.inicio_cronograma = new Data(subs.Min(x => x.inicio.Getdata()));
                    this.fim_cronograma = new Data(subs.Max(x => x.fim.Getdata()));
                }
            }




        }

        public List<Fase> GetEfetivoPrevisto(Recurso p)
        {
            if(p.equipe!="" && p.descricao.ToUpper().Contains("EFETIVO"))
            {
                return this.Subfases().FindAll(x => x.equipe.ToUpper().Replace(" ", "") == p.equipe.ToUpper().Replace(" ", ""));
            }
            return new List<Fase>();
        }

        public List<Fase> fases_pesos_avanco_fisico { get; set; } = new List<Fase>();
        public List<Recurso> recursos_custo { get; set; } = new List<Recurso>();
        public string msgerro { get; set; } = "";
        public string arquivoexcel { get; set; } = "";
        public List<Fase> Subfases()
        {
            return this.fases.SelectMany(x => x.fases).ToList().OrderBy(x=>x.inicio_real.Getdata()).ToList();
        }
        public List<Recurso> GetTodosRecursos()
        {
            List<Recurso> retorno = new List<Recurso>();
            retorno.AddRange(this.recursos__previstos);
            retorno.AddRange(this.improdutividade);
            retorno.AddRange(this.supervisor);

            return retorno;
        }
        public List<Recurso> GetRecursos()
        {
            List<Recurso> retorno = new List<Recurso>();
            retorno.AddRange(this.recursos__previstos.FindAll(x=>!x.descricao.ToUpper().Contains("EFETIVO")));


            return retorno;
        }

        public List<Fase> fases { get; set; } = new List<Fase>();
        public List<Recurso> GetRecursosPadrao()
        {
            List<Recurso> retorno = new List<Recurso>();
            retorno.Add(new Recurso("Efetivo Montador"));
            retorno.Add(new Recurso("Engenheiro de obras"));
            retorno.Add(new Recurso("Técnico em Seg. Trabalho"));
            retorno.Add(new Recurso("Técnico em edificações"));
            retorno.Add(new Recurso("Munck"));
            retorno.Add(new Recurso("Guindaste MD30"));
            retorno.Add(new Recurso("Guindaste 60 Ton"));
            retorno.Add(new Recurso("Guindaste 90 Ton"));
            retorno.Add(new Recurso("Guindaste 120 Ton"));
            retorno.Add(new Recurso("Guindaste 240 Ton"));
            retorno.Add(new Recurso("Plataforma Z45 - 14,5m"));
            retorno.Add(new Recurso("Plataforma Z60 - 20 m"));
            retorno.Add(new Recurso("Plataforma  Z80 - 25 m"));
            retorno.Add(new Recurso("Plataforma Z135 - 42 m"));
            retorno.Add(new Recurso("Plataforma 150 HAX - 47 m"));

            return retorno;
        }
        public List<Recurso> recursos__previstos { get; set; } = new List<Recurso>();
        public List<Recurso> supervisor { get; set; } = new List<Recurso>();
        public List<Recurso> improdutividade { get; set; } = new List<Recurso>();

        public List<Recurso> Getefetivos()
        {
            return this.recursos__previstos.FindAll(x => x.descricao.ToUpper().Contains("EFETIVO"));

        }
       
        public List<Avanco> GetAvancos(int dias = 7, string descricao = null, Data dmax = null)
        {
            List<Avanco> retorno = new List<Avanco>();
            var dt = primeiro_dia().Getdata();
            if (dmax == null)
            {
                dmax = new Data(this.fim_real.Getdata());
               
            }
           
            var fases = this.Subfases();
            if (descricao != null)
            {
                fases = fases.FindAll(x => x.descricao == descricao);
            }
            while (dt <= dmax.Getdata())
            {
                Avanco apon = GetAvanco(new Data(dt), fases);
                retorno.Add(apon);
                dt = dt.AddDays(dias);
            }
            if (retorno.Count > 0)
            {
                if (retorno.Last().data.Getdata() < dmax.Getdata())
                {
                    Avanco apon = GetAvanco(dmax, fases);
                    retorno.Add(apon);
                }
            }
            //adiciona um apontamento para ver a situação no dia
            if(retorno.Count>0)
            {
                var max = retorno.Max(x => x.data.Getdata());
                if(max>=DateTime.Now)
                {
                    retorno.Add(GetAvanco(new Data(DateTime.Now), fases));
                    retorno = retorno.OrderBy(x => x.data.Getdata()).ToList();
                }
            }
            return retorno;
        }
        private Avanco GetPrevistoAcumulado(Data dmax, List<Fase> fases = null, string descricao=null)
        {
            if(fases==null)
            {
                fases = this.Subfases();
            }
            if(descricao!=null)
            {
                fases = fases.FindAll(x => x.descricao == descricao);
            }
            //var vars = fases.Select(a => new Avanco(a.inicio, a.GetPrevistoDistribuidoDias().ToList().FindAll(x => x.data.Getdata() <= dmax.Getdata()).OrderBy(x => x.data.Getdata()).ToList())).ToList();
            var vars = fases.SelectMany(a => a.GetPrevistoDistribuidoDias().ToList().FindAll(x => x.data.Getdata() <= dmax.Getdata()).OrderBy(x => x.data.Getdata()).ToList()).ToList();
 
            var apon = new Avanco(new Data(dmax.Getdata()), vars);
            return apon;
        }
        public Data primeiro_dia()
        {
            return new Data(Conexoes.Utilz.PrimeiroDiaDaSemana(this.inicio_real.Getdata(), DayOfWeek.Sunday));
        }

        private Avanco GetRealizadoAcumulado(Data dmax, List<Fase> fases= null, string descricao = null)
        {
            if(fases==null)
            {
                fases = this.Subfases();
            }
            if(descricao!=null)
            {
                fases = fases.FindAll(x => x.descricao == descricao);
            }
            var valor = fases.Select(x => x.GetSomaApontamentos(new Data(dmax.Getdata()))).ToList();
            var apon = new Avanco(new Data(dmax.Getdata()),valor);
            return apon;
        }
        public Linha_de_Balanco()
        {

        }
    }
    [Serializable]
    public class Data : INotifyPropertyChanged
    {

        #region property
        [Browsable(false)]
        public event PropertyChangedEventHandler PropertyChanged;
        [Browsable(false)]
        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        [Browsable(false)]
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
        [XmlIgnore]
        [Browsable(false)]
        public int col { get; set; } = 0;
        [XmlIgnore]
        [Browsable(false)]
        public int linha { get; set; } = 0;
        public string datastr
        {
            get
            {

                if (this.Getdata() > new DateTime(2001, 01, 01))
                {
                    return this.dia.ToString().PadLeft(2, '0') + "/" + this.mes.ToString().PadLeft(2, '0') + "/" + this.ano.ToString();
                }
                return "";
            }
        }
        public bool valido
        {
            get
            {
                return this.Getdata() > new DateTime(2001, 01, 01);
            }
        }
        public override string ToString()
        {
            return datastr;
        }
        public int GetSemana()
        {
            if (!valido) { return 0; }
            CultureInfo myCI = new CultureInfo("pt-BR");
            Calendar myCal = myCI.Calendar;

            // Gets the DTFI properties required by GetWeekOfYear.
            CalendarWeekRule myCWR = myCI.DateTimeFormat.CalendarWeekRule;
            DayOfWeek primeiro_dia_semana = myCI.DateTimeFormat.FirstDayOfWeek;
            var retorno = myCal.GetWeekOfYear(Getdata(), myCWR, primeiro_dia_semana);
            return retorno;
        }
        public DateTime Getdata()
        {
            try
            {
                var dt = new DateTime(ano, mes, dia);
                return dt;
            }
            catch (Exception)
            {

            }
            return new DateTime();
        }
        public string GetDiaDaSemana()
        {
            if (!valido) { return "Data inválida"; }
            CultureInfo culture = new CultureInfo("pt-BR");
            DateTimeFormatInfo dtfi = culture.DateTimeFormat;
            string data1 = dtfi.GetDayName(Getdata().DayOfWeek);
            return data1;
        }
        private int _ano { get; set; } = 2001;
        public int ano
        {
            get
            {
                return _ano;
            }
            set
            {
                _ano = value;
                NotifyPropertyChanged("ano");
                NotifyPropertyChanged("dia_da_semana");

            }
        }
        private int _mes { get; set; } = 01;
        public int mes
        {
            get
            {
                return _mes;
            }
            set
            {
                if (value > 0 && value < 13)
                {
                    _mes = value;
                    NotifyPropertyChanged("mes");
                    NotifyPropertyChanged("dia_da_semana");
                }
            }
        }
        private int _dia { get; set; } = 01;
        public int dia
        {
            get
            {
                return _dia;
            }
            set
            {
                if (value > 0 && value < 32)
                {
                    _dia = value;
                    NotifyPropertyChanged("dia");
                    NotifyPropertyChanged("dia_da_semana");
                }
            }
        }
        [XmlIgnore]
        public int semana
        {
            get
            {
                return GetSemana();
            }
        }
        public string dia_da_semana
        {
            get
            {
                return GetDiaDaSemana();
            }
        }
        public Data()
        {

        }
        public Data(Data d)
        {
            this.dia = d.dia;
            this.mes = d.mes;
            this.ano = d.ano;
        }
        public Data(DateTime data)
        {
            if (data != null)
            {
                SetData(data);
            }
        }
        public Data Add(int dias)
        {
            return new Data(Getdata().AddDays(dias));
        }
        public Data(string data)
        {

            if (data != null)
            {
                if (Conexoes.Utilz.ESoNumero(data) && data != "")
                {
                    DateTime result = DateTime.FromOADate(Conexoes.Utilz.Int(data));

                    SetData(result);
                }
                else
                {
                    SetData(Conexoes.Utilz.Data(data));
                }
            }
        }
        public void SetData(DateTime data)
        {
            this.dia = data.Day;
            this.mes = data.Month;
            this.ano = data.Year;

        }
        public void SetData(Data data)
        {
            this.dia = data.dia;
            this.mes = data.mes;
            this.ano = data.ano;

        }
        public Data(ExcelRangeBase celula)
        {
            celula.Calculate();
            if (celula.Value != null)
            {
                this.SetData(Conexoes.Utilz.Data(celula.Text));
                if (!valido)
                {
                    if (celula.Value is DateTime)
                    {
                        this.SetData((DateTime)celula.Value);
                    }
                    else
                    {
                        var dt = new DateTime(1900, 01, 01).AddDays(Conexoes.Utilz.Double(celula.Value) - 2);
                        if (dt >= DateTime.Now.AddYears(-10) && dt <= DateTime.Now.AddYears(10))
                        {
                            this.SetData(dt);
                        }
                    }


                }
                this.col = celula.End.Column;
                this.linha = celula.End.Row;
            }
        }
    }
    [Serializable]
    public enum Tipo
    {
        Previsto,
        Realizado,
        Equipamento,
        Supervisor,
        Improdutividade,
        Avanco_Etapa,
    }
    public enum Tipo_Recurso
    {
        Recurso,
        Supervisor,
        Improdutividade,
        Custo,
    }
    [Serializable]
    public class Recurso : INotifyPropertyChanged
    {
        #region property
        [Browsable(false)]
        public event PropertyChangedEventHandler PropertyChanged;
        [Browsable(false)]
        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        [Browsable(false)]
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
        public string Getid(Linha_de_Balanco linha)
        {

            var subs = linha.GetEfetivosERecursos();

            var igual = subs.FindAll(x => x.id != "").Find(x => x.id == this.id);
            if (igual == null)
            {
                igual = subs.Find(x => x.ToString().ToUpper().Replace(" ", "").Replace("_", "") == this.ToString().ToUpper().Replace("_", "").Replace(" ", ""));
            }

            if (igual == null)
            {
                igual = subs.FindAll(x => x.descricao + "." + x.equipe != ".").Find(x => (x.descricao + "." + x.equipe).ToUpper().Replace(" ", "").Replace("_", "") == (this.descricao + "." + this.equipe).Replace("_", "").Replace(" ", ""));
            }

            if (igual == null)
            {
                igual = subs.FindAll(x => x.descricao != "").Find(x => x.descricao.ToUpper().Replace(" ", "").Replace("_", "") == this.descricao.ToUpper().Replace("_", "").Replace(" ", ""));
            }
            if (igual != null)
            {
                this.id = igual.id;
                return this.id;
            }
            return "";
        }

        public List<Avanco> GetAvancos()
        {
            var prevs = GetPrevistos();
            var reals = GetApontamentos();
            List<Data> datas = new List<Data>();
            datas.AddRange(prevs.Select(x => x.data));
            datas.AddRange(reals.Select(x => x.data));
            datas = datas.GroupBy(x => x.datastr).Select(x => x.First()).ToList();
            datas = datas.OrderBy(x => x.Getdata()).ToList();
            List<Avanco> retorno = new List<Avanco>();
            foreach(var dt in datas)
            {
                var prev = prevs.Find(x => x.data.datastr == dt.datastr);
                var real = reals.Find(x => x.data.datastr == dt.datastr);
                Avanco pp = new Avanco(dt, prev != null ? prev.valor:0, real != null ? real.valor:0,"",1);
                retorno.Add(pp);
            }
            return retorno;
        }
        public List<Apontamento> GetPrevistos()
        {
            List<Apontamento> retorno = new List<Apontamento>();
            var max = this.valor_previsto_importado;
            if (this.descricao.ToUpper().Contains("EFETIVO"))
            {
                max = this.lob.GetEfetivoPrevisto(this).Sum(x => x.total_efetivo);
            }
            if (max > 0)
            {
                var uteis = Conexoes.Utilz.GetRangeDatas(inicio.Getdata(), fim.Getdata(), false, false);
                var dias = (this.fim.Getdata() - this.inicio.Getdata()).Days;
                var d0 = this.inicio.Getdata();
                var valor = Math.Floor(max / uteis.Count);

                var resto = max - (valor * uteis.Count);
                int i = 0;
                if (valor > 0)
                {
                    foreach (var s in uteis)
                    {
                        retorno.Add(new Apontamento(new Data(s), valor + (i < resto ? 1 : 0)));
                        i++;
                    }
                }
                else
                {
                    //quando os dias são maiores que a quantidade, distribui
                    var entre_dias = (int)Math.Ceiling(uteis.Count / max);
                    var aponts = Math.Floor((double)uteis.Count / (double)entre_dias);
                    resto = this.total_previsto - aponts;

                    for (int a = 0; a < uteis.Count;)
                    {
                        double vlr = 1;
    
                        
                        retorno.Add(new Apontamento(new Data(uteis[a]),vlr));
                        a = a + entre_dias;
                    }
                }
            }
            var rst = max - retorno.Sum(x => x.valor);
            foreach(var s in retorno)
            {
                if(rst>0)
                {
                    s.valor = s.valor + 1;
                    rst = rst - 1;
                }

            }
            return retorno;
        }
        public List<Avanco> GetAvancosAcumulados(bool update = false, int dias = 7, Data dmax = null)
        {
            List<Avanco> retorno = new List<Avanco>();
            var dt = inicio.Getdata();
            if (dmax == null)
            {
                dmax = new Data(this.fim.Getdata());

            }
            while (dt <= dmax.Getdata())
            {
                Avanco apon = GetAvanco(dt,dias);
                retorno.Add(apon);
                dt = dt.AddDays(dias);
            }
            if (retorno.Count > 0)
            {
                if (retorno.Last().data.Getdata() < dmax.Getdata())
                {
                    var ddias = (dmax.Getdata() - retorno.Last().data.Getdata()).Days;
                    Avanco apon = GetAvanco(dmax.Getdata(), ddias);
                    retorno.Add(apon);
                }
            }
            return retorno;
        }
        public Avanco GetAvanco(DateTime dt, int dias = 7)
        {
            var prev = GetPrevistoAcumulado(dias, dt);
            var real = GerRealizadoAcumulado(dias, dt);
            List<Avanco> retorno = new List<Avanco>();
 
            return new Avanco(new Data(dt), prev.valor, real.valor,this.ToString(),1);
        }
        public Apontamento GetPrevistoAcumulado(int dias, DateTime dt)
        {
            var prev = this.GetPrevistos();
            return new Apontamento(new Data(dt), prev.FindAll(x => x.data.Getdata() <= dt && x.data.Getdata() >= dt.AddDays(-dias+1)).Sum(x => x.valor));
        }
        public Apontamento GerRealizadoAcumulado(int dias, DateTime dt)
        {
            return new Apontamento(new Data(dt), this.GetApontamentos().FindAll(x => x.data.Getdata() <= dt && x.data.Getdata() >= dt.AddDays(-dias+1)).Sum(x => x.valor));
        }
        [Browsable(false)]
        public Tipo_Recurso tipo { get; set; } = Tipo_Recurso.Recurso;
        [DisplayName("Equipe")]
        public string equipe
        {
            get
            {
                if (_equipe.Replace(" ", "").Length == 0)
                {
                    return "Indefinido";
                }
                return _equipe;
            }
            set
            {
                _equipe = value;
                NotifyPropertyChanged("equipe");
            }
        }
        private string _equipe { get; set; } = "";
        [Browsable(false)]
        public string supervisor { get; set; } = "";
        [Browsable(false)]
        public string motivo { get; set; } = "";
        [Browsable(false)]
        public string cargo { get; set; } = "";
        [Browsable(false)]
        public string chave
        {
            get
            {
                return (this.equipe != "" ? this.equipe + " - " : "") + this.descricao;
            }
        }
        [XmlIgnore]
        [Browsable(false)]
        public double realizado { get; set; } = 0;
        [XmlIgnore]
        [Browsable(false)]
        public Apontamento ultimo { get; set; } = new Apontamento();
        [XmlIgnore]
        [Browsable(false)]
        private List<Apontamento> _apontamentos { get; set; }
        public List<Apontamento> GetApontamentos(Apontamentos lista = null, bool update = false)
        {
            if (_apontamentos == null | update)
            {
                if (lista == null)
                {
                    lista = lob.Getapontamentos();
                }
                if (lista == null) { return new List<Apontamento>(); }
                Setid();
                this._apontamentos = lista.apontamentos.FindAll(x => x.id_pai == this.id).OrderBy(x => x.data.Getdata()).ToList();

                if (this._apontamentos.Count == 0 && this.chave.Length > 0)
                {
                    this._apontamentos = lista.apontamentos.FindAll(x => x.chave_pai == this.chave);
                    for (int i = 0; i < this._apontamentos.Count; i++)
                    {
                        this._apontamentos[i].id_pai = this.id;
                    }
                }

                if (this._apontamentos.Count > 0)
                {
                    this.ultimo = this._apontamentos.Last();
                    this.realizado = this.ultimo.valor;
                    for (int i = 0; i < this._apontamentos.Count; i++)
                    {
                        this._apontamentos[i].chave_pai = this.chave;
                    }
                }
                NotifyPropertyChanged("apontamentos");
            }


            return _apontamentos;
        }
        [Browsable(false)]
        [XmlIgnore]
        public Linha_de_Balanco lob { get; set; } = new Linha_de_Balanco();
        public void Setid()
        {
            if (this.id == "")
            {
                this.id = Conexoes.Utilz.RandomString(Vars.CompRandom);
            }
        }
        public void RemApontamento(Apontamento apontamento, Apontamentos lista = null)
        {
            if (lista == null)
            {
                lista = this.lob.Getapontamentos();
            }
            if (lista == null)
            {
                return;
            }
            lista.apontamentos.Remove(apontamento);
            Update();
        }
        public void AddApontamento(Data data, double valor, string descricao, Tipo tipo, Apontamentos lista = null)
        {
            if (lista == null)
            {
                lista = this.lob.Getapontamentos();
            }
            if (lista == null)
            {
                return;
            }
            Setid();
            lista.Add(this, valor, data, descricao, tipo);
            Update();
        }
        public void Update()
        {
            this.GetApontamentos(null, true);
            NotifyPropertyChanged("previsto");
            NotifyPropertyChanged("ultimo");
            NotifyPropertyChanged("realizado");
            NotifyPropertyChanged("diaria_util");
            NotifyPropertyChanged("descricao");
            NotifyPropertyChanged("total_previsto");
            NotifyPropertyChanged("total_utilizado");
            NotifyPropertyChanged("custo_realizado");
            NotifyPropertyChanged("disponivel");
        }
        [Browsable(false)]
        public Data inicio
        {
            get
            {
                if (this.lob.inicio_real.valido) { return this.lob.inicio_real; };
                var dts = previsto.FindAll(x => x.data.valido);
                dts.AddRange(this.GetApontamentos().ToList().FindAll(x => x.data.valido));
                if (dts.Count > 0)
                {
                    return new Data(dts.Min(x => x.data.Getdata()));
                }
                return new Data(new DateTime(2001, 01, 01));
            }
        }
        [Browsable(false)]
        public Data fim
        {
            get
            {
                if (this.lob.fim_real.valido) { return this.lob.fim_real; };
                var dts = previsto.FindAll(x => x.data.valido);
                dts.AddRange(this.GetApontamentos().ToList().FindAll(x => x.data.valido));

                if (dts.Count > 0)
                {
                    return new Data(dts.Max(x => x.data.Getdata()));
                }
                return new Data(new DateTime(2001, 01, 01));
            }
        }
        [Browsable(false)]
        public string id { get; set; } = "";
        [DisplayName("Equipamento")]
        public string descricao
        {
            get
            {
                return _descricao;
            }
            set
            {
                _descricao = value;
                NotifyPropertyChanged("descricao");
            }
        }
        private string _descricao { get; set; } = "";
        [Browsable(false)]
        public double custo_mensal { get; set; } = 0;
        [Browsable(false)]
        public double diaria_util { get; set; } = 0;
        [Browsable(false)]
        public double custo_realizado
        {
            get
            {
                return total_utilizado * diaria_util;
            }
        }
        [Browsable(false)]
        public double total_previsto
        {
            get
            {
                return this.valor_previsto_importado;
            }
        }
        [Browsable(false)]
        public double total_disponivel
        {
            get
            {

                var s = this.total_previsto - this.total_utilizado;
                if (s > 0)
                {
                    return s;
                }
                return 0;
            }
        }
        [Browsable(false)]
        public double total_utilizado
        {
            get
            {
                return this.GetApontamentos().Sum(x => x.valor);
            }
        }
        public override string ToString()
        {
            return this.descricao + " [P: " + this.total_previsto + " U: " + this.total_utilizado + "]";
        }
        [Browsable(false)]
        public List<Apontamento> previsto { get; set; } = new List<Apontamento>();
        [DisplayName("Previsto")]
        public double valor_previsto_importado
        {
            get
            {
                if (this.descricao.ToUpper().Contains("EFETIVO") && _diarias_efetivo == -1)
                {
                    GetDiarias_Efetivo();
                }
                else if (this.descricao.ToUpper().Contains("EFETIVO"))
                {
                    return _diarias_efetivo;
                }
               if(this.previsto.Count>0)
                {
                    return this.previsto.Sum(x => x.valor);
                }
                return _valor_previsto_importado;
            }
            set
            {
                _valor_previsto_importado = value;
              
                NotifyPropertyChanged("valor_previsto_importado");
            }
        }
        public void GetDiarias_Efetivo()
        {
            _diarias_efetivo = this.lob.GetEfetivoPrevisto(this).Sum(x => x.total_efetivo);
        }
        [XmlIgnore]
        [Browsable(false)]
        private double _diarias_efetivo { get; set; } = -1;
        private double _valor_previsto_importado { get; set; } = 0;
        [Browsable(false)]
        public double disponivel
        {
            get
            {
                var s = this.total_previsto - this.total_utilizado;
                if (s < 0)
                {
                    return 0;
                }
                else
                {
                    return s;
                }
            }
        }
        public Recurso(string Descricao, Tipo_Recurso tipo = Tipo_Recurso.Recurso)
        {
            this.descricao = Descricao;
            this.tipo = tipo;
        }
        public Recurso()
        {

        }
    }
    [Serializable]
    public class Fase : INotifyPropertyChanged
    {
        public List<Avanco> GetPrevistoDistribuidoDias(bool peso_fase = true)
        {
            List<Avanco> retorno = new List<Avanco>();
            var dmin = this.inicio_real.Getdata();
            var dmax = this.fim_real.Getdata();
            var dias_uteis = Conexoes.Utilz.GetRangeDatas(dmin, dmax, false, false);

            //arredondei os valores e joguei o resto no final.
            double valor_dia = Math.Round((double)(1.00 / (double)dias_uteis.Count),2);
            var resto = 1 - (valor_dia * (double)dias_uteis.Count);
            int c = 0;
            foreach (var dia in dias_uteis)
            {
                retorno.Add(new Avanco(new Data(dia), (valor_dia + (c==dias_uteis.Count-1?resto:0)) * (peso_fase?this.peso_fase:1) *100,0,this.ToString(), this.peso_fase));
                c++;
            }
            return retorno;
        }
        public Avanco GetSomaApontamentos(Data ate)
        {
            return new Avanco(new Data(ate.Getdata()), this.GetApontamentos().FindAll(x => x.data.Getdata() <= ate.Getdata()).Select(x => new Avanco(new Data(x.data.Getdata()),0, x.valor * this.peso_fase,x.ToString(), this.peso_fase)).ToList());
        }
        public override string ToString()
        {
            return ((this.pai != null ? this.pai.ToString() + " - " : "") + this.nomestr).Replace("_", " ").Replace(",", ".").ToUpper();
        }
        public string nomestr
        {
            get
            {
                return (((this.cod != "" && this.fases.Count == 0) ? this.cod + " - " : "") + this.descricao).Replace("_", " ").Replace(",", ".").ToUpper();
            }
        }
        public double TotalApontado
        {
            get
            {
                return this.GetApontamentos().Sum(y => y.valor);
            }
        }
        #region property
        [Browsable(false)]
        public event PropertyChangedEventHandler PropertyChanged;
        [Browsable(false)]
        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        [Browsable(false)]
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
        [XmlIgnore]
        [Browsable(false)]
        public double realizado
        {
            get
            {
                if (fases.Count > 0)
                {
                    return fases.Sum(x => x.realizado);
                }
                return _realizado;
            }
            set
            {
                _realizado = value;
            }
        }
        private double _realizado { get; set; } = 0;
        [XmlIgnore]
        [Browsable(false)]
        public Linha_de_Balanco lob { get; set; }
        [XmlIgnore]
        [Browsable(false)]
        private List<Apontamento> _apontamentos { get; set; }
        public List<Apontamento> GetApontamentos(Apontamentos lista = null, bool update = false)
        {
            if (_apontamentos == null | update)
            {
                //if (this.descricao == "" && this.pai != null)
                //{
                //    var s = this.fases.Select(x => x.pai).GroupBy(x => x).Select(x => x.First()).ToList();
                //    s = s.FindAll(x => x != null).ToList();
                //    foreach (var p in s)
                //    {
                //        p.GetApontamentos();
                //    }
                //}
                if (lista == null)
                {
                    if (lob == null)
                    {
                        lob = new Linha_de_Balanco();
                    }
                    lista = lob.Getapontamentos();
                }
                if (lista == null) { return new List<Apontamento>(); }
                Setid();
                this._apontamentos = lista.apontamentos.FindAll(x => x.id_pai == this.id).OrderBy(x => x.data.Getdata()).ToList();

                if (this._apontamentos.Count == 0 && this.chave.Length > 0)
                {
                    this._apontamentos = lista.apontamentos.FindAll(x => x.chave_pai == this.chave);
                    for (int i = 0; i < this._apontamentos.Count; i++)
                    {
                        this._apontamentos[i].id_pai = this.id;

                    }
                }

                if (this._apontamentos.Count > 0)
                {

                    this.realizado = this._apontamentos.Sum(x => x.valor);
                    for (int i = 0; i < this._apontamentos.Count; i++)
                    {
                        this._apontamentos[i].chave_pai = this.chave;
                    }
                }
                for (int i = 0; i < fases.Count; i++)
                {
                    fases[i].GetApontamentos(lista);
                }
            }
            return _apontamentos;
        }
        public void Setid()
        {
            if (this.id == "")
            {
                this.id = Conexoes.Utilz.RandomString(Vars.CompRandom);
            }
        }
        public string chave
        {
            get
            {
                return (this.pai != null ? this.pai.chave + " " : "") + (this.fases.Count == 0 ? this.cod + " - " : "") + this.descricao;
            }
        }
        public void RemApontamento(Apontamento apontamento, Apontamentos lista = null)
        {
            if (lista == null)
            {
                lista = this.lob.Getapontamentos();
            }
            if (lista == null)
            {
                return;
            }
            lista.apontamentos.Remove(apontamento);
            GetApontamentos(null, true);
        }
        public void AddApontamento(Data data, double valor, string descricao, Apontamentos lista = null)
        {
            if (lista == null)
            {
                lista = this.lob.Getapontamentos();
            }
            if (lista == null)
            {
                return;
            }
            Setid();
            lista.Add(this, valor, data, descricao);
            this.GetApontamentos(null, true);
        }
        public void SomarApontamento(Data data, double valor, Apontamentos lista = null)
        {
            if (lista == null)
            {
                lista = this.lob.Getapontamentos();
            }
            if (lista == null)
            {
                return;
            }

            var s = GetApontamentos();


            Setid();
            lista.Add(this, valor, data, "");
            GetApontamentos(null, true);
        }
        [Browsable(false)]
        public string id { get; set; } = "";

        [Browsable(false)]
        public int dias
        {
            get
            {
                var dias = Conexoes.Utilz.DiasUteis(this.inicio.Getdata(),this.fim.Getdata());
                if (dias > 0)
                {
                    return dias;
                }
                return 0;
            }
        }
        public double total_efetivo
        {
            get
            {
                return this.efetivo * dias;
            }
        }
        [Browsable(false)]
        public Data inicio { get; set; } = new Data();
        public Data inicio_real
        {
            get
            {
                if (this.fases.Count > 0)
                {
                    return new Data(this.fases.Min(x => x.inicio.Getdata()));
                }
                return this.inicio;
            }
        }
        public Data fim_real
        {
            get
            {
                if (this.fases.Count > 0)
                {
                    return new Data(this.fases.Max(x => x.fim.Getdata()));
                }
                return this.fim;
            }
        }
        [Browsable(false)]
        public Data fim { get; set; } = new Data();
        public string descricao { get; set; } = "";
        public void SetInicios()
        {
            if (fases.Count > 0)
            {
                var ini = fases.FindAll(x => x.inicio.ano > 2001);
                var ff = fases.FindAll(x => x.fim.ano > 2001);

                if (ini.Count > 0)
                {
                    this.inicio = new Data(ini.Min(x => x.inicio.Getdata()));
                }

                if (ff.Count > 0)
                {
                    this.fim = new Data(ff.Max(x => x.fim.Getdata()));
                }
            }
        }
        [XmlIgnore]
        [Browsable(false)]
        public Fase pai { get; set; }
        [Browsable(false)]
        public List<Fase> fases { get; set; } = new List<Fase>();
        [DisplayName("Área")]
        public double area
        {
            get
            {
                return _area;
            }
            set
            {
                _area = value;
                NotifyPropertyChanged();
            }
        }
        private double _area { get; set; } = 0;
        [DisplayName("Peso da Subetapa")]
        public double peso_fase
        {
            get
            {
                if (this.fases.Count > 0)
                {
                    return this.fases.Sum(x => x.peso_fase);
                }
                return this._peso_fase;
            }
            set
            {
                if (value > 0 && value <= 100)
                {
                    this._peso_fase = value;
                    NotifyPropertyChanged();
                }

            }
        }
        private double _peso_fase { get; set; } = 0;
        [DisplayName("Equipe")]
        public string equipe
        {
            get
            {
                if(_equipe ==null)
                {
                    return "Indefinido";
                }
                if (_equipe.Replace(" ", "").Length == 0)
                {
                    return "Indefinido";
                }
                return _equipe;

            }
            set
            {
                _equipe = value;
                NotifyPropertyChanged();
            }
        }
        private string _equipe { get; set; } = "";
        [DisplayName("PEP")]
        public string pep
        {
            get
            {
                return _pep;

            }
            set
            {
                _pep = value;
                NotifyPropertyChanged();
            }
        }
        private string _pep { get; set; } = "";
        [DisplayName("Efetivo/Dia")]
        public double efetivo { get; set; } = 0;
        [DisplayName("Sub-Etapa")]
        public string cod
        {
            get
            {
                return _cod;
            }
            set
            {
                _cod = value;
                NotifyPropertyChanged();
            }
        }
        private string _cod { get; set; } = "";
        public string Getid(Linha_de_Balanco linha)
        {
            if (this.fases.Count > 0)
            {
                return "";
            }
            var subs = linha.Subfases();

            var igual = subs.FindAll(x => x.id != "").Find(x => x.id == this.id);

            if (igual != null)
            {
                return igual.id;
            }

            if (igual == null)
            {
                igual = subs.Find(x => x.chave.ToUpper().Replace(" ", "").Replace("_", "") == this.chave.ToUpper().Replace("_", "").Replace(" ", ""));
            }

            if (igual != null)
            {
                this.id = igual.id;
                return this.id;
            }
            return "";
        }
        public Fase()
        {

        }
    }
    [Serializable]
    public class Apontamento : INotifyPropertyChanged
    {
        public void Copiar(Apontamento ap)
        {
            this.data = ap.data;
            this.descricao = ap.descricao;
            this.efetivo = ap.efetivo;
            this.tipo = ap.tipo;
            this.valor = ap.valor;
            this.responsavel = ap.responsavel;
        }
        #region property
        [Browsable(false)]
        public event PropertyChangedEventHandler PropertyChanged;
        [Browsable(false)]
        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        [Browsable(false)]
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
        public string descricao
        {
            get
            {
                return _descricao;
            }
            set
            {
                _descricao = value;
                NotifyPropertyChanged("descricao");
            }
        }
        private string _descricao { get; set; } = "";
        public string responsavel
        {
            get
            {
                return _responsavel;
            }
            set
            {
                _responsavel = value;
                NotifyPropertyChanged("responsavel");
            }
        }
        private string _responsavel { get; set; } = "";
        public string id_pai { get; set; } = "";
        public string chave_pai { get; set; } = "";
        public override string ToString()
        {
            return this.data.ToString() + " - " + this.tipo.ToString() + " - " + this.valor;
        }
        private Data _data { get; set; } = new Data();
        public Data data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
                NotifyPropertyChanged("data");
            }
        }
        public Tipo tipo { get; set; } = Tipo.Previsto;
        private double _valor { get; set; } = 0;
        public double valor
        {
            get
            {

                return _valor;
            }
            set
            {
                _valor = value;
                NotifyPropertyChanged("valor");
            }
        }
        private double _efetivo { get; set; } = 0;
        public double efetivo
        {
            get
            {
                return _efetivo;
            }
            set
            {
                _efetivo = value;
                NotifyPropertyChanged("efetivo");
            }
        }
        public Apontamento()
        {

        }
        public Apontamento(Data data, double valor, Tipo tipo = Tipo.Previsto, string descricao = "")
        {
            this.data = data;
            this.tipo = tipo;
            this.valor = valor;
            this.descricao = descricao;
        }
    }
    [Serializable]
    public class Apontamentos
    {
        public string arquivo
        {
            get
            {
                return (this.diretorio.EndsWith(@"\") ? this.diretorio : this.diretorio + @"\") + nomearq;
            }
        }
        public override string ToString()
        {
            return "Apontamentos: " + this.apontamentos.Count;
        }
        public void Salvar(string diretorio)
        {
            if (!Directory.Exists(diretorio))
            {

                return;
            }
            var s = Conexoes.Utilz.RetornarSerializado<Apontamentos>(this);
            if (!diretorio.EndsWith(@"\"))
            {
                diretorio = diretorio + @"\";
            }
            string arquivo = diretorio + nomearq;
            Conexoes.Utilz.GravarArquivo(arquivo, new List<string> { s }, Encoding.GetEncoding(1252));
        }
        public string diretorio { get; set; } = "";
        public string nomearq { get; set; } = "apontamentos.cfg";
        public Apontamentos Carregar(string diretorio)
        {
            if (!diretorio.EndsWith(@"\"))
            {
                diretorio = diretorio + @"\";
            }
            var arq = diretorio + nomearq;
            if (File.Exists(arq))
            {
                DateTime min = new DateTime(2001, 01, 01);

                var carregar = string.Join("", Conexoes.Utilz.LerArquivo(arq, Encoding.GetEncoding(1252)));
                if (carregar.Length > 0)
                {
                    var s = Conexoes.Utilz.LerSerializado<Apontamentos>(carregar);
                    if (s != null)
                    {
                        s.diretorio = diretorio;

                        return s;
                    }
                }
            }
            return new Apontamentos();
        }
        public List<Apontamento> apontamentos { get; set; } = new List<Apontamento>();
        public Apontamentos()
        {

        }
        public void Add(Fase fase, double valor, Data data, string descricao)
        {
            Apontamento pp = new Apontamento(data, valor, Tipo.Avanco_Etapa);

            if (fase.id == "")
            {
                fase.id = Conexoes.Utilz.RandomString(Vars.CompRandom);
            }
            pp.id_pai = fase.id;
            pp.chave_pai = fase.chave;
            pp.descricao = descricao;

            this.apontamentos.Add(pp);
        }
        public void Add(Recurso fase, double valor, Data data, string descricao, Tipo tipo = Tipo.Equipamento)
        {
            Apontamento pp = new Apontamento(data, valor, tipo);

            if (fase.id == "")
            {
                fase.id = Conexoes.Utilz.RandomString(Vars.CompRandom);
            }
            pp.chave_pai = fase.chave;
            pp.id_pai = fase.id;
            pp.descricao = descricao;
            this.apontamentos.Add(pp);
        }

    }
    [Serializable]
    public class Diario
    {
        public void Salvar(string diretorio)
        {
            if (!Directory.Exists(diretorio))
            {

                return;
            }
            var s = Conexoes.Utilz.RetornarSerializado<Diario>(this);
            if (!diretorio.EndsWith(@"\"))
            {
                diretorio = diretorio + @"\";
            }
            string arquivo = diretorio + nomearq;
            Conexoes.Utilz.GravarArquivo(arquivo, new List<string> { s }, Encoding.GetEncoding(1252));
        }
        public string diretorio { get; set; } = "";
        public string nomearq { get; set; } = "diario.cfg";
        public Diario Carregar(string diretorio)
        {
            if (!diretorio.EndsWith(@"\"))
            {
                diretorio = diretorio + @"\";
            }
            var arq = diretorio + nomearq;
            if (File.Exists(arq))
            {
                DateTime min = new DateTime(2001, 01, 01);

                var carregar = string.Join("", Conexoes.Utilz.LerArquivo(arq, Encoding.GetEncoding(1252)));
                if (carregar.Length > 0)
                {
                    var s = Conexoes.Utilz.LerSerializado<Diario>(carregar);
                    if (s != null)
                    {
                        s.diretorio = diretorio;
                        return s;
                    }
                }
            }
            return new Diario();
        }
        public List<Apontamento> apontamentos { get; set; } = new List<Apontamento>();
        public Diario()
        {

        }
        public void Add(Data data, string descricao)
        {
            Apontamento pp = new Apontamento(data, 0, Tipo.Avanco_Etapa);
            pp.descricao = descricao;
            this.apontamentos.Add(pp);
            this.apontamentos = this.apontamentos.OrderBy(x => x.data.Getdata()).ToList();
        }
    }
    [Serializable]
    public class Avanco
    {
        public double peso { get; set; } = 1;
        public double previsto_peso
        {
            get
            {
                if(this.avancos.Count>0)
                {
                    return this.avancos.Sum(x => x.previsto_peso);
                }
                return this.previsto / peso;
            }
        }
        public double realizado_peso
        {
            get
            {
                if (this.avancos.Count > 0)
                {
                    return this.avancos.Sum(x => x.realizado_peso);
                }
                return this.realizado / peso;
            }
        }
        public List<Avanco> avancos { get; set; } = new List<Avanco>();
        public string descricao { get; set; } = "";
        public double desvio
        {
            get
            {
                //var sm = data.GetSemana();
                //var sm_2 = new Data(DateTime.Now).GetSemana() + DateTime.Now.Year;
                if(data.Getdata() <= DateTime.Now)
                {
                return Math.Round(realizado - previsto,2);
                }
                return 0;
            }
        }
        public double max
        {
           get
            {
                return previsto > realizado ? previsto : realizado;
            }
        }
        public double min
        {
            get
            {
                return previsto < realizado ? previsto : realizado;
            }
        }
        public override string ToString()
        {
            return data.ToString() + (descricao != "" ? " - " + descricao + " - " : "") + descr + (avancos.Count > 0 ? " [" + avancos.Count.ToString().PadLeft(2, '0') + " avs]" : "");
        }
        public string descr
        {
            get
            {
                return "P: " + Math.Round(this.previsto, 2) + "%  R:" + Math.Round(this.realizado, 2) + "%" ;
            }
        }
        public Data data { get; set; } = new Data();
        private double _previsto { get; set; } = 0;
        public double previsto
        {
            get
            {
                if(avancos.Count>0)
                {
                    return avancos.Sum(x => x.previsto);
                }
                return _previsto;
            }
            set
            {
                _previsto = value;
            }
        }
        private double _realizado { get; set; } = 0;
        public double realizado
        {
            get
            {
                if (avancos.Count > 0)
                {
                    return avancos.Sum(x => x.realizado);
                }
                return _realizado;
            }
            set
            {
                _realizado = value;
            }
        }
        public double multiplicador { get; set; } = 1;
        public Avanco()
        {

        }
        public Avanco(Data data, double previsto, double realizado, string descricao, double peso)
        {
            this.data = data;
            this.previsto = previsto;
            this.realizado = realizado;
            this.descricao = descricao;
            this.peso = peso;

        }
        
        public Avanco(Data data, List<Avanco> avancos, string descricao = "")
        {
            this.data = data;
            this.avancos = new List<Avanco>(avancos.FindAll(x => x.previsto > 0 | x.realizado > 0).ToList().OrderBy(x => x.data.Getdata()).ToList());
            this.descricao = descricao;
        }

    }

    [Serializable]
    public class Restricao : INotifyPropertyChanged
    {
        public override string ToString()
        {
            return this.data + " - " + this.pep + " - " + this.descricao;
        }
        #region property
        [Browsable(false)]
        public event PropertyChangedEventHandler PropertyChanged;
        [Browsable(false)]
        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        [Browsable(false)]
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
        [ReadOnly(true)]
        [DisplayName("Data")]
        public Data data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
                NotifyPropertyChanged("data");
            }
        }
        private Data _data { get; set; } = new Data();


        [DisplayName("PEP")]
        public string pep
        {
            get
            {
                return _pep;
            }
            set
            {
                _pep = value;
                NotifyPropertyChanged("pep");
            }
        }
        private string _pep { get; set; } = "";

        [DisplayName("Descrição")]
        public string descricao
        {
            get
            {
                return _descricao;
            }
            set
            {
                _descricao = value;
                NotifyPropertyChanged("descricao");
            }
        }
        private string _descricao { get; set; } = "";
        public Restricao()
        {

        }
    }


    [Serializable]
    public class Observacao : INotifyPropertyChanged
    {
        public override string ToString()
        {
            return this.data + " - " + this.responsavel + " - " + this.descricao;
        }
        #region property
        [Browsable(false)]
        public event PropertyChangedEventHandler PropertyChanged;
        [Browsable(false)]
        public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        [Browsable(false)]
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        [DisplayName("Data")]
        [ReadOnly(true)]
        public Data data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
                NotifyPropertyChanged("data");
            }
        }
        private Data _data { get; set; } = new Data();


        [DisplayName("Responsável")]
        public string responsavel
        {
            get
            {
                return _responsavel;
            }
            set
            {
                _responsavel = value;
                NotifyPropertyChanged("responsavel");
            }
        }
        private string _responsavel { get; set; } = "";
        [DisplayName("Descrição")]
        public string descricao
        {
            get
            {
                return _descricao;
            }
            set
            {
                _descricao = value;
                NotifyPropertyChanged("descricao");
            }
        }
        private string _descricao { get; set; } = "";

        public Observacao()
        {

        }
    }
}
