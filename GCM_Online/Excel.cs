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
        public static bool SalvarResumo(string path, out string msg, bool abrir = true)
        {
            msg = "";
        novamente:
            try
            {
            retentar:
                try
                {
                    if(!File.Exists(Vars.template_resumo))
                    {
                        MessageBox.Show("Arquivo de template não encontrado: " + Vars.template_resumo);
                        return false;
                    }
                    if (File.Exists(path)) { File.Delete(path); };

                    File.Copy(Vars.tb_efetivos, path);
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
                var selecao = Conexoes.Utilz.SelecionarObjetos<Contrato>(dbase.obras(), null, "Selecione", true);
                if (selecao.Count > 0)
                {
            
                    using (var pck = new OfficeOpenXml.ExcelPackage())
                    {
                        Conexoes.Wait w = new Conexoes.Wait(10, "Carregando planilha...");
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

                        for (int i = 0; i < selecao.Count; i++)
                        {
                            int l = i + 1;
                            var ob = selecao[i];
                            var s = ob.Getlob();
                            re.Cells["A" + l].Value = ob.descricao;
                            re.Cells["B" + l].Value = ob.contrato;
                            re.Cells["C" + l].Value = ob.status.ToString();
                            re.Cells["D" + l].Value = ob.gerente;
                            re.Cells["E" + l].Value = ob.engenheiro;
                            re.Cells["F" + l].Value = ""/*INÍCIO CLIENTE*/;
                            re.Cells["G" + l].Value = ""/*FIM CLIENTE*/;
                            re.Cells["H" + l].Value = ob.inicio.Getdata();
                            re.Cells["I" + l].Value = ob.fim.Getdata();
                            re.Cells["J" + l].Value = ""/*DIAS DE ATRASO*/;
                            re.Cells["K" + l].Value = ""/*PROJEÇÃO DATA FIM*/;
                            re.Cells["L" + l].Value = s.GetTotal().previsto;
                            re.Cells["M" + l].Value = s.GetTotal().realizado;
                            re.Cells["N" + l].Value = s.GetTotalSemanaAnterior3().desvio;
                            re.Cells["N" + l].Value = s.GetTotalSemanaAnterior2().desvio;
                            re.Cells["N" + l].Value = s.GetTotalSemanaAnterior().desvio;
                        }

                        pck.SaveAs(new FileInfo(path));
                        w.somaProgresso();

                    }
                }
            }
            catch (Exception ex)
            {
                if (Conexoes.Utilz.Pergunta("Tentar novamente?\n\n" + ex.Message + "\n\nErro: " + ex.StackTrace))
                {
                    goto novamente;
                }

            }

            return File.Exists(path);
        }
    }
}
