using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCM_Offline
{
    public class OpcoesImportarApontamentos
    {
        [Category("Importar")]
        [DisplayName("Avanço de Etapas")]
        public bool apontamentos_etapas { get; set; } = true;
        [Category("Importar")]
        [DisplayName("Equipamentos")]
        public bool apontamentos_recursos { get; set; } = true;
        [Category("Importar")]
        [DisplayName("Observações")]
        public bool observacoes { get; set; } = true;
        [Category("Importar")]
        [DisplayName("Restrições")]
        public bool restricoes { get; set; } = true;
        [Category("Importar")]
        [DisplayName("Planos de Ação")]
        public bool planosdeacao { get; set; } = true;



        [Category("Importar")]
        [DisplayName("Novos")]
        public bool importar_novos { get; set; } = true;

        [Category("Atualizar")]
        [DisplayName("Datas")]
        public bool atualiza_datas { get; set; } = true;
        [Category("Atualizar")]
        [DisplayName("Datas Cronograma")]
        [Browsable(false)]
        public bool atualiza_datas_cronograma { get; set; } = true;
        [Category("Atualizar")]
        [DisplayName("Nomes equipes")]
        public bool atualiza_equipes { get; set; } = true;
        [Category("Atualizar")]
        [DisplayName("Nomes PEPs")]
        public bool nomes_peps { get; set; } = true;
        [Category("Atualizar")]
        [DisplayName("Descrição Etapas")]
        public bool descricao { get; set; } = true;
        public OpcoesImportarApontamentos()
        {

        }
    }
}
