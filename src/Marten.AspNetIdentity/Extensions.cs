using System;
using System.Security.Claims;
using JasperFx;
using JasperFx.MultiTenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Marten.AspNetIdentity
{
	public static class Extensions
	{
		/// <summary>
		/// Adds the stores to the service container, plus a IDocumentStore as a singleton, using the provided connection string.
		/// </summary>
		/// <typeparam name="TUser"></typeparam>
		/// <typeparam name="TRole"></typeparam>
		/// <param name="builder"></param>
		/// <param name="postgresConnectionString"></param>
		/// <returns></returns>
		public static IdentityBuilder AddMartenStores<TUser, TRole>(this IdentityBuilder builder, string postgresConnectionString)
													where TUser : IdentityUser, IClaimsUser
													where TRole : IdentityRole
		{
			IDocumentStore documentStore = CreateDocumentStore(postgresConnectionString);

			builder = builder
						  .AddRoleStore<MartenRoleStore<TRole>>()
						  .AddRoleManager<RoleManager<TRole>>()
						  .AddUserStore<MartenUserStore<TUser>>()
						  .AddUserManager<UserManager<TUser>>();

			builder.Services.AddSingleton<IDocumentStore>(documentStore);

			return builder;
		}

		public static IdentityBuilder AddMartenStores<TUser, TRole>(this IdentityBuilder builder)
												where TUser : IdentityUser, IClaimsUser
												where TRole : IdentityRole
		{
			return builder
						.AddRoleStore<MartenRoleStore<TRole>>()
						.AddRoleManager<RoleManager<TRole>>()
						.AddUserStore<MartenUserStore<TUser>>()
						.AddUserManager<UserManager<TUser>>();
		}

		public static IDocumentStore CreateDocumentStore(string connectionString, string databaseSchemaName = "aspnetidentity",
														 string databaseOwner = "aspnetidentity", int connectionLimit = -1)
		{
			var documentStore = DocumentStore.For(options =>
			{
				options.TenantIdStyle = TenantIdStyle.ForceLowerCase;
				options.AutoCreateSchemaObjects = AutoCreate.All;
				
				options.CreateDatabasesForTenants(c =>
				{
					c.ForTenant()
						.CheckAgainstPgDatabase()
						.WithOwner(databaseOwner)
						.WithEncoding("UTF-8")
						.ConnectionLimit(connectionLimit)
						.OnDatabaseCreated(_ =>
						{
							Console.WriteLine($"Postgres '{_.Database}' database created");
						});
				});

				options.DatabaseSchemaName = databaseSchemaName;
				options.Connection(connectionString);
				// options.Schema.For<IdentityUser>().Index(user => user.Id); Indexing seems to not correctly cast right now and is thus avoided
				// options.Schema.For<IdentityRole>().Index(role => role.Id);
				options.Schema.For<IdentityUser>();
				options.Schema.For<IdentityRole>();
			});

			return documentStore;
		}
	}
}