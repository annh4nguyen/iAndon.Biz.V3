﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class Entities : DbContext
    {
        public Entities()
            : base("name=Entities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<DM_MES_BREAK_TIME> DM_MES_BREAK_TIME { get; set; }
        public virtual DbSet<DM_MES_CONFIGURATION> DM_MES_CONFIGURATION { get; set; }
        public virtual DbSet<DM_MES_EVENTDEF> DM_MES_EVENTDEF { get; set; }
        public virtual DbSet<DM_MES_LINE> DM_MES_LINE { get; set; }
        public virtual DbSet<DM_MES_NODE> DM_MES_NODE { get; set; }
        public virtual DbSet<DM_MES_NODE_TYPE> DM_MES_NODE_TYPE { get; set; }
        public virtual DbSet<DM_MES_PRODUCT_CATEGORY> DM_MES_PRODUCT_CATEGORY { get; set; }
        public virtual DbSet<DM_MES_PRODUCT_CONFIG> DM_MES_PRODUCT_CONFIG { get; set; }
        public virtual DbSet<DM_MES_ZONE> DM_MES_ZONE { get; set; }
        public virtual DbSet<MES_MSG_LINE_EVENT> MES_MSG_LINE_EVENT { get; set; }
        public virtual DbSet<MES_MSG_NODE_EVENT> MES_MSG_NODE_EVENT { get; set; }
        public virtual DbSet<MES_MSG_NODE_STOP> MES_MSG_NODE_STOP { get; set; }
        public virtual DbSet<MES_MSG_NODE_WORKING> MES_MSG_NODE_WORKING { get; set; }
        public virtual DbSet<MES_NODE_EVENT> MES_NODE_EVENT { get; set; }
        public virtual DbSet<MES_PRODUCT_CYCLE_TIME> MES_PRODUCT_CYCLE_TIME { get; set; }
        public virtual DbSet<MES_RESPONSIBILITY> MES_RESPONSIBILITY { get; set; }
        public virtual DbSet<DM_FACTORY> DM_FACTORY { get; set; }
        public virtual DbSet<DM_MES_STOP_REASON> DM_MES_STOP_REASON { get; set; }
        public virtual DbSet<MES_LOG_LAST_UPDATE> MES_LOG_LAST_UPDATE { get; set; }
        public virtual DbSet<MES_WORK_ORDER> MES_WORK_ORDER { get; set; }
        public virtual DbSet<MES_LINE_EVENT> MES_LINE_EVENT { get; set; }
        public virtual DbSet<MES_RAW_UPDATE_CONFIG> MES_RAW_UPDATE_CONFIG { get; set; }
        public virtual DbSet<MES_RAW_UPDATE_EVENT> MES_RAW_UPDATE_EVENT { get; set; }
        public virtual DbSet<MES_RAW_UPDATE_REPORT_LINE_DETAIL> MES_RAW_UPDATE_REPORT_LINE_DETAIL { get; set; }
        public virtual DbSet<MES_MSG_LINE> MES_MSG_LINE { get; set; }
        public virtual DbSet<DG_DM_SHIFT> DG_DM_SHIFT { get; set; }
        public virtual DbSet<MES_LINE_STOP> MES_LINE_STOP { get; set; }
        public virtual DbSet<MES_LINE_WORKING> MES_LINE_WORKING { get; set; }
        public virtual DbSet<MES_MSG_LINE_STOP> MES_MSG_LINE_STOP { get; set; }
        public virtual DbSet<MES_MSG_LINE_WORKING> MES_MSG_LINE_WORKING { get; set; }
        public virtual DbSet<MES_WORK_PLAN> MES_WORK_PLAN { get; set; }
        public virtual DbSet<DM_MES_PRODUCT> DM_MES_PRODUCT { get; set; }
        public virtual DbSet<MES_REPORT_LINE> MES_REPORT_LINE { get; set; }
        public virtual DbSet<MES_WORK_PLAN_DETAIL> MES_WORK_PLAN_DETAIL { get; set; }
        public virtual DbSet<MES_LINE_TIME_PRODUCTION> MES_LINE_TIME_PRODUCTION { get; set; }
        public virtual DbSet<MES_MSG_LINE_DETAIL> MES_MSG_LINE_DETAIL { get; set; }
        public virtual DbSet<MES_MSG_LINE_PRODUCT> MES_MSG_LINE_PRODUCT { get; set; }
        public virtual DbSet<MES_REPORT_LINE_DETAIL> MES_REPORT_LINE_DETAIL { get; set; }
        public virtual DbSet<MES_WORK_PLAN_DETAIL_HISTORY> MES_WORK_PLAN_DETAIL_HISTORY { get; set; }
    }
}
