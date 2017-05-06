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
        public ChoETLSqlServerDbContext(string connectionString) :
            base(connectionString)
        {
            Database.Log = new Action<string>((m) =>
            {
                if (ChoETLFramework.TraceSwitch.TraceInfo)
                    Console.WriteLine(m);
                ChoETLLog.Info(m);
            });
            Database.SetInitializer<ChoETLSqlServerDbContext<T>>(null);
        }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            MethodInfo method = modelBuilder.GetType().GetMethod("Entity");
            method = method.MakeGenericMethod(new Type[] { typeof(T) });
            method.Invoke(modelBuilder, null);
            base.OnModelCreating(modelBuilder);
        }
    }


}
