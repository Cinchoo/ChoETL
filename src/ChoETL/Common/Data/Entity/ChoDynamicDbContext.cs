using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public partial class ChoDynamicDbContext : DbContext
    {
        public ChoDynamicDbContext()
            : base("name=ChoETLDynamicDbContext")
        {
            Database.SetInitializer(new NullDatabaseInitializer<ChoDynamicDbContext>());
        }

        public void AddTable(Type type)
        {
            _tables.Add(type.Name, type);
        }

        private Dictionary<string, Type> _tables = new Dictionary<string, Type>();

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var entityMethod = modelBuilder.GetType().GetMethod("Entity");
            MethodInfo method = modelBuilder.GetType().GetMethod("Entity");
            method = method.MakeGenericMethod(_tables.Values.ToArray());
            method.Invoke(modelBuilder, null);
            base.OnModelCreating(modelBuilder);

            //foreach (var table in _tables)
            //{
            //    entityMethod.MakeGenericMethod(table.Value).Invoke(modelBuilder, new object[] { });
            //    foreach (var pi in (table.Value).GetProperties())
            //    {
            //        if (pi.Name == "Id")
            //            modelBuilder.Entity(table.Value).HasKey(typeof(int), "Id");
            //        else
            //            modelBuilder.Entity(table.Value).StringProperty(pi.Name);
            //    }
            //}
        }
    }
}
