
using Conexoes.Macros;
using DB;
using GCM_Offline;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace GCM_Online
{
    public class Contrato
    {
        public Linha_de_Balanco Getlob()
        {
            Linha_de_Balanco retorno = new Linha_de_Balanco();
            retorno.emissao = this.ultima_importacao;
            retorno.gerente = this.gerente;
            retorno.inicio_cronograma = this.inicio;
            retorno.inicio_real = this.inicio;
            retorno.pedido = this.contrato;
            retorno.status = this.status;
           
            foreach(var s in this.GetLancamentos())
            {
                retorno.Getapontamentos().apontamentos.Add(s.GetApontamento());
            }

        
            List<Recurso> recs = new List<Recurso>();
            List<Fase> fases = new List<Fase>();
            var etapas = this.Subfases().Select(x => x.etapa).Distinct().ToList();
            foreach(var et in etapas)
            {
                Fase pp = new Fase();
                pp.lob = retorno;
                pp.descricao = et;
                foreach (var p in this.Subfases().FindAll(x=>x.etapa == et))
                {
                    pp.fases.Add(p.GetFase(retorno));
                }
                retorno.fases.Add(pp);
            }

            foreach(var p in this.GetTodosRecursos())
            {
                recs.Add(p.GetRecurso(retorno));
            }

         

            retorno.recursos__previstos = recs.FindAll(x => x.tipo == Tipo_Recurso.Recurso);
            retorno.recursos_custo = recs.FindAll(x => x.tipo == Tipo_Recurso.Custo);
            retorno.improdutividade = recs.FindAll(x => x.tipo == Tipo_Recurso.Improdutividade);
            retorno.supervisor = recs.FindAll(x => x.tipo == Tipo_Recurso.Supervisor);

            retorno.Ajustes();

            return retorno;
        }
        private List<ApontamentoOnline> _Lancamento { get; set; }
        public List<ApontamentoOnline> GetLancamentos(bool update = false)
        {
            if(_Lancamento==null | update)
            {
                _Lancamento = dbase.GetApontamentosPorPai(-1, this.id);
            }
            return _Lancamento;
        }
        public GCM_Offline.Status_Montagem status { get; set; } = Status_Montagem.EM_ANDAMENTO;
        [Browsable(false)]
        public Data inicio
        {
            get
            {
                if(this.GetSubEtapas().Count>0)
                {
                    return new Data(this.GetSubEtapas().Min(x => x.inicio.Getdata()));
                }
                return new Data();
            }
        }
        [Browsable(false)]
        public Data fim
        {
            get
            {
                if (this.GetSubEtapas().Count > 0)
                {
                    return new Data(this.GetSubEtapas().Max(x => x.fim.Getdata()));
                }
                return new Data();
            }
        }
        public void ImportarLob(GCM_Offline.Linha_de_Balanco lob)
        {
            List<FaseOnline> subs = new List<FaseOnline>();
            List<RecursoOnline> eft = new List<RecursoOnline>();
            Conexoes.Wait w = new Conexoes.Wait(lob.Subfases().Count + lob.GetTodosRecursos().Count + 10,"Salvando avanço...");
            w.Show();
            w.somaProgresso();
            dbase.ApagarEtapas(this);
            foreach (var p in lob.Subfases())
            {
                var sub = new FaseOnline(p, this.id);
                sub.Salvar();
                if (sub.id > 0)
                {
                  var aponts =  p.GetApontamentos();
                    foreach (var ap in p.GetApontamentos())
                    {
                        var lanc = new ApontamentoOnline(ap, this.id, sub.id);
                        lanc.Salvar();
                    }
                }
                else
                {
                    //não deveria vir aqui
                }
                subs.Add(sub);
                w.somaProgresso();
            }
            foreach (var p in lob.GetTodosRecursos())
            {
                var sub = new RecursoOnline(p, this.id);
                var igual = this.GetTodosRecursos().Find(x => x.id_excel == sub.id_excel | (x.descricao == sub.descricao && x.tipo == sub.tipo));
                if (igual != null)
                {
                    sub.id = igual.id;
                }
                sub.Salvar();
                if (sub.id > 0)
                {
                    foreach (var ap in p.GetApontamentos().FindAll(x=>x.valor>0 | x.efetivo>0))
                    {
                        var lanc = new ApontamentoOnline(ap, this.id, sub.id);

                        lanc.Salvar();


                    }
                }
                else
                {

                }
                eft.Add(sub);
                w.somaProgresso();

            }



            this.ultima_importacao = new Data(DateTime.Now);
            this.Salvar();
            w.somaProgresso();
            w.Close();
        }
        public override string ToString()
        {
            return this.contrato + " - " + this.descricao;
        }
        [Browsable(false)]
        public Data ultima_importacao { get; set; } = new Data();
        [Browsable(false)]
        public int id { get; set; } = 0;
        [DisplayName("Pedido")]
        public string contrato { get; set; } = "";
        [DisplayName("Descrição")]
        public string descricao { get; set; } = "";
        [DisplayName("Engenheiro de Obras")]
        public string engenheiro { get; set; } = "";
        [DisplayName("Gerente de Montagem")]
        public string gerente { get; set; } = "";
        [DisplayName("Área")]
        public double area { get; set; } = 0;
        private List<FaseOnline> _subetapas { get; set; }
        public List<FaseOnline> Subfases()
        {
            return this.GetSubEtapas();
        }
        [Browsable(false)]
        public List<FaseOnline> GetSubEtapas(bool atualizar = false)
        {
            if(_subetapas==null | atualizar)
            {
                _subetapas = new List<FaseOnline>();
                var ss = Conexoes.DBases.BancoRM.DB.Consulta("select * from " + Vars.db + "." + Vars.tb_subetapas + " as pr where pr.id_obra='" + this.id + "'");
                foreach(var s in ss.Linhas)
                {
                    _subetapas.Add(new FaseOnline(s));
                }
            }
            return _subetapas;
        }
        private List<RecursoOnline> _efetivos { get; set; }
        [Browsable(false)]
        public List<RecursoOnline> GetTodosRecursos(bool atualizar = false)
        {
            if (_efetivos == null | atualizar)
            {
                _efetivos = new List<RecursoOnline>();
                var ss = Conexoes.DBases.BancoRM.DB.Consulta("select * from " + Vars.db + "." + Vars.tb_efetivos + " as pr where pr.id_obra='" + this.id + "'");
                foreach (var s in ss.Linhas)
                {
                    _efetivos.Add(new RecursoOnline(s));
                }
            }
            return _efetivos;
        }
        public Contrato(DB.Linha s)
        {
            this.id = s.Get("id").Int;
            this.contrato = s.Get("contrato").ToString();
            this.descricao = s.Get("descricao").ToString();
            this.engenheiro = s.Get("engenheiro").ToString();
            this.gerente = s.Get("gerente").ToString();
            this.area = s.Get("area").Double();
            this.status = Conexoes.Utilz.StringParaEnum<GCM_Offline.Status_Montagem>(s.Get("status").ToString());
            this.ultima_importacao = new Data(s.Get("ultima_importacao").Data);
        }
        public Contrato()
        {

        }
        public Linha GetL()
        {
            Linha l = new Linha();
            l.Add("contrato", contrato);
            l.Add("descricao", descricao);
            l.Add("engenheiro", engenheiro);
            l.Add("gerente", gerente);
            l.Add("status", status.ToString());
            l.Add("area", area);
            l.Add("ultima_importacao", ultima_importacao.ToString());

            return l;
        }
        public void Salvar()
        {
            if(this.id==0)
            {
                this.id = (int)Conexoes.DBases.BancoRM.DB.Cadastro(GetL().Celulas, GCM_Online.Vars.db,GCM_Online.Vars.tb_obras);
            }
            else
            {
            Conexoes.DBases.BancoRM.DB.Update(new List<DB.Celula> { new DB.Celula("id", id) }, GetL().Celulas, GCM_Online.Vars.db, GCM_Online.Vars.tb_obras);
            }
        }
    }
    public class FaseOnline
    {
        public Fase GetFase(Linha_de_Balanco lob)
        {
            Fase r = new Fase();
            r.area = this.area;
            r.cod = this.cod;
            r.descricao = this.descricao;
            r.efetivo = this.efetivo;
            r.equipe = this.equipe;
            r.fim = new Data(this.fim);
            r.id = this.id_excel;
            r.inicio = new Data(this.inicio);
            r.pep = this.pep;
            r.peso_fase = this.peso_fase;
            r.lob = lob;
            return r;
        }
        public override string ToString()
        {
            return (this.pep!=""?this.pep:this.cod) + " - " + this.descricao;
        }
        public List<ApontamentoOnline> GetLancamentos(bool update = false)
        {
            if(_lancamentos==null | update)
            {
                _lancamentos = new List<ApontamentoOnline>();
                _lancamentos.AddRange(dbase.GetApontamentosPorPai(this.id,-1, GCM_Offline.Tipo.Avanco_Etapa.ToString()));
            }
            return _lancamentos;
        }
        private List<ApontamentoOnline> _lancamentos { get; set; }
        public Linha GetL()
        {
            Linha l = new Linha();
            l.Add("id_obra", id_obra);
            l.Add("id_excel", id_excel);
            l.Add("pep", pep);
            l.Add("area", area);
            l.Add("cod", cod);
            l.Add("efetivo", efetivo);
            l.Add("descricao", descricao);
            l.Add("equipe", equipe);
            l.Add("etapa", etapa);
            l.Add("peso_fase", peso_fase);
            l.Add("inicio", inicio.ToString());
            l.Add("fim", fim.ToString());

            return l;
        }
        public void Salvar()
        {
            if (this.id == 0)
            {
                this.id = (int)Conexoes.DBases.BancoRM.DB.Cadastro(GetL().Celulas, GCM_Online.Vars.db, GCM_Online.Vars.tb_subetapas);
            }
            else if(this.id_obra>0)
            {
                Conexoes.DBases.BancoRM.DB.Update(new List<DB.Celula> { new DB.Celula("id", id) }, GetL().Celulas, GCM_Online.Vars.db, GCM_Online.Vars.tb_subetapas);
            }
        }
        public int id { get; set; } = 0;
        public int id_obra { get; set; } = 0;
        public string id_excel { get; set; } = "";
        public string pep { get; set; } = "";
        public double area { get; set; } = 0;
        public string cod { get; set; } = "";
        public string equipe { get; set; } = "";
        public double efetivo { get; set; } = 0;
        public double peso_fase { get; set; } = 0;
        public Data inicio { get; set; } = new Data();
        public Data fim { get; set; } = new Data();
        public string descricao { get; set; } = "";
        public string etapa { get; set; } = "";
        public FaseOnline(Linha s)
        {
            this.id = s.Get("id").Int;
            this.id_obra = s.Get("id_obra").Int;
            this.id_excel = s.Get("id_excel").ToString();
            this.pep = s.Get("pep").ToString();
            this.area = s.Get("area").Double();
            this.cod = s.Get("cod").ToString();
            this.etapa = s.Get("etapa").ToString();
            this.peso_fase = s.Get("peso_fase").Double();
            this.efetivo = s.Get("efetivo").Double();
            this.descricao = s.Get("descricao").ToString();
            this.equipe = s.Get("equipe").ToString();
            this.inicio = new Data(s.Get("inicio").ToString());
            this.fim = new Data(s.Get("fim").ToString());
        }
        public FaseOnline(Fase p, int id_obra)
        {
            this.area = p.area;
            this.cod = p.cod;
            this.descricao = p.descricao;
            this.efetivo = p.efetivo;
            
            this.id_excel = p.id;
            this.id_obra = id_obra;
            this.inicio.SetData(p.inicio);
            this.fim.SetData(p.fim);
            this.pep = p.pep;
            this.peso_fase = p.peso_fase;
            this.etapa = p.pai.descricao;
            this.equipe = p.equipe;
        }
    }
    public class RecursoOnline
    {
        public Recurso GetRecurso(Linha_de_Balanco lob)
        {
            Recurso r = new Recurso();
            r.cargo = this.cargo;
            r.custo_mensal = this.custo_mensal;
            r.descricao = this.descricao;
            r.diaria_util = this.diaria_util;
            r.equipe = this.equipe;
            r.id = this.id_excel;
            r.lob = lob;
            r.motivo = this.motivo;
            r.supervisor = this.supervisor;
            r.valor_previsto_importado = this.valor_previsto_importado;
            r.tipo = this.tipo;


            return r;
        }
        public override string ToString()
        {
            return this.descricao;
        }
        public List<ApontamentoOnline> GetLancamentosRealizados(bool update = false)
        {
            if (_lancamentos == null | update)
            {
                _lancamentos = new List<ApontamentoOnline>();
                _lancamentos.AddRange(dbase.GetApontamentosPorPai(this.id,-1, GCM_Offline.Tipo.Equipamento.ToString()));
            }
            return _lancamentos;
        }
        private List<ApontamentoOnline> _lancamentos { get; set; }
        public Linha GetL()
        {
            Linha l = new Linha();
            l.Add("id_obra", id_obra);
            l.Add("id_excel", id_excel);
            l.Add("tipo", tipo.ToString());
            l.Add("equipe", equipe);
            l.Add("supervisor", supervisor);
            l.Add("motivo", motivo);
            l.Add("cargo", cargo);
            l.Add("descricao", descricao);
            l.Add("custo_mensal", custo_mensal);
            l.Add("diaria_util", diaria_util);
            l.Add("valor_previsto_importado", valor_previsto_importado);
            l.Add("inicio", inicio.Getdata());
            l.Add("fim", fim.Getdata());
            return l;
        }
        public void Salvar()
        {
            if (this.id == 0)
            {
                this.id = (int)Conexoes.DBases.BancoRM.DB.Cadastro(GetL().Celulas, GCM_Online.Vars.db, GCM_Online.Vars.tb_efetivos);
            }
            else if (this.id_obra > 0)
            {
                Conexoes.DBases.BancoRM.DB.Update(new List<DB.Celula> { new DB.Celula("id", id) }, GetL().Celulas, GCM_Online.Vars.db, GCM_Online.Vars.tb_efetivos);
            }
        }
        public int id { get; set; } = 0;
        public int id_obra { get; set; } = 0;
        public string id_excel { get; set; } = "";
        public Tipo_Recurso tipo { get; set; } = Tipo_Recurso.Recurso;
        public string equipe { get; set; } = "";
        public string supervisor { get; set; } = "";
        public string motivo { get; set; } = "";
        public string cargo { get; set; } = "";
        public string descricao { get; set; } = "";
        public double custo_mensal { get; set; } = 0;
        public double diaria_util { get; set; } = 0;
        public double valor_previsto_importado { get; set; } = 0;
        public Data inicio { get; set; } = new Data();
        public Data fim { get; set; } = new Data();
        public RecursoOnline(Linha s)
        {
            this.id = s.Get("id").Int;
            this.id_obra = s.Get("id_obra").Int;
            this.id_excel = s.Get("id_excel").ToString();
            this.tipo = Conexoes.Utilz.StringParaEnum<Tipo_Recurso>(s.Get("tipo").ToString());
            this.equipe = s.Get("equipe").ToString();
            this.supervisor = s.Get("supervisor").ToString();
            this.motivo = s.Get("motivo").ToString();
            this.cargo = s.Get("cargo").ToString();
            this.custo_mensal = s.Get("custo_mensal").Double();
            this.descricao = s.Get("descricao").ToString();
            this.diaria_util = s.Get("diaria_util").Double();
            this.valor_previsto_importado = s.Get("valor_previsto_importado").Double();
            this.inicio= new Data(s.Get("inicio").ToString());
            this.fim = new Data(s.Get("fim").ToString());
        }
        public RecursoOnline(Recurso p, int id_obra)
        {
            this.cargo = p.cargo;
            this.custo_mensal = p.custo_mensal;
            this.descricao = p.descricao;
            this.diaria_util = p.diaria_util;
            this.equipe = p.equipe;
            this.id_excel = p.id;
            this.id_obra = id_obra;
            this.motivo = p.motivo;
            this.supervisor = p.supervisor;
            this.tipo = p.tipo;
            this.valor_previsto_importado = p.valor_previsto_importado;
            this.inicio.SetData(p.inicio);
            this.fim.SetData(p.fim);
        }
    }
    public class ApontamentoOnline
    {
        public Apontamento GetApontamento()
        {
            Apontamento pp = new Apontamento();
            pp.chave_pai = this.chave_pai;
            pp.data = new Data(this.data);
            pp.descricao = this.descricao;
            pp.efetivo = this.efetivo;
            pp.id_pai = this.id_pai_excel;
            pp.responsavel = this.responsavel;
            pp.tipo = this.tipo;
            pp.valor = this.valor;
            return pp;
        }
        public override string ToString()
        {
            return this.data.ToString() + " - " + this.valor;
        }
        public Linha GetL()
        {
            Linha l = new Linha();
            l.Add("id_obra", id_obra);
            l.Add("descricao", descricao);
            l.Add("responsavel", responsavel);
            l.Add("id_pai", id_pai);
            l.Add("id_pai_excel", id_pai_excel);
            l.Add("chave_pai", chave_pai);
            l.Add("data", data.ToString());
            l.Add("tipo", tipo.ToString());
            l.Add("valor", valor);
            l.Add("efetivo", efetivo);

            return l;
        }
        public void Salvar()
        {
            if (this.id == 0)
            {
                this.id = (int)Conexoes.DBases.BancoRM.DB.Cadastro(GetL().Celulas, GCM_Online.Vars.db, GCM_Online.Vars.tb_lancamentos);
            }
            else if (this.id_obra > 0)
            {
                Conexoes.DBases.BancoRM.DB.Update(new List<DB.Celula> { new DB.Celula("id", id) }, GetL().Celulas, GCM_Online.Vars.db, GCM_Online.Vars.tb_lancamentos);
            }
        }
        public int id { get; set; } = 0;
        public int id_obra { get; set; } = 0;
        public int id_pai { get; set; } = 0;
        public string id_pai_excel { get; set; } = "";
        public string descricao { get; set; } = "";
        public string responsavel { get; set; } = "";
        public string chave_pai { get; set; } = "";
        public Data data { get; set; } = new Data();
        public Tipo tipo { get; set; } =  Tipo.Avanco_Etapa;
        public double valor { get; set; } = 0;
        public double efetivo { get; set; } = 0;
        public ApontamentoOnline(Linha p)
        {
            this.id = p.Get("id").Int;
            this.id_obra = p.Get("id_obra").Int;
            this.id_pai = p.Get("id_pai").Int;
            this.id_pai_excel = p.Get("id_pai_excel").ToString();
            this.descricao = p.Get("descricao").ToString();
            this.responsavel = p.Get("responsavel").ToString();
            this.chave_pai = p.Get("chave_pai").ToString();
            this.data = new Data(p.Get("data").Data);
            this.tipo = Conexoes.Utilz.StringParaEnum<Tipo>(p.Get("tipo").ToString());
            this.valor = p.Get("valor").Double();
            this.efetivo = p.Get("efetivo").Double();
        }
        public ApontamentoOnline(Apontamento p, int id_obra, int id_pai)
        {
            this.chave_pai = p.chave_pai;
            this.data = p.data;
            this.descricao = p.descricao;
            this.efetivo = p.efetivo;
            this.id_obra = id_obra;
            this.id_pai = id_pai;
            this.id_pai_excel = p.id_pai;
            this.responsavel = p.responsavel;
            this.tipo = p.tipo;
            this.valor = p.valor;

        }

    }
    public static class Vars
    {
        public static string db { get; set; } = "painel_de_obras2";
        public static string tb_subetapas { get; set; } = "gcm_subetapas";
        public static string tb_efetivos { get; set; } = "gcm_efetivos";
        public static string template_resumo
        {
            get
            {
                return System.Windows.Forms.Application.StartupPath + @"\template_resumo_obras.xlsx";
            }
        }
        public static string tb_lancamentos { get; set; } = "gcm_lancamentos";
        public static string tb_obras { get; set; } = "gcm_obras";
    }
    public static class dbase
    {
        public static List<ApontamentoOnline> GetApontamentosPorPai(int id_pai = -1, int id_obra = -1, string tipo = "")
        {
            var comando = "select * from " + Vars.db + "." + Vars.tb_lancamentos + " as pr ";
            int s = 0;

            if(id_obra> 0)
            {
                if (s == 0)
                {
                    comando = comando + " where ";

                }
                else if(s>1)
                {
                    comando = comando + " and ";
                }
                comando = comando + "pr.id_obra = '" + id_obra + "'";
                s++;
            }
            if (id_pai > 0)
            {
                if (s == 0)
                {
                    comando = comando + " where ";
                   
                }
                else  if (s > 1)
                {
                    comando = comando + " and ";
                }

                comando = comando + "pr.id_pai = '" + id_pai + "'";
                s++;
            }
            if (tipo !="")
            {
                if (s == 0)
                {
                    comando = comando + " where ";

                }
                else if (s > 1)
                {
                    comando = comando + " and ";
                }

                comando = comando + "pr.tipo = '" + tipo + "'";
                s++;
            }

            List<ApontamentoOnline> retorno = new List<ApontamentoOnline>();
            var lis = Conexoes.DBases.BancoRM.DB.Consulta(comando);
            foreach(var p in lis.Linhas)
            {
                retorno.Add(new ApontamentoOnline(p));
            }
            return retorno;
        }
        public static void Apagar(Contrato ob)
        {
            Conexoes.DBases.BancoRM.DB.Apagar(new List<Celula> { new Celula("id", ob.id) }, Vars.db, Vars.tb_obras);

            Conexoes.DBases.BancoRM.DB.Apagar(new List<Celula> { new Celula("id_obra", ob.id) }, Vars.db, Vars.tb_efetivos);
            Conexoes.DBases.BancoRM.DB.Apagar(new List<Celula> { new Celula("id_obra", ob.id) }, Vars.db, Vars.tb_lancamentos);
            Conexoes.DBases.BancoRM.DB.Apagar(new List<Celula> { new Celula("id_obra", ob.id) }, Vars.db, Vars.tb_subetapas);
        }
        public static void Apagar(FaseOnline ob)
        {
            Conexoes.DBases.BancoRM.DB.Apagar(new List<Celula> { new Celula("id", ob.id) }, Vars.db, Vars.tb_subetapas);
            Conexoes.DBases.BancoRM.DB.Apagar(new List<Celula> { new Celula("id_pai", ob.id) }, Vars.db, Vars.tb_lancamentos);
        }
        public static void Apagar(ApontamentoOnline ob)
        {
            Conexoes.DBases.BancoRM.DB.Apagar(new List<Celula> { new Celula("id", ob.id) }, Vars.db, Vars.tb_lancamentos);
        }
        public static void Apagar(RecursoOnline ob)
        {
            Conexoes.DBases.BancoRM.DB.Apagar(new List<Celula> { new Celula("id", ob.id) }, Vars.db, Vars.tb_efetivos);
            Conexoes.DBases.BancoRM.DB.Apagar(new List<Celula> { new Celula("id_pai", ob.id) }, Vars.db, Vars.tb_lancamentos);
        }
        public static void ApagarLancamentos(Contrato ob)
        {
            Conexoes.DBases.BancoRM.DB.Apagar(new List<Celula> { new Celula("id_obra", ob.id) }, Vars.db, Vars.tb_lancamentos);
        }
        public static void ApagarEtapas(Contrato ob)
        {
            Conexoes.DBases.BancoRM.DB.Apagar(new List<Celula> { new Celula("id_obra", ob.id) }, Vars.db, Vars.tb_subetapas);
            Conexoes.DBases.BancoRM.DB.Apagar(new List<Celula> { new Celula("id_obra", ob.id) }, Vars.db, Vars.tb_lancamentos);
        }
        public static List<Contrato> obras(bool update = false)
        {
            if(_obras == null | update)
            {
                _obras = new List<Contrato>();
                var obs = Conexoes.DBases.BancoRM.DB.Consulta("select * from " + Vars.db + "."  + Vars.tb_obras);
                foreach(var s in obs.Linhas)
                {
                    _obras.Add(new Contrato(s));
                }
            }
            return _obras;

        }
        private  static List<Contrato> _obras { get; set; }
    }

}
