using Biblioteca_Daniel;
using Conexoes;
using mshtml;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telerik.Windows.Core;

namespace GCM_Offline
{
   public class Excel
    {

        public static Linha_de_Balanco ImportarLOB(string path, string pedido, Linha_de_Balanco atual, out bool status)
        {
            denovo:
            status = false;
            List<string> erros = new List<string>();
            if (!File.Exists(path))
            {
                MessageBox.Show("Arquivo não existe " + path);
                return new Linha_de_Balanco() { arquivoexcel = path, msgerro = "Arquivo não existe" };
            }
            Linha_de_Balanco retorno = new Linha_de_Balanco();
            retorno.pedido = pedido;
            retorno.recursos__previstos = retorno.GetRecursosPadrao();
            retorno.arquivoexcel = path;
            var linhas = 0;
            var colunas = 0;
            int l0 = 15;
            int c0 = 1;
            try
            {
                using (var pck = new OfficeOpenXml.ExcelPackage())
                {
                    Conexoes.Wait w = new Conexoes.Wait(10, "Carregando planilha...");
                    w.Show();
                    w.somaProgresso();
                    //using (var stream = File.OpenRead(path))
                    using (Stream stream = new FileStream(path,
                                 FileMode.Open,
                                 FileAccess.Read,
                                 FileShare.ReadWrite))
                    {
                        pck.Load(stream);
                    }
                    var ws = pck.Workbook.Worksheets.ToList();

                    var lob = ws.Find(x => x.Name.ToUpper() == "LOB");

                    if (lob == null)
                    {
                        w.Close();
                        MessageBox.Show("Aba com o nome 'LOB' não encontrada.");
                        return new Linha_de_Balanco() { arquivoexcel = path };
                    }

                    var wbase = ws.Find(x => x.Name.ToUpper().Replace(" ", "") == "BASE");
                    if (wbase == null)
                    {
                        w.Close();
                        MessageBox.Show("Aba com o nome 'BASE' não encontrada.");
                        return new Linha_de_Balanco() { arquivoexcel = path };
                    }

                    w.somaProgresso();

                    if (lob != null)
                    {


                        var l_data = 15;
                        var col_datas = 17;
                        if (lob.Dimension != null)
                        {
                            linhas = lob.Dimension.End.Row;
                            w.SetProgresso(1, linhas, "Carregando...");
                            colunas = 11;
                        }




                        List<Fase> fases = new List<Fase>();
                        for (int L = l0; L <= linhas; L++)
                        {
                            try
                            {
                                w.somaProgresso();
                                var wlinha = lob.Cells[L, 2, L, colunas];
                                string wlinhastr = getlinhastr(wlinha);


                                string col_J, col_B, col_N, col_D, col_E;
                                getchaves(lob, L, out col_J, out col_B, out col_N, out col_D, out col_E);

                                var min = new DateTime(2001, 01, 01);

                                if (col_J.ToUpper().Contains("ETAPA") && col_B != "")
                                {
                                    Fase p = new Fase();
                                    p.area = GetValor<double>(lob.Cells["H" + L]);
                                    p.cod = GetValor<string>(lob.Cells["C" + L]);
                                    p.inicio = new Data(GetValor<string>(lob.Cells["D" + L]));
                                    p.fim = new Data(GetValor<string>(lob.Cells["E" + L]));
                                    p.descricao = GetValor<string>(lob.Cells["B" + L]);
                                    var TTTT = p.descricao.Replace(" ", "_").Split('_');
                                    var etapastr = "";
                                    if (TTTT.Count() > 1 && TTTT[0].ToUpper().Contains("ETAPA"))
                                    {
                                        etapastr = TTTT[1];
                                    }

                                    if (Conexoes.Utilz.ESoNumero(TTTT[0]))
                                    {
                                        etapastr = TTTT[0];
                                    }



                                    if (p.area<=0)
                                    {
                                        erros.Add("Linha " + L + " Valor área é obrigatório. Ajuste.");

                                    }



                                    //pula uma linha
                                    L++;
                                    getchaves(lob, L, out col_J, out col_B, out col_N, out col_D, out col_E);
                                    //procura pelas sub-etapas
                                    while (!col_J.ToUpper().Contains("SUBETAPA") && col_B.Length > 0)
                                    {
                                        if (col_J.ToUpper().Contains("EXISTE") | (new Data(col_D).valido && new Data(col_E).valido))
                                        {
                                            Fase f = new Fase();
                                            f.pai = p;
                                            f.descricao = GetValor<string>(lob.Cells["B" + L]);
                                            f.cod = GetValor<string>(lob.Cells["C" + L]);
                                            f.inicio = new Data(GetValor<string>(lob.Cells["D" + L]));
                                            f.fim = new Data(GetValor<string>(lob.Cells["E" + L]));
                                            f.peso_fase = GetValor<double>(lob.Cells["I" + L]);
                                            f.equipe = GetValor<string>(lob.Cells["N" + L]);
                                            f.pep = retorno.pedido + "." + etapastr.PadRight(3, '0') + "." + f.cod;
                                            f.efetivo = GetValor<double>(lob.Cells["P" + L]);
                                            f.Getid(atual);

                                            //if(f.peso_fase==0 | f.peso_fase>1)
                                            //{
                                            //    erros.Add("Linha " + L +" Células de peso avanço físico  Célula (I" + L + "): Valor zerado ou inválido.");
                                            //}

                                            if (f.inicio.Getdata() > f.fim.Getdata())
                                            {
                                                erros.Add("Linha " + L + " Células de datas  (D" + L + " e ou E" + L + "): data inicial maior que a final.");

                                            }

                                            if (f.cod.Replace(" ", "") == "")
                                            {
                                                erros.Add("Linha " + L + " Coluna [C] CÓD em branco.");
                                            }

                                            if (f.inicio.Getdata() < min)
                                            {
                                                if (p.fases.Count > 0)
                                                {
                                                    f.inicio = new Data(p.fases.Last().inicio.Getdata().AddDays(1));
                                                }
                                            }
                                            if (f.fim.Getdata() < min)
                                            {
                                                f.fim = new Data(f.inicio.Getdata().AddDays(1));
                                            }

                                            if (f.descricao != null && f.cod != null)
                                            {
                                                p.fases.Add(f);
                                            }
                                        }
                                        else if ((new Data(col_D).valido | new Data(col_E).valido))
                                        {
                                            erros.Add("Linha " + L + " Células de datas  (D" + L + " e ou E" + L + "): preenchimento inválido ou em branco. Valor esperado: Data");
                                        }

                                        L++;
                                        getchaves(lob, L, out col_J, out col_B, out col_N, out col_D, out col_E);
                                        wlinhastr = getlinhastr(lob.Cells[L, c0, L, colunas]);
                                    }

                                    if (p.fases.Count > 0)
                                    {
                                        if (!Conexoes.Utilz.ESoNumero(etapastr) | etapastr == "")
                                        {
                                            erros.Add("Célula [" + p.descricao + "] preenchimento inválido. Nome da etapa deve seguir o seguinte padrão: [ETAPA_001_DESCRICAO]");
                                        }
                                        var figuais = p.fases.GroupBy(x => x.cod).ToList().FindAll(x => x.Count() > 1).FindAll(x => x.Key.Replace(" ", "") != "");

                                        erros.AddRange(figuais.Select(x => "Há mais de uma sub-etapa com o nome " + x.Key + " Corrija a coluna 'C'"));
                                        erros.AddRange(p.fases.FindAll(x => x.cod.Length != 3).Select(x => "Etapa " + p.descricao + " Campo CÓD [coluna C] inválido deve conter 3 caracteres: " + x.cod));
                                        p.SetInicios();
                                        retorno.fases.Add(p);
                                    }
                                    //else
                                    //{
                                    //    erros.Add(col_J + " Nenhuma sub-etapa válida encontrada.");

                                    //}

                                    //se nao achar mais nenhum valor na coluna J e na coluna B
                                    if (col_J.Length == 0 && col_B.Length == 0)
                                    {
                                        //pula linhas se estão em branco durante 10 tentativas
                                        int ll = 50;
                                        int c = 1;
                                        while (c < ll && col_J.Length == 0 && col_B.Length == 0 && col_N.Length == 0)
                                        {
                                            L++;
                                            c++;
                                            getchaves(lob, L, out col_J, out col_B, out col_N, out col_D, out col_E);
                                        }


                                        if (col_B.Length == 0 && col_J.Length == 0 && col_N.Length == 0)
                                        {
                                            break;
                                        }
                                        else
                                        {
                                            //volta uma linha para que o loop possa continuar pra proxima etapa
                                            L--;
                                        }
                                    }
                                }
                                else if (col_N.ToUpper().Contains("RECURSOS"))
                                {
                                    retorno.Ajustes();
                                    L++;
                                    string titulo = "";
                                    pula_em_branco_recursos(lob, ref L, ref titulo);



                                    while (titulo != "" && titulo != null)
                                    {

                                        pula_em_branco_recursos(lob, ref L, ref titulo);

                                        if (titulo != "" && titulo != null && titulo.ToUpper() != "SEMANA" && !titulo.ToUpper().Replace(" ", "").Contains("MÉDIAEFETIVO"))
                                        {
                                            bool novo = false;
                                            var pp = retorno.recursos__previstos.Find(x => x.descricao.ToUpper().Replace(" ", "").Replace("_", "") == titulo.ToUpper().Replace(" ", "").Replace("_", ""));
                                            if (pp == null)
                                            {
                                                pp = new Recurso();
                                                novo = true;
                                            }
                                            pp.descricao = titulo;



                                            var dts = col_datas;
                                            pp.Getid(atual);

                                            var dt0 = GetValor<string>(lob.Cells[l_data - 1, dts, l_data, dts]);
                                            var data = new Data(dt0);
                                            while (dt0 != "" | dt0 != null && data.Getdata() < retorno.fim.Getdata())
                                            {

                                                dts++;
                                                dt0 = GetValor<string>(lob.Cells[l_data - 1, dts, l_data, dts]);
                                                data = new Data(dt0);
                                                if (dt0 == null | dt0 == "")
                                                {
                                                    int c = 0;
                                                    int max = 50;

                                                    while (c <= max && (dt0 == "" | dt0 == null))
                                                    {
                                                        dts++;
                                                        dt0 = GetValor<string>(lob.Cells[l_data - 1, dts, l_data, dts]);
                                                        c++;
                                                    }


                                                    if (c == max)
                                                    {
                                                        break;
                                                    }

                                                }
                                                else
                                                {
                                                    Apontamento ap = new Apontamento();
                                                    var valor = GetValor<double>(lob.Cells[L, dts]);
                                                    if (valor > 0)
                                                    {
                                                        ap.valor = Conexoes.Utilz.Double(valor);
                                                        ap.data = new Data(dt0);
                                                        if (ap.data.Getdata() <= retorno.fim.Getdata())
                                                        {
                                                            pp.previsto.Add(ap);

                                                        }
                                                        else break;
                                                    }
                                                }
                                            }


                                            if (pp.previsto.Count > 0 && novo)
                                            {

                                                retorno.recursos__previstos.Add(pp);
                                            }
                                        }
                                        L++;
                                        pula_em_branco_recursos(lob, ref L, ref titulo);

                                    }



                                    L++;
                                    //L = GetRecursos(retorno, lob, l_data, col_recursos, col_datas, L);



                                    break;
                                }




                            }
                            catch (Exception ex)
                            {


                            }

                        }


                    }

                    w.somaProgresso();
                    if (wbase != null)
                    {


                        linhas = wbase.Dimension.End.Row;
                        l0 = 2;
                        int max_pular = 50;
                        int pular = 0;
                        //vai tentar mapear o peso das atividades
                        w.SetProgresso(1, linhas, "Carregando dados de recursos...");
                        for (int L = l0; L < linhas; L++)
                        {
                            w.somaProgresso();
                            string desc = "";

                            try
                            {
                                desc = GetValor<string>(wbase.Cells["B" + L]);

                            }
                            catch (Exception)
                            {

                            }
                            if ((desc == "" | desc == null) && pular < max_pular)
                            {
                                pular++;
                                continue;
                            }
                            else if (desc == "" | desc == null)
                            {
                                break;
                            }
                            else
                            {
                                //se encontrou o titulo, vai tentar mapear o valor.
                                try
                                {
                                    var cod = GetValor<string>(wbase.Cells["A" + L]);
                                    var peso = Conexoes.Utilz.Double(GetValor<string>(wbase.Cells["D" + L]).Replace("%", "").Replace(" ", ""));
                                    if (peso > 0 && cod != null)
                                    {
                                        if (cod != "")
                                        {
                                            retorno.fases_pesos_avanco_fisico.Add(new Fase() { cod = cod, peso_fase = peso, descricao = desc });

                                        }
                                    }
                                }
                                catch (Exception)
                                {

                                }

                            }

                        }

                        //mapeia o custo de recursos;
                        l0 = 3;
                        for (int L = l0; L < linhas; L++)
                        {
                            string desc = "";

                            try
                            {
                                desc = GetValor<string>(wbase.Cells["F" + L]);

                            }
                            catch (Exception)
                            {

                            }
                            if ((desc == "" | desc == null) && pular < max_pular)
                            {
                                pular++;
                                continue;
                            }
                            else if (desc == "" | desc == null)
                            {
                                break;
                            }
                            else
                            {
                                //se encontrou o titulo, vai tentar mapear o valor.
                                try
                                {
                                    var custo_mensal = Conexoes.Utilz.Double(GetValor<string>(wbase.Cells["G" + L]));
                                    var diaria_util = Conexoes.Utilz.Double(GetValor<string>(wbase.Cells["H" + L]));
                                    if (custo_mensal > 0 && diaria_util > 0)
                                    {
                                        retorno.recursos_custo.Add(new Recurso() { descricao = desc, custo_mensal = custo_mensal, diaria_util = diaria_util });
                                    }
                                }
                                catch (Exception)
                                {

                                }

                            }

                        }

                    }

                    w.Close();
                }

            }
            catch (Exception ex)
            {
                if (Conexoes.Utilz.Pergunta(ex.Message + "\n\n\n" + ex.StackTrace + "\n\n" + "Tentar novamente?"))
                {
                    goto denovo;
                }
                {
                    status = false;
                    return new Linha_de_Balanco();
                }
            }

            retorno.AjustaPesosEtapas();


            var fases_iguais = retorno.Subfases().GroupBy(x => x.pep).ToList().FindAll(x => x.Count() > 1);

            if(fases_iguais.Count>0)
            {
                erros.AddRange(fases_iguais.Select(x => x.Key + " Etapa repete mais de uma vez. Verifique as colunas 'B'  na linha das etapas e a coluna 'C' nas linhas de sub-etapas. Não pode ter etapas com mesmo número."));
            }

            var peso_fases = retorno.Subfases().Sum(x => x.peso_fase);
            if(peso_fases < 0.9 | peso_fases > 1.1)
            {
                erros.Add("Soma da % do peso de avanço físico (Coluna I) inválido: Deve fechar 1 a soma das etapas válidas. Dalor soma: " + peso_fases);
            }

            if (retorno.fases.Count == 0)
            {
                MessageBox.Show("Nenhuma etapa válida encontrada no arquivo " + path);
            }
            else if (retorno.Subfases().Count == 0)
            {
                MessageBox.Show("Nenhuma sub-etapa válida encontrada. Verifique os preenchimentos.");

            }
            else if (retorno.recursos__previstos.Count == 0)
            {
                MessageBox.Show("Nenhum recurso válido encontrado no arquivo " + path);
            }
            else if(erros.Count>0)
            {
                Conexoes.Utilz.ShowReports(erros.Distinct().ToList().Select(x => new Report("Erro", x)).ToList());
            }
            else
            {
                MessageBox.Show("Linha de Balanço Importada!");
                status = true;

            }
            return retorno;
        }
        public static Linha_de_Balanco ImportarApontamentos(string path, Linha_de_Balanco atual, out bool status)
        {
        retentar:
            bool salvar = false;
            if (!File.Exists(path))
            {
                status = false;
                return new Linha_de_Balanco() { arquivoexcel = path, msgerro = "Arquivo não existe" };
            }
            Linha_de_Balanco r = new Linha_de_Balanco();
            r.arquivoexcel = path;

            Conexoes.Wait w = new Conexoes.Wait(5,"Importando Planilha...");
            w.Show();
            try
            {
                using (var pck = new OfficeOpenXml.ExcelPackage())
                {
                    //using (var stream = File.OpenRead(path))
                    using (Stream stream = new FileStream(path,
                                 FileMode.Open,
                                 FileAccess.Read,
                                 FileShare.ReadWrite))
                    {
                        pck.Load(stream);
                    }
                    var ws = pck.Workbook.Worksheets.ToList();

                    var rel = ws.Find(x => x.Name.ToUpper() == "RELATORIO");
                    var av = ws.Find(x => x.Name.ToUpper() == "AVANÇO");
                    var rec = ws.Find(x => x.Name.ToUpper() == "RECURSOS");
                    var lob = ws.Find(x => x.Name.ToUpper() == "LOB");
                    if (rel == null)
                    {
                        w.Close();
                        MessageBox.Show("Aba 'Relatorio' não encontrada. abortado.");
                        status = false;
                        return new Linha_de_Balanco();
                    }
                    if (av == null)
                    {
                        w.Close();
                        MessageBox.Show("Aba 'Avanço' não encontrada. abortado.");
                        status = false;
                        return new Linha_de_Balanco();
                    }
                    if (rec == null)
                    {
                        w.Close();
                        MessageBox.Show("Aba 'Recursos' não encontrada. abortado.");
                        status = false;
                        return new Linha_de_Balanco();
                    }
                    if (lob == null)
                    {
                        w.Close();
                        MessageBox.Show("Aba 'LOB' não encontrada. abortado.");
                        status = false;
                        return new Linha_de_Balanco();
                    }
                    var cels_efetivo = new List<ExcelRange>();
                    cels_efetivo.Add( rec.Cells["B4"]);
                    cels_efetivo.Add(rec.Cells["B39"]);
                    cels_efetivo.Add(rec.Cells["B40"]);
                    cels_efetivo.Add(rec.Cells["B41"]);

                    cels_efetivo = cels_efetivo.FindAll(x => x.Text != "" && x.Value != null);



                    cels_efetivo = cels_efetivo.FindAll(x => !x.Text.ToUpper().Contains("EFETIVO"));

                    if (cels_efetivo.Count>0)
                    {
                        w.Close();
                        MessageBox.Show("Aba  'Recursos', Linhas B4, B39, B40 e B41 são reservadas somente para efetivo. Se contém um efetivo, coloque a palavra 'Efetivo' na coluna B, (Equipamento).");
                        status = false;
                        return new Linha_de_Balanco();
                    }



                    #region aba Relatorio
                    r.emissao = new Data(rel.Cells["R3"].Text);
                    r.inicio_cronograma = new Data(rel.Cells["F19"].Text);
                    r.fim_cronograma = new Data(rel.Cells["M19"].Text);
                    r.status = Conexoes.Utilz.StringParaEnum<Status_Montagem>(rel.Cells["N12"].Text);
                    r.gerente = rel.Cells["F12"].Text;

                    w.somaProgresso();
                    #endregion

                    #region Aba Recursos
                    var datas_recurso = rec.Cells["I3:KL3"].ToList().Select(x => new Data(x)).ToList().FindAll(x => x.valido).ToList();
                    //equipamento
                    for (int i = 4; i < 42; i++)
                    {

                        string equipe = rec.Cells["A" + i].Text;
                        string equipamento = rec.Cells["B" + i].Text;
                        double previsto = Conexoes.Utilz.Double(rec.Cells["C" + i].Text);
                        double utilizado = Conexoes.Utilz.Double(rec.Cells["D" + i].Text);
                        string id = rec.Cells["F" + i].Text;

                        if (equipe != "" | equipamento != "")
                        {
                            var valores = rec.Cells["I" + i + ":KL" + i].ToList().FindAll(x => x.Value != null);
                            Recurso pp = new Recurso();
                            pp.equipe = equipe;
                            pp.descricao = equipamento;
                            pp.lob = r;
                            pp.valor_previsto_importado = previsto;
                            pp.tipo = Tipo_Recurso.Recurso;
                            pp.id = id;
                            pp.Getid(atual);
                            if (pp.id == "")
                            {
                                pp.Setid();
                                rec.Cells["F" + i].Value = pp.id;
                                salvar = true;
                            }

                            foreach (var valor in valores)
                            {
                                var vlr = Conexoes.Utilz.Double(valor.Value);
                                var data = datas_recurso.Find(x => x.col == valor.End.Column);
                                if (data != null && vlr>0)
                                {
                                    pp.AddApontamento(data, vlr, "", Tipo.Equipamento);
                                }

                            }
                            if ( (previsto > 0 | utilizado > 0 | pp.previsto.Count>0))
                            {

                            r.recursos__previstos.Add(pp);
                            }
                        }
                    }
                    w.somaProgresso();

                    //supervisor
                    for (int i = 45; i < 53; i++)
                    {
                        string equipe = rec.Cells["A" + i].Text;
                        string equipamento = rec.Cells["B" + i].Text;
                        double previsto = Conexoes.Utilz.Double(rec.Cells["C" + i].Value);
                        string id = rec.Cells["F" + i].Text;

                        if (equipe != "" | equipamento != "")
                        {
                            var valores = rec.Cells["I" + i + ":KL" + i].ToList().FindAll(x => x.Value != null);
                            Recurso pp = new Recurso();
                            pp.equipe = equipe;
                            pp.descricao = equipamento;
                            pp.lob = r;
                            pp.valor_previsto_importado = previsto;
                            pp.tipo = Tipo_Recurso.Supervisor;
                            pp.id = id;
                            pp.Getid(atual);
                            if (pp.id == "")
                            {
                                pp.Setid();
                                rec.Cells["F" + i].Value = pp.id;
                                salvar = true;
                            }
                            foreach (var valor in valores)
                            {
                                var vlr = Conexoes.Utilz.Double(valor.Value);
                                var data = datas_recurso.Find(x => x.col == valor.End.Column);
                                if (data != null && vlr > 0)
                                {
                                    pp.AddApontamento(data, vlr, "", Tipo.Supervisor);
                                }

                            }
                            r.supervisor.Add(pp);
                        }
                    }
                    w.somaProgresso();

                    //improdutividade
                    for (int i = 56; i < 64; i++)
                    {
                        string motivo = rec.Cells["B" + i].Text;
                        string id = rec.Cells["F" + i].Text;
                        if (motivo != "")
                        {
                            var valores = rec.Cells["I" + i + ":KL" + i].ToList().FindAll(x => x.Value != null);
                            Recurso pp = new Recurso();
                            pp.motivo = motivo;
                            pp.tipo = Tipo_Recurso.Improdutividade;
                            pp.lob = r;
                            pp.id = id;
                            pp.Getid(atual);
                            if (pp.id == "")
                            {
                                pp.Setid();
                                rec.Cells["F" + i].Value = pp.id;
                                salvar = true;
                            }

                            foreach (var valor in valores)
                            {
                                var vlr = Conexoes.Utilz.Double(valor.Value);
                                var data = datas_recurso.Find(x => x.col == valor.End.Column);
                                if (data != null && vlr > 0)
                                {
                                    pp.AddApontamento(data, vlr, "", Tipo.Improdutividade);
                                }

                            }
                            r.improdutividade.Add(pp);
                        }
                    }
                    w.somaProgresso();

                    #endregion


                    var celulasid = av.Cells["A1:A406"].ToList().FindAll(x => x.Value != null).ToList();
                    var datas = av.Cells["J2:LY2"].ToList().FindAll(x => x.Value != null).Select(x => new Data(x)).ToList();
                    List<Fase> etapas = new List<Fase>();
                    #region Aba Avanço
                    for (int i = 5; i < 206; i++)
                    {
                        //vai na aba lob e pega os dados das etapas cadastradas
                        string pep = lob.Cells["D" + i].Text;
                        string atividade = lob.Cells["C" + i].Text;
                        string etp = lob.Cells["F" + i].Text;
                        if (etp.Replace(" ","").Replace("_","").Replace("-","") != "" && atividade.Replace(" ","").Replace("-","").Replace("_","") != "")
                        {
                            //se a etapa e a atividade não estão em branco
                            int num = Conexoes.Utilz.Int(lob.Cells["A" + i].Text);
                            string equipe = lob.Cells["G" + i].Text;
                            double efetivo = Conexoes.Utilz.Double(lob.Cells["H" + i].Text);
                            Data inicio = new Data(lob.Cells["I" + i]);
                            Data fim = new Data(lob.Cells["J" + i]);
                            double area_etapa = Conexoes.Utilz.Double(lob.Cells["K" + i].Text);
                            double valor_etapa = Conexoes.Utilz.Double(lob.Cells["L" + i].Text);
                            double peso_atividade = Conexoes.Utilz.Double(lob.Cells["M" + i].Text);
                            double tamanho_obra = Conexoes.Utilz.Double(lob.Cells["N" + i].Text);
                            string pep_sap = lob.Cells["O" + i].Text;
                            string id = lob.Cells["P" + i].Text;
                            var celav = celulasid.Find(x => Conexoes.Utilz.Int(x.Text) == num);

                            Fase etapa = etapas.Find(x => x.descricao == etp.Replace("_", " "));
                            if (etapa == null)
                            {
                                etapa = new Fase();
                                etapa.descricao = etp.Replace("_", " ");
                                etapa.area = area_etapa;
                                etapa.lob = r;
                                etapas.Add(etapa);

                            }

                            Fase pp = new Fase();
                            pp.efetivo = efetivo;
                            pp.inicio = inicio;
                            pp.pai = etapa;
                            pp.fim = fim;
                            pp.area = area_etapa;
                            pp.peso_fase = peso_atividade;
                            pp.pep = pep_sap;

                            pp.cod = pep;
                            pp.id = id;
                           
                            pp.descricao = atividade.Replace(pep + "_-_", "").Replace(pep + " - ", "").Replace("_", " ");
                            pp.lob = r;
                            pp.equipe = equipe;
                            pp.Getid(atual);
                            pp.pai = etapa;

                            //se acha a celula correspondente na aba "Avanço" e o id num é maior que 0
                            if (celav != null && num > 0)
                            {
                                var lp = celav.End.Row;
                                var lr = lp + 1;
                                var apontamentos = av.Cells["J$@$:LY$@$".Replace("$@$", lr.ToString())].ToList().FindAll(x => x.Value != null);
                                foreach (var ap in apontamentos)
                                {
                                    var chv =av.Cells[1, ap.End.Column].Text;
                                    var chv0 =av.Cells[6, ap.End.Column].Text;
                                    //ignora as colunas que são a soma da semana
                                    if ((chv != "1" && chv!="0") | chv0=="1")
                                    {
                                        continue;
                                    }
                                        var valor = Conexoes.Utilz.Double(ap.Text);
                                    if (valor > 0)
                                    {
                                        var dt = datas.Find(x => x.col == ap.End.Column);
                                        if (dt != null && pp.GetApontamentos().Find(x=>x.data.datastr ==dt.datastr) == null)
                                        {
                                           pp.AddApontamento(dt, valor, "");
                                        }
                                    }
                                }
                          
                                etapa.fases.Add(pp);
                            }
                        }
                    }
                    w.somaProgresso();

                    #endregion

                    r.fases.AddRange(etapas);

                    if(salvar)
                    {
                        pck.SaveAs(new FileInfo(path));
                    }
                }
            }
            catch (Exception ex)
            {

                if (Conexoes.Utilz.Pergunta("Tentar novamente?\n\n" + ex.Message + "\n" + ex.StackTrace))
                {
                    w.Close();
                    goto retentar;
                }
                else
                {
                    status = false;
                    return new Linha_de_Balanco();
                }

            }
            var peps = r.Subfases().Select(x => x.pep).GroupBy(x => x).ToList();
            var pps = peps.FindAll(x => x.Count() > 1);
            var psp = peps.FindAll(x => x.Key == "");
            if(pps.Count>0)
            {
                w.Close();
                MessageBox.Show("Há peps repetidos dentro desse arquivo. Ajuste os peps (Aba LOB, coluna O)");
                status = false;
                return r;
            }
            if(psp.Count>0)
            {
                w.Close();
                MessageBox.Show("Há peps em branco dentro desse arquivo. Ajuste os peps (Aba LOB, coluna O)");
                status = false;
                return r;
            }

            var ss = r.Subfases().FindAll(x => Conexoes.Utilz.PEP.Get.Pedido(x.pep, true) != atual.pedido);
            //if (ss.Count > 0)
            //{
            //    w.Close();
            //    MessageBox.Show("Há" + ss.Count + " peps que não são do pedido " + atual.pedido + " dentro desse arquivo. Ajuste os peps (Aba LOB, coluna O)");
            //    status = false;
            //    return r;
            //}
            status = true;
            w.Close();
            return r;
        }
        public static bool ExportarApontamentos(Linha_de_Balanco dados, Obra obra, bool abrir, string Destino = null)
        {
            if(!dados.emissao.valido)
            {
                dados.emissao = new Data(DateTime.Now);
                dados.Salvar();
               dados  =  dados.Carregar();
            }
            if (dados.inicio_real.Getdata()> dados.inicio.Getdata() | !dados.inicio_real.valido)
            {
                dados.inicio_real.SetData(dados.inicio);
            }

            if (dados.fim_real.Getdata() < dados.fim.Getdata() | !dados.fim_real.valido)
            {
                dados.fim_real.SetData(dados.fim);
            }

            if(!dados.inicio_cronograma.valido)
            {
                dados.inicio_cronograma.SetData(dados.inicio_real);
            }
            if(!dados.fim_cronograma.valido)
            {
                dados.fim_cronograma.SetData(dados.fim_real);
            }
            var d_min = dados.inicio.Getdata();

            if (Destino == null)
            {
                Destino = Biblioteca_Daniel.Arquivo_Pasta.salvar("XLSX", "SELECIONE O DESTINO");
            }

            if(Destino==null)
            {
                return false;
            }
            
            if(Destino == "")
            {
                return false;
            }

            retentar:
            try
            {
                if (File.Exists(Destino)) { File.Delete(Destino); };

                File.Copy(Vars.template_avanco, Destino);
            }
            catch (Exception EX)
            {
                if (abrir)
                {
                    if (abrir)
                    {
                        if (Conexoes.Utilz.Pergunta(EX.Message + "\n\n Tentar Novamente?"))
                        {
                            goto retentar;
                        }
                    }

                }
                return false;
            }

            Conexoes.Wait ww = new Conexoes.Wait(10, "Criando planilha...");
            ww.Show();
            try
            {
          
                using (var pck = new OfficeOpenXml.ExcelPackage())
                {


                    using (Stream stream = new FileStream(Destino,
                                     FileMode.Open,
                                     FileAccess.Read,
                                     FileShare.ReadWrite))
                    {
                        pck.Load(stream);
                    }

                    pck.Workbook.CalcMode = ExcelCalcMode.Automatic;

                    dados.AjustaPesosEtapas();
                    ww.somaProgresso();

                    foreach (var w in pck.Workbook.Worksheets)
                    {
                        //preenche a linha de balanço
                        if (w.Name.ToUpper() == "RELATORIO")
                        {
                            w.Cells["F" + 10].Value = obra.nome_obra;
                            w.Cells["F" + 12].Value = obra.gerente;
                            w.Cells["N" + 12].Value = dados.status;
                            w.Cells["R" + 10].Value = obra.contrato;
                            //w.Cells["R" + 3].Value = dados.emissao.Getdata(); //coloquei formula que preenche com o dia de hoje
                            w.Cells["F" + 19].Value = dados.inicio_cronograma.Getdata();
                            w.Cells["F" + 20].Value = dados.inicio_real.Getdata();
                            w.Cells["M" + 19].Value = dados.fim_cronograma.Getdata();
                            w.Cells["M" + 20].Value = dados.fim_real.Getdata();
                            ww.somaProgresso();

                        }
                        else if (w.Name.ToUpper() == "LOB")
                        {
                            
                            int l0 = 5;
                            int l = l0;

                            w.Cells["Q2"].Value = d_min;

                            foreach (var e in dados.fases)
                            {
                                foreach (var sub in e.fases)
                                {
                                    w.Cells["C" + l].Value = sub.descricao;
                                    w.Cells["D" + l].Value = sub.cod;
                                    w.Cells["E" + l].Value = sub.nomestr;
                                    w.Cells["F" + l].Value = e.ToString().Replace("_", " ");
                                    w.Cells["G" + l].Value = sub.equipe;
                                    w.Cells["H" + l].Value = sub.efetivo;
                                    w.Cells["I" + l].Value = sub.inicio.Getdata();
                                    w.Cells["J" + l].Value = sub.fim.Getdata();
                                    w.Cells["K" + l].Value = e.area;
                                    w.Cells["L" + l].Value = e.peso_fase;
                                    w.Cells["M" + l].Value = sub.peso_fase;
                                    w.Cells["N" + l].Value = dados.area_total;
                                    w.Cells["O" + l].Value = sub.pep;
                                    w.Cells["P" + l].Value = sub.id;
                                    l++;
                                }
                            }

                            ww.somaProgresso();
                        }
                        else if (w.Name.ToUpper() == "RECURSOS")
                        {
                            var datas = w.Cells["I3:KL3"].ToList().Select(x => new Data(x)).ToList().FindAll(x => x.valido).ToList();
                            var efetivos = dados.recursos__previstos.FindAll(x => x.descricao.ToUpper().Contains("EFETIVO"));
                            var outros = dados.recursos__previstos.FindAll(x => !x.descricao.ToUpper().Contains("EFETIVO"));
                            //começa na coluna g
                            int col_0 = 9;
                            List<int> l_efetivos = new List<int> { 4, 39, 40, 41 };

                           
                            for (int i = 0; i < efetivos.Count; i++)
                            {
                                if (i < l_efetivos.Count)
                                {
                                    var le = l_efetivos[i];
                                    w.Cells["A" + le].Value = efetivos[i].equipe;
                                    w.Cells["B" + le].Value = efetivos[i].descricao;
                                    w.Cells["C" + le].Value = efetivos[i].total_previsto;
                                    foreach (var lanc in efetivos[i].GetApontamentos())
                                    {
                                        //vê quantos dias da data mínima tem o lançamento e pula as colunas.
                                        //var col = (lanc.data.Getdata() - d_min).Days;
                                        var col = datas.Find(x => x.datastr == lanc.data.datastr);
                                        if (col !=null)
                                        {
                                            //grava a linha do efetivo
                                            w.Cells[le, col.col].Value = lanc.valor;
                                        }
                                    }
                                }
                            }

                            int ll = 5;
                            for (int i = 0; i < outros.Count; i++)
                            {
                                var efet = outros[i];

                                w.Cells["A" + ll].Value = efet.equipe;
                                w.Cells["B" + ll].Value = efet.descricao;
                                w.Cells["C" + ll].Value = efet.total_previsto;
                                efet.Setid();
                                w.Cells["F" + ll].Value = efet.id;
                                foreach (var lanc in efet.GetApontamentos())
                                {
                                    //vê quantos dias da data mínima tem o lançamento e pula as colunas.
                                    var col = datas.Find(x => x.datastr == lanc.data.datastr);
                                    if (col != null)
                                    {
                                        //grava a linha do efetivo
                                        w.Cells[ll, col.col].Value = lanc.valor;
                                    }
                                }
                                ll++;
                            }


                            //supervisores
                            ll = 45;
                            for (int i = 0; i < dados.supervisor.Count; i++)
                            {
                                var lancs = dados.supervisor[i];

                                w.Cells["A" + ll].Value = lancs.supervisor;
                                w.Cells["B" + ll].Value = lancs.cargo;
                                w.Cells["C" + ll].Value = lancs.total_previsto;
                                foreach (var lanc in lancs.GetApontamentos())
                                {
                                    //vê quantos dias da data mínima tem o lançamento e pula as colunas.
                                    var col = datas.Find(x => x.datastr == lanc.data.datastr);
                                    if (col != null)
                                    {
                                        //grava a linha do efetivo
                                        w.Cells[ll, col.col].Value = lanc.valor;
                                    }
                                }
                                ll++;
                            }

                            //improdutividade
                            ll = 56;
                            for (int i = 0; i < dados.supervisor.Count; i++)
                            {
                                var lancs = dados.supervisor[i];

                                w.Cells["B" + ll].Value = lancs.motivo;
                                w.Cells["C" + ll].Value = lancs.total_previsto;
                                foreach (var lanc in lancs.GetApontamentos())
                                {
                                    //vê quantos dias da data mínima tem o lançamento e pula as colunas.
                                    var col = datas.Find(x => x.datastr == lanc.data.datastr);
                                    if (col != null)
                                    {
                                        //grava a linha do efetivo
                                        w.Cells[ll, col.col].Value = lanc.valor;
                                    }
                                }
                                ll++;
                            }

                            ww.somaProgresso();
                            //pck.Save();
                        }
                        else if (w.Name.ToUpper() == "AVANÇO")
                        {
                            //primeira linha do realizado
                            var ll = 8;
                            var col_0 = 10;
                            w.Cells["J2"].Calculate();
                            //w.Cells["J2"].Value = d_min;
                            //pck.Save();

                            //w.Cells["J2:LY2"].Calculate();
                            //var dtsss = w.Cells["J2:LY2"].ToList();
                            DateTime t1 = d_min;
                           
                            var datas= w.Cells["J2:LY2"].ToList().Select(x=> new Data(x)).ToList().FindAll(x=> x.valido).ToList();
                            foreach (var subetapa in dados.Subfases())
                            {
                                //w.Cells["B" + (ll-1)].Value = subetapa.ToString();
                                //w.Cells["C" + (ll-1)].Value = etapa.ToString();
                                w.Cells["G" + (ll - 1)].Value = subetapa.inicio.Getdata();
                                w.Cells["H" + (ll - 1)].Value = subetapa.fim.Getdata();
                                //w.Cells["I" + (ll-1)].Value = subetapa.previsto>1?subetapa.previsto/100:subetapa.previsto;
                                foreach (var lanc in subetapa.GetApontamentos())
                                {
                                    var dt = datas.Find(x => x.Getdata() == lanc.data.Getdata());

                                    //vê quantos dias da data mínima tem o lançamento e pula as colunas.
                                    int col = (lanc.data.Getdata() - d_min).Days;
                                    int add = (int)Math.Floor((double)col / 7);

                                    col = col + add;

                                    if (dt != null)
                                    {
                                        //grava a linha do efetivo
                                        w.Cells[ll, dt.col].Value = lanc.valor / 100;
                                        //w.Cells[ll,dt.col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                        //w.Cells[ll,dt.col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                                    }
                                }
                      
                                var dias_uteis = Conexoes.Utilz.GetRangeDatas(subetapa.inicio_real.Getdata(), subetapa.fim_real.Getdata(), false, false);
                                //é desativado o peso da fase pq já está na fórmula multiplicando da aba LOB
                                var utt = subetapa.GetPrevistoDistribuidoDias(false);

                                //grava o previsto.
                                //12/05/2020 - mudei a logica para pegar a mesma função da Fase
                                foreach (var lanc in utt)
                                {
                                    var dt = datas.FindAll(x => x.datastr == lanc.data.datastr);
                                    //vê quantos dias da data mínima tem o lançamento e pula as colunas.


                                    if (dt.Count > 0)
                                    {
                                        //grava a linha do efetivo
                                        w.Cells[ll - 1, dt[0].col].Value = lanc.valor;
                                    }
                                    else
                                    {

                                    }

                                }


                                //if (dias_uteis.Count > 0)
                                //{
                                //    double porcentagem = (double)(1 / (double)dias_uteis.Count);

                                //    //grava o previsto
                                //    foreach (var lanc in dias_uteis)
                                //    {
                                //        var dt = datas.FindAll(x => x.datastr == new Data(lanc).datastr);
                                //        //vê quantos dias da data mínima tem o lançamento e pula as colunas.


                                //        if (dt.Count > 0)
                                //        {
                                //            //grava a linha do efetivo
                                //            w.Cells[ll - 1, dt[0].col].Value = porcentagem;
                                //        }

                                //    }
                                //}


                                ll = ll + 2;
                            }
                        }
                        else if(w.Name.ToUpper() == "DADOS_GRAFICO")
                        {
                            var datas = w.Cells["B1:LQ1"].ToList().Select(x => new Data(x)).ToList().FindAll(x => x.valido).ToList();
                            var dt_Max = dados.fim;
                            var dt_final = datas.Find(x => x.datastr == dt_Max.datastr);

                            if(dt_final!=null)
                            {
                                w.Cells[1, dt_final.col, 3, datas.Last().col].Clear();
                            }

                        }

                        ww.somaProgresso();
                        //pck.Save();
                    }
                    pck.SaveAs(new FileInfo(Destino));
                    ww.somaProgresso();
                    ww.Close();
                }
                if (abrir && File.Exists(Destino))
                {
                    Conexoes.Utilz.Abrir(Destino);
                }
            }
            catch (Exception ex)
            {
                ww.Close();
                if (abrir)
                {
                    if (Conexoes.Utilz.Pergunta(ex.Message + "\n\n Tentar Novamente?\n\n" + ex.StackTrace))
                    {
                        goto retentar;
                    }
                }
            }


         

            return File.Exists(Destino);
        }





        private static int GetRecursos(Linha_de_Balanco retorno, ExcelWorksheet lob, int l_data, int col_recursos, int col_datas, int L)
        {
            var titulo = GetValor<string>(lob.Cells["N" + L]);
            //pula até 10 linhas se tiver alguma linha em branco
            pula_em_branco_recursos(lob, ref L, ref titulo);

            int dts = col_datas;
            var dt0 = GetValor<string>(lob.Cells[l_data - 1, dts, l_data, dts]);
            List<string> datas = new List<string>();
            while (dt0 != "" && dt0 != null)
            {

                try
                {
                    datas.Add(dt0);
                    dts++;
                    dt0 = GetValor<string>(lob.Cells[l_data - 1, dts, l_data, dts]);
                    if (dt0 == "")
                    {
                        int c = 0;
                        int max = 10;
                        while (c < max && (dt0 == "" | dt0 == null))
                        {
                            dts++;
                            dt0 = GetValor<string>(lob.Cells[l_data, dts, l_data, dts]);
                        }
                    }
                }
                catch (Exception)
                {

                    dt0 = "";
                }

            }
            while (titulo != "" && titulo != null)
            {


                titulo = GetValor<string>(lob.Cells["N" + L]);
                L++;
                pula_em_branco_recursos(lob, ref L, ref titulo);

                if (titulo != "" && titulo != null && titulo.ToUpper() != "SEMANA" && !titulo.ToUpper().Replace(" ", "").Contains("MÉDIAEFETIVO"))
                {
                    Recurso pp = new Recurso();
                    pp.descricao = titulo;
                    for (int i = 0; i < datas.Count; i++)
                    {
                        Apontamento ap = new Apontamento();
                        var valor = GetValor<double>(lob.Cells[L, col_recursos + i]);
                        if (valor > 0)
                        {
                            ap.valor = Conexoes.Utilz.Double(valor);
                            ap.data = new Data(datas[i]);
                            pp.previsto.Add(ap);
                        }
                    }
                    if (pp.previsto.Count > 0)
                    {
                        retorno.recursos__previstos.Add(pp);
                    }
                }


            }

            return L;
        }
        private static void pula_em_branco_recursos(ExcelWorksheet w, ref int L, ref string titulo)
        {
            titulo = GetValor<string>(w.Cells["N" + L]);

            if (titulo == "" | titulo == null)
            {
                int c = 0;
                int max = 50;
                while ((titulo == "" | titulo == null) && c < max)
                {
                    titulo = GetValor<string>(w.Cells["N" + L]);
                    if(titulo=="" | titulo == null)
                    {
                    L++;
                    }
                    c++;
                }

            }
        }
        public static T GetValor<T>(ExcelRange celula)
        {
            try
            {
                return celula.GetValue<T>();
            }
            catch (Exception ex)
            {
            
                
            }


            try
            {
                if (celula.Value != null)
                {

                    if(celula.Value.GetType().ToString().ToUpper().Replace(" ","").Contains("OBJECT["))
                    {
                        //uso esse cara pra tentar celulas que estão juntas
                        var ss = (celula.Value as object[,]).Cast<object>().ToList().FindAll(x => x != null);
                        
                       

                        var sst = string.Join("", ss);
                        
                        return (T)(object)sst;
                    }
                    return (T)(object)celula.Value.ToString();
                }
            }
            catch (Exception ex)
            {

            }

            return default(T);
        }
        private static string getlinhastr(ExcelRange wlinha)
        {
            string retorno = "";
            foreach(var p in wlinha.ToList())
            {
                try
                {
                    var ss = p.Value;
                    if (ss != null)
                    {
                        retorno = retorno + p.GetValue<string>();
                    }
                }
                catch (Exception ex)
                {

                }
               
            }
            return retorno.Replace(" ","");
            //return string.Join("", wlinha.SelectMany(x => x.ToList().Select(y => y.Value != null ? y.GetValue<string>().Replace(" ", "") : "")));
        }
        private static void getchaves(ExcelWorksheet w, int L, out string col_J, out string col_B, out string col_N, out string col_D,out string col_E)
        {
            col_J = w.Cells["J" + L].GetValue<string>();
            col_B = w.Cells["B" + L].GetValue<string>();
            col_N = w.Cells["N" + L].GetValue<string>();
            col_D = w.Cells["D" + L].GetValue<string>();
            col_E = w.Cells["E" + L].GetValue<string>();
            if (col_J == null)
            {
                col_J = "";
            }
            if (col_B == null)
            {
                col_B = "";
            }
            if (col_N == null)
            {
                col_N = "";
            }
            if (col_D == null)
            {
                col_D = "";
            }
            if (col_E == null)
            {
                col_E = "";
            }
        }
    }
}
