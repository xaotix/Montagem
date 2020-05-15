using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Windows.Media;

namespace GCM_Offline
{
    [Serializable]
    public class Obra : INotifyPropertyChanged
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
        public Linha_de_Balanco getLOB()
        {
            if (!Directory.Exists(diretorio))
            {
                return new Linha_de_Balanco();
            }
            return new Linha_de_Balanco().Carregar(diretorio);
        }
        public override string ToString()
        {
            return this.contrato + " - " + this.nome_obra;
        }
        [Browsable(false)]
        [XmlIgnore]
        public string nomearq { get; set; } = "obra.cfg";
        public void Salvar(string pasta = null)
        {
            if(pasta == null)
            {
                pasta = this.diretorio;
            }
            if (Directory.Exists(pasta))
            {
                if (!pasta.EndsWith(@"\"))
                {
                    pasta = pasta + @"\";
                }
                var arq = pasta + "obra.cfg";
                var ss = Conexoes.Utilz.RetornarSerializado<Obra>(this);
                Conexoes.Utilz.GravarArquivo(arq, new List<string> { ss });
            }
        }
        [ReadOnly(true)]
        [Category("Obra")]
        [DisplayName("Pedido")]
        public string contrato
        {
            get
            {
                
                return _contrato;
            }
            set
            {
                _contrato = value;
                NotifyPropertyChanged("contrato");
            }
        }
        private string _contrato { get; set; } = "00-000000.P00";
        [Category("Obra")]
        [DisplayName("Nome")]
        public string nome_obra
        {
            get
            {
                return _nome_obra;
            }
            set
            {
                _nome_obra = value;
                NotifyPropertyChanged("nome_obra");
            }
        }
        private string _nome_obra { get; set; } = "Nome da Obra";
        [Category("Obra")]
        [DisplayName("Cliente")]
        public string cliente
        {
            get
            {
                return _cliente;
            }
            set
            {
                _cliente = value;
                NotifyPropertyChanged("cliente");
            }
        }
        private string _cliente { get; set; } = "";
        [Category("Contato Engenharia")]
        [DisplayName("Nome")]
        public string contato_engenharia
        {
            get
            {
                return _contato_engenharia;
            }
            set
            {
                _contato_engenharia = value;
                NotifyPropertyChanged("contato_engenharia");
            }
        }
        private string _contato_engenharia { get; set; } = "";
        [Category("Contato Engenharia")]
        [DisplayName("Telefone")]
        public string contato_engenharia_telefone
        {
            get
            {
                return _contato_engenharia_telefone;
            }
            set
            {
                _contato_engenharia_telefone = value;
                NotifyPropertyChanged("contato_engenharia_telefone");
            }
        }
        private string _contato_engenharia_telefone { get; set; } = "";

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
        [DisplayName("Engenheiro de Obras")]
        public string engenheiro
        {
            get
            {
                return _engenheiro;
            }
            set
            {
                _engenheiro = value;
                NotifyPropertyChanged("engenheiro");
            }
        }
        private string _engenheiro { get; set; } = "";
        public Obra()
        {

        }
        [Browsable(false)]
        public string diretorio { get; set; } = "";
        public Obra(string diretorio)
        {

        }
        public Obra Carregar(string diretorio)
        {
            var arquivo = diretorio + @"\" + nomearq;
            if (File.Exists(arquivo))
            {
                var pp = string.Join("", Conexoes.Utilz.LerArquivo(arquivo, Encoding.GetEncoding(1252)));

                var ps = Conexoes.Utilz.LerSerializado<Obra>(pp);
                ps.diretorio = diretorio;
                return ps;
            }
            return new Obra() { diretorio = diretorio };
        }
    }



    public class Item
    {
        public object objeto { get; set; }
        public string Titulo { get; set; } = "Testes";
        public SolidColorBrush cor { get; set; } = new SolidColorBrush(Colors.Green);
        public TimeSpan Duration { get; set; }
        public DateTime Date { get; set; }
        public DateTime DateFim { get; set; }
        public Item()
        {

        }
    }
}
