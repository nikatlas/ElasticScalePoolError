using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using Microsoft.WindowsAzure.Mobile.Service;
using Microsoft.WindowsAzure.Mobile.Service.Tables;
using System;
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;
using System.Configuration;
using System.Security.Principal;
using test1.DataObjects;

namespace test1.Models
{
    /// <summary>
    ///   Context needed for database Communication! 
    ///   Remember that it is Elastic Scale Connection which means that it connects to many databases!
    ///   
    ///     Multiple Constructors for easy database access! 
    ///     
    ///     Default constructon () accesses : 
    ///         I am leavning the default connection String MS_TableConncetionString and i leave the associated database as a common database.
    ///     
    ///     Multiple other constructors for Elastic Usage!
    /// </summary>
    public class esef<T> : DbContext
    {
        // Todo Check Split/Merge Elastic Scale 


        // You can add custom code to this file. Changes will not be overwritten.
        // 
        // If you want Entity Framework to alter your database
        // automatically whenever you change your model schema, please use data migrations.
        // For more information refer to the documentation:
        // http://msdn.microsoft.com/en-us/data/jj591621.aspx
        //
        // To enable Entity Framework migrations in the cloud, please ensure that the `
        // service name, set by the 'MS_MobileServiceName' AppSettings in the local 
        // Web.config, is the same as the service name when hosted in Azure.

        public static string schema = "contactshub";
        // The connection string needed for quick access to ElasticScale Sharding.
        public static string defaultConnectionString = ConfigurationManager.ConnectionStrings["ElasticScaleConnectionString"].ToString();
        /// <summary>
        ///     Default constructon () accesses : 
        ///         I am leavning the default connection String MS_TableConncetionString and i leave the associated database as a common database.
        /// </summary>
        public esef()
            : base(defaultConnectionString)
        {

        }

        #region ModelCreation not needed so frequently!

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {

            schema = "contactshub";// ServiceSettingsDictionary.GetSchemaName();
            if (!string.IsNullOrEmpty(schema))
            {
                modelBuilder.HasDefaultSchema(schema);
            }

            modelBuilder.Conventions.Add(
                new AttributeToColumnAnnotationConvention<TableColumnAttribute, string>(
                    "ServiceTableColumn", (property, attributes) => attributes.Single().ColumnType.ToString()));
            modelBuilder.Conventions.Add(new OneToManyCascadeDeleteConvention());
        }

        #endregion

        #region DataSets
        public virtual DbSet<TodoItem> Todos { get; set; }

        #endregion

        #region ElasticScale Needed

        /// <summary>
        ///     Creates a context that connects to the specified connection string!
        ///     Used internaly
        /// </summary>
        /// <param name="connectionString"></param>
        protected internal esef(string connectionString)
            : base(SetInitializerForConnection(connectionString))
        {
        }

        // Only static methods are allowed in calls into base class c'tors
        private static string SetInitializerForConnection(string connnectionString)
        {
            // We want existence checks so that the schema can get deployed
            Database.SetInitializer<esef<T>>(new DropCreateDatabaseIfModelChanges<esef<T>>());
            return connnectionString;
        }

        // C'tor for data dependent routing. This call will open a validated connection routed to the proper
        // shard by the shard map manager. Note that the base class c'tor call will fail for an open connection
        // if migrations need to be done and SQL credentials are used. This is the reason for the 
        // separation of c'tors into the DDR case (this c'tor) and the internal c'tor for new shards.
        /// <summary>
        ///     Actually opens a connection to the correct Shard based on the Shard Key on the specified ShardMap        /// 
        ///     Note As stated in the Microsoft's examples: 
        ///         C'tor for data dependent routing. This call will open a validated connection routed to the proper
        ///         shard by the shard map manager. Note that the base class c'tor call will fail for an open connection
        ///         if migrations need to be done and SQL credentials are used. This is the reason for the 
        ///         separation of c'tors into the DDR case (this c'tor) and the internal c'tor for new shards.
        ///     
        /// </summary>
        /// <param name="shardMap"> ShardMap responisble for the connection. By ShardMap we . Note:"I suggest for a Global Singleton that will be responsible for the whole Sharding Stuff!"</param>
        /// <param name="shardingKey"> The mapping key in order to determine the correct Shard! </param>
        /// <param name="connectionStr"> Connection string to the Server</param>
        public esef(ShardMap shardMap, T shardingKey, string connectionStr)
            : base(CreateDDRConnection(shardMap, shardingKey, connectionStr), true /* contextOwnsConnection */)
        {
        }

        // Only static methods are allowed in calls into base class c'tors
        private static DbConnection CreateDDRConnection(ShardMap shardMap, T shardingKey, string connectionStr)
        {
            // No initialization
            Database.SetInitializer<esef<T>>(new CreateDatabaseIfNotExists<esef<T>>());
            // Ask shard map to broker a validated connection for the given key
            SqlConnection conn = shardMap.OpenConnectionForKey<T>(shardingKey, connectionStr, ConnectionOptions.Validate);
            return conn;
        }

        /// <summary>
        ///     Regular custom c'tor for quick access. It uses the defaultConnectionStr which must be specified in the context class and in the WebConfig propaby!
        ///     
        /// </summary>
        /// <param name="shardMap">ShardMap responisble for the connection. Note:"I suggest for a Global Singleton that will be responsible for the whole Sharding Stuff!"</param>
        /// <param name="shardingKey">The mapping key in order to determine the correct Shard! </param>
        public esef(ShardMap shardMap, T shardingKey)
            : base(CreateDDRConnection(shardMap, shardingKey, defaultConnectionString), true /* contextOwnsConnection */)
        {

        }

        #endregion

    }
}
