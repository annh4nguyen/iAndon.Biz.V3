//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace iAndon.Biz.Logic
{
    using System;
    using System.Collections.Generic;
    
    public partial class MES_WORK_ORDER
    {
        public string WORK_ORDER_ID { get; set; }
        public string WORK_ORDER_CODE { get; set; }
        public string PRODUCT_ID { get; set; }
        public decimal QUANTITY { get; set; }
        public decimal PLANNED_QUANTITY { get; set; }
        public decimal PRODUCED_QUANTITY { get; set; }
        public System.DateTime DEADLINE { get; set; }
        public string DESCRIPTION { get; set; }
        public string PO_CODE { get; set; }
        public short STATUS { get; set; }
    }
}
