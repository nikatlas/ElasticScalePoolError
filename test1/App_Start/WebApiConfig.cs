using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Web.Http;
using test1.DataObjects;
using test1.Models;
using Microsoft.WindowsAzure.Mobile.Service;
using Microsoft.Azure.SqlDatabase.ElasticScale.ShardManagement;
using System.Data.SqlClient;
using System.Configuration;
using System.Linq;

namespace test1
{
    public static class WebApiConfig
    {
        public static ListShardMap<Guid> sm;
        public static void Register()
        {
            // Use this class to set configuration options for your mobile service
            ConfigOptions options = new ConfigOptions();

            // Use this class to set WebAPI configuration options
            HttpConfiguration config = ServiceConfig.Initialize(new ConfigBuilder(options));

            // To display errors in the browser during development, uncomment the following
            // line. Comment it out again when you deploy your service for production use.
            // config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;





            string ccstring = "User ID=test@f1ohphxyzj;Password=Micro123456;Trusted_Connection=False;Encrypt=True;Connection Timeout=30;";
            string csstring = "tcp:f1ohphxyzj.database.windows.net,1433";
            // Connection string with administrative credentials for the root database
            SqlConnectionStringBuilder connStrBldr = new SqlConnectionStringBuilder(ccstring);
            connStrBldr.DataSource = csstring;
            connStrBldr.InitialCatalog = "shardmap";

            ///*
            // Deploy shard map manager.
            ShardMapManager smm;
            if (!ShardMapManagerFactory.TryGetSqlShardMapManager(connStrBldr.ConnectionString, ShardMapManagerLoadPolicy.Eager, out smm))
            {
                smm = ShardMapManagerFactory.CreateSqlShardMapManager(connStrBldr.ConnectionString);
            }

            // Try Get Company Mapping
            
            if (!smm.TryGetListShardMap<Guid>("companyMapping", out sm)) // sm static for testcase on top

            {
                // Create Company Mapping
                sm = smm.CreateListShardMap<Guid>("companyMapping");
            }

            // Step 2

            SqlConnectionStringBuilder connStrBldr2 = new SqlConnectionStringBuilder(ccstring);
            connStrBldr2.DataSource = csstring;
            connStrBldr2.InitialCatalog = "shard0";

            Shard temp;
            if (!sm.TryGetShard(new ShardLocation(csstring, "shard0"), out temp))
            {
                temp = sm.CreateShard(new ShardLocation(csstring, "shard0"));
            }

            // Go into a DbContext to trigger migrations and schema deployment for the new shard.
            // This requires an un-opened connection.
            using (var db = new esef<Guid>(connStrBldr2.ConnectionString))
            {
                // Run a query to engage EF migrations
                (from b in db.Todos
                 select b).Count();
            }

            string server = csstring;
            string connstr = ccstring;
            //RegisterNewShard(csstring, "shard0", ccstring, "5A979B24-C746-11E4-85C0-78678ED529EF", sm);

            // Register the mapping of the tenant to the shard in the shard map.
            // After this step, DDR on the shard map can be used
            PointMapping<Guid> mapping;
            if (!sm.TryGetMappingForKey(new Guid("5A979B24-C746-11E4-85C0-78678ED529EF"), out mapping))
            {
                sm.CreatePointMapping(new Guid("5A979B24-C746-11E4-85C0-78678ED529EF"), temp);
            }

        }

        // Enter a new shard - i.e. an empty database - to the shard map, allocate a first tenant to it 
        // and kick off EF intialization of the database to deploy schema
        // public void RegisterNewShard(string server, string database, string user, string pwd, string appname, int key)
        // ShardMapping is used when there are multiple ShardMapping methods ( Multiple keys )
        public static void RegisterNewShard(string server, string database, string connstr, string key, ListShardMap<Guid> ShardMaping)
        {
            connstr = server;
            Shard shard;
            ShardLocation shardLocation = new ShardLocation(server, database);

            if (!ShardMaping.TryGetShard(shardLocation, out shard))
            {
                shard = ShardMaping.CreateShard(shardLocation);
            }

            SqlConnectionStringBuilder connStrBldr = new SqlConnectionStringBuilder(connstr);
            connStrBldr.DataSource = server;
            connStrBldr.InitialCatalog = database;

            // Go into a DbContext to trigger migrations and schema deployment for the new shard.
            // This requires an un-opened connection.
            using (var db = new esef<Guid>(connStrBldr.ConnectionString))
            {
                // Run a query to engage EF migrations
                (from b in db.Todos
                 select b).Count();
            }

            // Register the mapping of the tenant to the shard in the shard map.
            // After this step, DDR on the shard map can be used
            PointMapping<Guid> mapping;
            if (!ShardMaping.TryGetMappingForKey(new Guid(key), out mapping))
            {
                ShardMaping.CreatePointMapping(new Guid(key), shard);
            }
        }
    }
}

