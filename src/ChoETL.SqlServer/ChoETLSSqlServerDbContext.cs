using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    internal class ChoETLSqlServerDbContext<T> : DbContext
    {
        public Action<string> Log { get; set; } = Console.WriteLine;

        public ChoETLSqlServerDbContext(string connectionString) :
            base(connectionString)
        {
            Database.Log = new Action<string>((m) =>
            {
                if (Log != null)
                    Log(m);
                else
                    ChoETLLog.Info(m);
            });
            Database.SetInitializer<ChoETLSqlServerDbContext<T>>(null);
        }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            typeof(T).ScanAndDefineKeyToEntity();

            MethodInfo method = modelBuilder.GetType().GetMethod("Entity");
            method = method.MakeGenericMethod(new Type[] { typeof(T) });
            method.Invoke(modelBuilder, null);
            base.OnModelCreating(modelBuilder);
        }
    }
}
