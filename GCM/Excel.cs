using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCM
{
   public class Excel
    {
        public static Linha_de_Balanco CarregarLinhaDeBalanco(string path)
        {
            if(!File.Exists(path))
            {
                return new Linha_de_Balanco() { arquivoexcel = path, msgerro = "Arquivo não existe" };
            }
            Linha_de_Balanco retorno = new Linha_de_Balanco();
            retorno.arquivoexcel = path;
            var linhas = 0;
            var colunas = 0;
            int l0 = 15;
            int c0 = 1;

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
               
                var lob = ws.Find(x => x.Name.ToUpper() == "LOB");
                var wbase = ws.Find(x => x.Name.ToUpper().Replace(" ","") == "BASE");

                Conexoes.Wait w = new Conexoes.Wait(10, "Carregando planilha...");
                w.Show();
                w.somaProgresso();
                w.somaProgresso();

                if (lob!=null)
                {
                   
                    var l_data = 15;
                    var col_recursos = 16;
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


                            string col_J, col_B, col_N;
                            getchaves(lob, L, out col_J, out col_B, out col_N);

                            var min = new DateTime(2001, 01, 01);

                            if (col_J.ToUpper().Contains("ETAPA") && col_B != "")
                            {
                                Fase p = new Fase();
                                p.area = GetValor<double>(lob.Cells["H" + L]);
                                p.cod = GetValor<string>(lob.Cells["C" + L]);
                                p.inicio = new Data(GetValor<string>(lob.Cells["D" + L]));
                                p.fim = new Data(GetValor<string>(lob.Cells["E" + L]));
                                p.descricao = GetValor<string>(lob.Cells["B" + L]);
                                //pula uma linha
                                L++;
                                getchaves(lob, L, out col_J, out col_B, out col_N);
                                //procura pelas sub-etapas
                                while (!col_J.ToUpper().Contains("SUBETAPA") && col_B.Length > 0)
                                {
                                    if (col_J.ToUpper().Contains("EXISTE"))
                                    {
                                        Fase f = new Fase();
                                        f.pai = p;
                                        f.descricao = GetValor<string>(lob.Cells["B" + L]);
                                        f.cod = GetValor<string>(lob.Cells["C" + L]);
                                        f.inicio = new Data(GetValor<string>(lob.Cells["D" + L]));
                                        f.fim = new Data(GetValor<string>(lob.Cells["E" + L]));
                                        f.peso_fase = GetValor<double>(lob.Cells["I" + L]);
                                        f.montador = GetValor<string>(lob.Cells["N" + L]);
                                        f.efetivo = GetValor<double>(lob.Cells["P" + L]);

                                        if(f.inicio.Getdata()<min)
                                        {
                                            if(p.fases.Count>0)
                                            {
                                                f.inicio = new Data(p.fases.Last().inicio.Getdata().AddDays(1));
                                            }
                                        }
                                        if(f.fim.Getdata()<min)
                                        {
                                            f.fim = new Data(f.inicio.Getdata().AddDays(1));
                                        }

                                        if (f.descricao != null && f.cod != null)
                                        {
                                            p.fases.Add(f);
                                        }
                                    }

                                    L++;
                                    getchaves(lob, L, out col_J, out col_B, out col_N);
                                    wlinhastr = getlinhastr(lob.Cells[L, c0, L, colunas]);
                                }
                                if (p.fases.Count > 0)
                                {
                                    p.SetInicios();
                                    retorno.fases.Add(p);
                                }

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
                                        getchaves(lob, L, out col_J, out col_B, out col_N);
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
                                        Recurso pp = new Recurso();
                                        pp.descricao = titulo;
                                        var dts = col_datas;


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

                                                
                                                if(c==max)
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


                                        if (pp.previsto.Count > 0)
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
                if(wbase!=null)
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
                        if((desc=="" | desc == null) && pular<max_pular)
                        {
                            pular++;
                            continue;
                        }
                        else if(desc == "" | desc == null)
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
                                if(peso>0)
                                {
                                    retorno.fases_pesos_avanco_fisico.Add(new Fase() { cod = cod, peso_fase = peso, descricao = desc });
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
                                if (custo_mensal > 0)
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
            return retorno;
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

        private static void getchaves(ExcelWorksheet w, int L, out string col_J, out string col_B, out string col_N)
        {
            col_J = w.Cells["J" + L].GetValue<string>();
            col_B = w.Cells["B" + L].GetValue<string>();
            col_N = w.Cells["N" + L].GetValue<string>();
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
        }
    }
}
