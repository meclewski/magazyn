﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Magazyn
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class dbWarehouseEntities : DbContext
    {
        public dbWarehouseEntities()
            : base("name=dbWarehouseEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<C__MigrationHistory> C__MigrationHistory { get; set; }
        public virtual DbSet<Cable> Cables { get; set; }
        public virtual DbSet<Department> Departments { get; set; }
        public virtual DbSet<Log> Logs { get; set; }
        public virtual DbSet<Person> People { get; set; }
    }
}