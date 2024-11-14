using System;
using System.Collections.Generic;
using System.Drawing;

namespace Nop.Plugin.Misc.Moloni.Models
{
    public class Tax
    {
        public int product_id { get; set; }
        public int tax_id { get; set; }
        public float value { get; set; }
        public int order { get; set; }
        public int cumulative { get; set; }
        public TaxDetail? tax { get; set; }
    }

    public class TaxDetail
    {
        public int tax_id { get; set; }
        public int? type { get; set; }
        public int? saft_type { get; set; }
        public string? vat_type { get; set; }
        public string? stamp_tax { get; set; }
        public string? name { get; set; }
        public float? value { get; set; }
        public string? fiscal_zone { get; set; }
        public int? active_by_default { get; set; }
        public string? exemption_reason { get; set; }
    }

    public class TaxAndFees
    {
        public int tax_id { get; set; }
        public string name { get; set; }
        public float value { get; set; }
        public int type { get; set; } //1 = A taxa é uma percentagem (Ex.: IVA)
                                      //2 = A taxa é um valor monetário fixo, independentemente do artigo ou serviço a que é aplicado(Ex: Selo fiscal)
                                      //3 = A taxa é um valor monetário e depende do artigo ou serviço a que é aplicado(Ex.: Eco taxa).
        public int saft_type { get; set; } //1 = A taxa é um valor adicionado (IVA)
                                           //2 = A taxa é um imposto direto (Imposto de Selo)
                                           //3 = A taxa não é nenhum dos dois casos anteriores
        public string? vat_type { get; set; } //RED = IVA reduzido       !!! Obrigatório caso saft_type = 1 !!!
                                             //INT = IVA intermédio
                                             //NOR = IVA normal
                                             //ISE = Isento de IVA
                                             //OUT = Outro tipo de IVA
        public string? stamp_tax { get; set; } // !!! Obrigatório, caso saft_type = 2 !!!
        public string exemption_reason { get; set; } // Apenas valores de M01 a M16 !!! Obrigatório se value = 0 !!!
        public string fiscal_zone { get; set; }
        public int active_by_default { get; set; }
    }

    public class TaxExemption
    {
        public string code { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int visibility { get; set; }
    }
}