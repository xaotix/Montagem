using GCM_Offline;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GCM_Online
{
    public class Excel
    {
        public static bool SalvarResumo(string path, Data datamax, out string msg, bool abrir = true)
        {
            if(!datamax.valido)
            {
                msg = "Data inválida";
                return false;
            }
            msg = "";
        novamente:
            Conexoes.Wait w = new Conexoes.Wait(10, "Carregando planilha...");
            try
            {
            retentar:
                if (!File.Exists(Vars.template_resumo))
                {
                    MessageBox.Show("Arquivo de template não encontrado: " + Vars.template_resumo);
                    return false;
                }
                if (File.Exists(path)) { File.Delete(path); };
                var selecao = Conexoes.Utilz.SelecionarObjetos<Contrato>(dbase.obras(), null, "Selecione", true);
                if (selecao.Count > 0)
                {
                    File.Copy(Vars.template_resumo, path);
                    using (var pck = new OfficeOpenXml.ExcelPackage())
                    {
                       
                        w.Show();
                        w.somaProgresso();

                        using (Stream stream = new FileStream(path,
                                     FileMode.Open,
                                     FileAccess.Read,
                                     FileShare.ReadWrite))
                        {
                            pck.Load(stream);
                        }
                        var ws = pck.Workbook.Worksheets.ToList();

                        var re = ws.Find(x => x.Name.ToUpper() == "RESUMO");
                        if(re == null)
                        {
                            msg = "Aba 'Resumo' não encontrada no arquivo de template.";
                            return false;
                        }
                        var dt = Conexoes.Utilz.UltimoDiaDoMes(datamax.Getdata());
                        var dt1 = dt.AddDays(-7);
                        var dt2 = dt.AddDays(-14);
                        var dt3 = dt.AddDays(-21);
                        re.Cells["Z1"].Value = dt;
                        re.Cells["Q1"].Value = "DESVIO " + datamax.datastr;
                        selecao = selecao.OrderBy(x => x.descricao).ToList();
                        for (int i = 0; i < selecao.Count; i++)
                        {
                            int l = i + 4;
                            var ob = selecao[i];
                            var s = ob.Getlob();
                            s.CalcularEfetivosPrevistos();
                            re.Cells["A" + l].Value = ob.descricao;
                            re.Cells["B" + l].Value = ob.contrato;
                            re.Cells["C" + l].Value = ob.status.ToString();
                            re.Cells["D" + l].Value = ob.gerente;
                            re.Cells["E" + l].Value = ob.engenheiro;
                            re.Cells["F" + l].Value = string.Join(" / ", s.Getefetivos().FindAll(x=>x.equipe.ToUpper()!="INDEFINIDO").Select(x=> x.equipe).Distinct().ToList());

                            re.Cells["H" + l].Value = ob.inicio.Getdata();
                            re.Cells["I" + l].Value = ob.fim.Getdata();
                            re.Cells["J" + l].Value = s.dias_atraso(s._data_max);
                            re.Cells["K" + l].Value = ob.ultima_importacao;
                            re.Cells["L" + l].Value = s.GetTotal(s._data_max).previsto/100;
                            re.Cells["M" + l].Value = s.GetTotal(s._data_max).realizado/100;
                            re.Cells["N" + l].Value = Math.Abs(s.GetTotalSemanaAnterior3(s._data_max).desvio/100);
                            re.Cells["O" + l].Value = Math.Abs(s.GetTotalSemanaAnterior2(s._data_max).desvio/100);
                            re.Cells["P" + l].Value = Math.Abs(s.GetTotalSemanaAnterior(s._data_max).desvio/100);
                            re.Cells["Q" + l].Value = Math.Abs(s.GetTotal(s._data_max).desvio/100);

                            var diarias = s.diarias_efetivo();
                            re.Cells["W" + l].Value =diarias.previsto;
                            re.Cells["X" + l].Value =diarias.realizado;


                            var av0 = s.GetEfetivoSemana(new Data(dt));
                            var av1 = s.GetEfetivoSemana(new Data(dt1));
                            var av2 = s.GetEfetivoSemana(new Data(dt2));
                            var av3 = s.GetEfetivoSemana(new Data(dt3));


                            var ap0 = av0.avancos.SelectMany(x => x.avancos).SelectMany(y => y.avancos).ToList().FindAll(x => x.realizado > 0).ToList();
                            var ap1 = av1.avancos.SelectMany(x => x.avancos).SelectMany(y => y.avancos).ToList().FindAll(x => x.realizado > 0).ToList();
                            var ap2 = av2.avancos.SelectMany(x => x.avancos).SelectMany(y => y.avancos).ToList().FindAll(x => x.realizado > 0).ToList();
                            var ap3 = av3.avancos.SelectMany(x => x.avancos).SelectMany(y => y.avancos).ToList().FindAll(x => x.realizado > 0).ToList();

                            var dias0 = ap0.GroupBy(x => x.data.datastr).Count();
                            var dias1 = ap1.GroupBy(x => x.data.datastr).Count();
                            var dias2 = ap2.GroupBy(x => x.data.datastr).Count();
                            var dias3 = ap3.GroupBy(x => x.data.datastr).Count();


                            re.Cells["Z" + l].Value = Math.Abs(s.GetTotalSemanaAnterior3(new Data(dt)).realizado / 100);
                            re.Cells["AA" + l].Value =dias0;
                            re.Cells["AB" + l].Value = ap0.Sum(x => x.realizado) / dias0;

                            re.Cells["AC" + l].Value = Math.Abs(s.GetTotalSemanaAnterior2(new Data(dt)).realizado / 100);
                            re.Cells["AD" + l].Value = dias1;
                            re.Cells["AE" + l].Value = Math.Round(ap1.Sum(x=>x.realizado) / dias1,2);

                            re.Cells["AF" + l].Value = Math.Abs(s.GetTotalSemanaAnterior(new Data(dt)).realizado / 100);
                            re.Cells["AG" + l].Value = dias2;
                            re.Cells["AH" + l].Value = Math.Round(ap2.Sum(x => x.realizado) / dias2,2);

                            re.Cells["AI" + l].Value = Math.Abs(s.GetTotal(new Data(dt)).realizado / 100);
                            re.Cells["AJ" + l].Value = dias3;
                            re.Cells["Ak" + l].Value = Math.Round(ap3.Sum(x => x.realizado) / dias3,2);



                        }

                        pck.SaveAs(new FileInfo(path));
                        w.somaProgresso();

                        w.Close();

                    }
                }
            }
            catch (Exception ex)
            {
              
                if (Conexoes.Utilz.Pergunta("Tentar novamente?\n\n" + ex.Message + "\n\nErro: " + ex.StackTrace))
                {
                    w.Close();

                    goto novamente;
                }

            }
            if(File.Exists(path) && abrir)
            {
                Conexoes.Utilz.Abrir(path);
            }
            return File.Exists(path);
        }
    }
}
