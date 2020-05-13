using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Montagem
{
   public class Funcoes
    {

        public static GCM_Offline.Apontamento apontamento(string Titulo, GCM_Offline.Apontamento apontamento = null, bool valor = true, bool efetivo = false, bool responsavel = false, string titulo_responsavel = "Responsável", bool data = true)
        {
            AdicionarApontamento mm = new AdicionarApontamento(efetivo,valor,responsavel,apontamento);
           
            mm.Title = Titulo;
            mm.lbl_responsavel.Content = titulo_responsavel;
            mm.ShowDialog();
            if ((bool)mm.DialogResult)
            {
                if(apontamento==null)
                {
                return mm.apontamento;

                }
                else
                {
                    apontamento.data = mm.apontamento.data;
                    apontamento.descricao = mm.apontamento.descricao;
                    apontamento.valor = mm.apontamento.valor;
                    return apontamento;

                }
            }

            return null;

        }
    }
}
