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

namespace GCM
{
    [Serializable]
    public class  Linha_de_Balanco
    {
        public void SalvarTudo()
        {
            this.Salvar(this.diretorio);
            this.Getapontamentos().Salvar(this.diretorio);
        }
        public void SetPesoAvanco(Fase fase)
        {
            if(fase.peso_fase>0)
            {
                return;
            }
            var peso = this.fases_pesos_avanco_fisico.Find(x => x.cod == fase.cod);
            if(peso!=null)
            {
            fase.peso_fase = peso.peso_fase*100;
            }
        }
        [XmlIgnore]
        private Apontamentos _apontamentos { get; set; }
        public Apontamentos Getapontamentos()
        {
            if (Directory.Exists(diretorio) && _apontamentos == null)
            {
                _apontamentos = new Apontamentos().Carregar(this.diretorio);
            }
            return _apontamentos;
        }
        public string arquivo { get
            {
                return this.diretorio.EndsWith(@"\")?this.diretorio:this.diretorio + @"\" + nomearq;
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
        public Linha_de_Balanco Carregar(string diretorio)
        {
            if (!diretorio.EndsWith(@"\"))
            {
                diretorio = diretorio + @"\";
            }
            var arq = diretorio + nomearq;
            if(File.Exists(arq))
            {
                DateTime min = new DateTime(2001, 01, 01);

                var carregar = string.Join("",Conexoes.Utilz.LerArquivo(arq, Encoding.GetEncoding(1252)));
                if(carregar.Length>0)
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
        public void Salvar(string diretorio)
        {
            if (!Directory.Exists(diretorio))
            {
               
                return;
            }
            var s=  Conexoes.Utilz.RetornarSerializado<Linha_de_Balanco>(this);
            if(!diretorio.EndsWith(@"\"))
            {
                diretorio = diretorio + @"\";
            }
            string arquivo = diretorio + nomearq;
            Conexoes.Utilz.GravarArquivo(arquivo, new List<string> { s }, Encoding.GetEncoding(1252));
        }
        public void Ajustes()
        {
            //ajusta as datas das fases.
            if (this.fases.Count > 0)
            {
                foreach(var fase in this.fases)
                {
                    fase.SetInicios();
                }
                DateTime min = new DateTime(2001, 01, 01);
                var ini = this.fases.FindAll(x => x.inicio.Getdata() > new DateTime(2001, 01, 01));
                var ff = this.fases.FindAll(x => x.fim.Getdata() > new DateTime(2001, 01, 01));
                if (ini.Count > 0)
                {
                    this.inicio = new Data(ini.Min(x => x.inicio.Getdata()));

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
                    this.fim = new Data(ini.Max(x => x.fim.Getdata()));
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

            var dt = this.inicio.Getdata();
            var fim = this.fim.Getdata();

            //vincula as fases com o lob
            for (int a = 0; a < this.fases.Count; a++)
            {
                this.fases[a].lob = this;

                for (int b = 0; b < this.fases[a].fases.Count; b++)
                {
                    //atualiza o peso do acanço
                    this.SetPesoAvanco(this.fases[a].fases[b]);
                    this.fases[a].fases[b].lob = this;
                    this.fases[a].fases[b].pai = this.fases[a];
                    this.fases[a].fases[b].UpdateApontamentos();
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
            }
        }
        public Data inicio { get; set; } = new Data();
        public Data fim { get; set; } = new Data();



        public List<Fase> fases_pesos_avanco_fisico { get; set; } = new List<Fase>();
        public List<Recurso> recursos_custo { get; set; } = new List<Recurso>();
        public string msgerro { get; set; } = "";
        public string arquivoexcel { get; set; } = "";
        public List<Fase> fases { get; set; } = new List<Fase>();
        public List<Recurso> recursos__previstos { get; set; } = new List<Recurso>();
        public Linha_de_Balanco()
        {

        }
    }
   [Serializable]
   public class Data
    {
        public bool valido
        {
            get
            {
                return this.Getdata() > new DateTime(2001, 01, 01);
            }
        }
        public override string ToString()
        {
            if(this.Getdata()>new DateTime(2001,01,01))
            {
            return this.dia.ToString().PadLeft(2, '0') + "/" + this.mes.ToString().PadLeft(2, '0') + "/" + this.ano.ToString();
            }
            return "";
        }
        public int GetSemana(DateTime date)
        {
            CultureInfo myCI = new CultureInfo("pt-BR");
            Calendar myCal = myCI.Calendar;

            // Gets the DTFI properties required by GetWeekOfYear.
            CalendarWeekRule myCWR = myCI.DateTimeFormat.CalendarWeekRule;
            DayOfWeek primeiro_dia_semana = myCI.DateTimeFormat.FirstDayOfWeek;
            var retorno = myCal.GetWeekOfYear(DateTime.Now, myCWR, primeiro_dia_semana);
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
        public string GetDiaDaSemana(DateTime data)
        {
            CultureInfo culture = new CultureInfo("pt-BR");
            DateTimeFormatInfo dtfi = culture.DateTimeFormat;
            string data1 = dtfi.GetDayName(data.DayOfWeek);
            return data1;
        }
        private int _ano { get; set; }= 01;
        public int ano
        {
            get
            {
                return _ano;
            }
            set
            {
                _ano = value;
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
                if(value>0 && value<13)
                {
                    _mes = value;
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
                }
            }
        }
        public int semana { get; set; } = 0;
        public string dia_da_semana { get; set; } = "";
        public Data()
        {

        }
        public Data(DateTime data)
        {
            if(data!=null)
            {
            SetData(data);
            }
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
            this.semana = GetSemana(data);
            this.dia_da_semana = GetDiaDaSemana(data);
        }
    }
    [Serializable]
    public enum Tipo
    {
        Previsto,
        Uso_Recurso,
        Avanco_Etapa,
    }

    [Serializable]
    public class Recurso 
    {
        public string chave
        {
            get
            {
                return this.descricao;
            }
        }
        [XmlIgnore]
        public double realizado { get; set; } = 0;
        [XmlIgnore]
        public Apontamento ultimo { get; set; } = new Apontamento();
        [XmlIgnore]
        public List<Apontamento> apontamentos { get; set; } = new List<Apontamento>();
        public List<Apontamento> GetApontamentos(Apontamentos lista = null)
        {
            if (lista == null)
            {
                lista = lob.Getapontamentos();
            }
            if (lista == null) { return new List<Apontamento>(); }
            Setid();
            this.apontamentos = lista.apontamentos.FindAll(x => x.id_pai == this.id).OrderBy(x => x.data.Getdata()).ToList();

            if(this.apontamentos.Count == 0 && this.chave.Length>0)
            {
                this.apontamentos = lista.apontamentos.FindAll(x => x.chave_pai == this.chave);
                for (int i = 0; i < this.apontamentos.Count; i++)
                {
                    this.apontamentos[i].id_pai = this.id;
                }
            }

            if (this.apontamentos.Count > 0)
            {
                this.ultimo = this.apontamentos.Last();
                this.realizado = this.ultimo.valor;
                for (int i = 0; i < this.apontamentos.Count; i++)
                {
                    this.apontamentos[i].chave_pai = this.chave;
                }
            }
            return apontamentos;
        }

        [XmlIgnore]
        public Linha_de_Balanco lob { get; set; } = new Linha_de_Balanco();
        private void Setid()
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
        }
        public void AddApontamento(Data data, double valor, Apontamentos lista = null)
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
            lista.Add(this, valor, data);
        }
        public Data inicio
        {
            get
            {
                var dts = previsto.FindAll(x => x.data.Getdata() > new DateTime(2001, 01, 01));
                if(dts.Count>0)
                {
                    return new Data(dts.Min(x => x.data.Getdata()));
                }
                return new Data(new DateTime(2001, 01, 01));
            }
        }
        public Data fim
        {
            get
            {
                var dts = previsto.FindAll(x => x.data.Getdata() > new DateTime(2001, 01, 01));
                if (dts.Count > 0)
                {
                    return new Data(dts.Max(x => x.data.Getdata()));
                }
                return new Data(new DateTime(2001, 01, 01));
            }
        }
        public string id { get; set; } = "";
        public string descricao { get; set; } = "";
        public double custo_mensal { get; set; } = 0;
        public double diaria_util { get; set; } = 0;
        public override string ToString()
        {
            return this.descricao;
        }
        public List<Apontamento> previsto { get; set; } = new List<Apontamento>();


        public Recurso()
        {

        }
    }

   [Serializable]
   public class Fase : INotifyPropertyChanged
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
        public double realizado
        {
            get
            {
                if(fases.Count>0)
                {
                    return fases.Sum(x => x.realizado * x.peso_fase/100);
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
        public Apontamento ultimo { get; set; } = new Apontamento();
        [XmlIgnore]
        [Browsable(false)]
        public Linha_de_Balanco lob { get; set; } = new Linha_de_Balanco();
        [XmlIgnore]
        [Browsable(false)]
        public List<Apontamento> apontamentos { get; set; } = new List<Apontamento>();
        public List<Apontamento> UpdateApontamentos(Apontamentos lista = null)
        {
            if(this.descricao=="")
            {
                var s = this.fases.Select(x => x.pai).GroupBy(x => x).Select(x=>x.First()).ToList();
                s = s.FindAll(x => x != null).ToList();
                foreach(var p in s)
                {
                    p.UpdateApontamentos();
                }
            }
            if(lista==null)
            {
                lista = lob.Getapontamentos();
            }
            if (lista == null) { return new List<Apontamento>(); }
            Setid();
            this.apontamentos = lista.apontamentos.FindAll(x => x.id_pai == this.id).OrderBy(x=>x.data.Getdata()).ToList();

            if (this.apontamentos.Count == 0 && this.chave.Length > 0)
            {
                this.apontamentos = lista.apontamentos.FindAll(x => x.chave_pai == this.chave);
                for (int i = 0; i < this.apontamentos.Count; i++)
                {
                    this.apontamentos[i].id_pai = this.id;

                }
            }

            if (this.apontamentos.Count>0)
            {
                this.ultimo = this.apontamentos.Last();
                this.realizado = this.ultimo.valor;
                for (int i = 0; i < this.apontamentos.Count; i++)
                {
                    this.apontamentos[i].chave_pai = this.chave;
                }
            }
            for (int i = 0; i < fases.Count; i++)
            {
                fases[i].UpdateApontamentos(lista);
            }
            return apontamentos;
        }
        public void Setid()
        {
            if (this.id == "")
            {
                this.id = Conexoes.Utilz.RandomString(Vars.CompRandom);
            }
        }

        public string chave { get
            {
                return (this.fases.Count==0?this.cod + " - ":"") + this.descricao;
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
        }
        public void AddApontamento(Data data, double valor, Apontamentos lista=null)
        {
            if(lista==null)
            {
                lista = this.lob.Getapontamentos();
            }
            if(lista==null)
            {
                return;
            }
            Setid();
            lista.Add(this, valor, data);
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

            var s = UpdateApontamentos();
            double valor_atual = 0;
            if(s.Count>0)
            {
                valor_atual = s.Max(x => x.valor);
            }

            Setid();
            lista.Add(this, valor +valor_atual, data);
        }
        [Browsable(false)]
        public string id { get; set; } = "";
        [Browsable(false)]
        public double previsto
        {
            get
            {
                if(DateTime.Now>fim.Getdata())
                {
                    return 100;
                }
                else
                {
                    var dd = (DateTime.Now - inicio.Getdata()).Days;
                    return dd / dias * 100;
                }

            }
            set
            {
                var ss = value;
            }
        }
        [Browsable(false)]
        public int dias
        {
            get
            {
                var dias = (fim.Getdata() - inicio.Getdata()).Days;
                if(dias>0)
                {
                    return dias;
                }
                return 0;
            }
        }
        [Browsable(false)]
        public Data inicio { get; set; } = new Data();
        [Browsable(false)]
        public Data fim { get; set; } = new Data();

        public string descricao { get; set; } = "";
        public void SetInicios()
        {
            if(fases.Count>0)
            {
                var ini  = fases.FindAll(x=>x.inicio.ano>2001);
                var ff = fases.FindAll(x=>x.fim.ano>2001);

                if(ini.Count>0)
                {
                    this.inicio = new Data(ini.Min(x => x.inicio.Getdata()));
                }

                if(ff.Count>0)
                {
                    this.fim = new Data(ff.Max(x => x.fim.Getdata()));
                }
            }
        }
        public override string ToString()
        {
            return ((this.cod != "" && this.fases.Count==0)?this.cod + " - ":"") + this.descricao;
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
                return this._peso_fase;
            }
            set
            {
                if(value>0 && value<=100)
                {
                    this._peso_fase = value;
                    NotifyPropertyChanged();
                }

            }
        }
        private double _peso_fase { get; set; } = 0;
        [DisplayName("Montador")]
        public string montador
        {
            get
            {
                return _montador;
               
            }
            set
            {
                _montador = value;
                NotifyPropertyChanged();
            }
        }
        private string _montador { get; set; } = "";
        [DisplayName("Efetivo")]
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
        public Fase()
        {

        }
    }

    [Serializable]
    public class Apontamento
    {
        public string id_pai { get; set; } = "";
        public string chave_pai { get; set; } = "";
        public override string ToString()
        {
            return this.data.ToString() + " - " + this.tipo.ToString() + " - " + this.valor;
        }
        public Data data { get; set; } = new Data();
        public Tipo tipo { get; set; } = Tipo.Previsto;
        public double valor { get; set; } = 0;
        public double efetivo { get; set; } = 0;
        public Apontamento()
        {

        }

        public Apontamento(Data data, double valor, Tipo tipo = Tipo.Previsto)
        {
            this.data = data;
            this.tipo = tipo;
            this.valor = valor;
        }
    }

    [Serializable]
    public class Apontamentos
    {
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

        public void Add(Fase fase, double valor, Data data)
        {
            Apontamento pp = new Apontamento(data, valor, Tipo.Avanco_Etapa);

            if(fase.id=="")
            {
                fase.id = Conexoes.Utilz.RandomString(Vars.CompRandom);
            }
            pp.id_pai = fase.id;
            pp.chave_pai = fase.chave;
            this.apontamentos.Add(pp);
        }
        public void Add(Recurso fase, double valor, Data data)
        {
            Apontamento pp = new Apontamento(data, valor, Tipo.Uso_Recurso);

            if (fase.id == "")
            {
                fase.id = Conexoes.Utilz.RandomString(Vars.CompRandom);
                pp.chave_pai = fase.chave;
            }
            pp.id_pai = fase.id;
            this.apontamentos.Add(pp);
        }

    }

}
