using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Umbraco.Core;
using UmbracoIdentity;
using UmbracoIdentity.Models;

namespace Our.UmbracoIdentity.Extensions
{
    /// <summary>
    /// ExternalLoginStore that uses Azure Table Storage
    /// </summary>
    public class AzureTableStorageExternalLoginStore : DisposableObject, IExternalLoginStore
    {
        private static string externalLoginsTableName = GetTableName();
        public void DeleteUserLogins(int memberId)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.ConnectionStrings["AzureTableStorageExternalLoginStoreConnectionString"].ConnectionString);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the "ExternalLogins" table.
            CloudTable table = tableClient.GetTableReference(externalLoginsTableName);
            table.CreateIfNotExists();

            // clear out logins for member
            TableQuery<ExternalLoginEntity> query = new TableQuery<ExternalLoginEntity>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, memberId.ToString()));

            var oldLogins = table.ExecuteQuery(query);
            if (oldLogins.Any())
            {
                TableBatchOperation batchOperation = new TableBatchOperation();
                foreach (ExternalLoginEntity login in oldLogins)
                {
                    batchOperation.Delete(login);
                }
                table.ExecuteBatch(batchOperation);
            }
        }

        public IEnumerable<int> Find(UserLoginInfo login)
        {
            var userIds = new List<int>();

            // set up Table Storage
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.ConnectionStrings["AzureTableStorageExternalLoginStoreConnectionString"].ConnectionString);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the "ExternalLogins" table.
            CloudTable table = tableClient.GetTableReference(externalLoginsTableName);
            table.CreateIfNotExists();

            // clear out logins for member
            // NOTE: this may not be efficient at scale because without using the partitionkey, it must do a full table scan. Maybe add an index table?
            TableQuery<ExternalLoginEntity> query = new TableQuery<ExternalLoginEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("ProviderKey", QueryComparisons.Equal, login.ProviderKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("LoginProvider", QueryComparisons.Equal, login.LoginProvider)
                )
            );

            var logins = table.ExecuteQuery(query);
            if (logins.Any())
            {
                foreach (var l in logins)
                {
                    userIds.Add(l.UserId); // should I check if the id is already there?
                }
            }

            return userIds;
        }

        public IEnumerable<IdentityMemberLogin<int>> GetAll(int userId)
        {
            var found = new List<IdentityMemberLogin<int>>();

            // set up Table Storage
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.ConnectionStrings["AzureTableStorageExternalLoginStoreConnectionString"].ConnectionString);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the "ExternalLogins" table.
            CloudTable table = tableClient.GetTableReference(externalLoginsTableName);
            table.CreateIfNotExists();

            TableQuery<ExternalLoginEntity> query = new TableQuery<ExternalLoginEntity>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userId.ToString()));

            var logins = table.ExecuteQuery(query);
            if (logins.Any())
            {
                TableBatchOperation batchOperation = new TableBatchOperation();
                foreach (ExternalLoginEntity login in logins)
                {
                    found.Add(new IdentityMemberLogin<int> { LoginProvider = login.LoginProvider, ProviderKey = login.ProviderKey, UserId = userId });
                }
            }

            return found;
        }

        public void SaveUserLogins(int memberId, IEnumerable<UserLoginInfo> logins)
        {
            // set up Table Storage
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.ConnectionStrings["AzureTableStorageExternalLoginStoreConnectionString"].ConnectionString);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the "ExternalLogins" table.
            CloudTable table = tableClient.GetTableReference(externalLoginsTableName);
            table.CreateIfNotExists();

            // clear out logins for member
            TableQuery<ExternalLoginEntity> query = new TableQuery<ExternalLoginEntity>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, memberId.ToString()));

            var oldLogins = table.ExecuteQuery(query);
            if (oldLogins.Any())
            {
                TableBatchOperation batchOperation = new TableBatchOperation();
                foreach (ExternalLoginEntity login in oldLogins)
                {
                    batchOperation.Delete(login);
                }
                table.ExecuteBatch(batchOperation);
            }

            // add them all
            foreach (var l in logins)
            {
                var externalLogin = new ExternalLoginEntity(memberId, l.LoginProvider, l.ProviderKey);
                TableOperation insertOperation = TableOperation.Insert(externalLogin);
                table.Execute(insertOperation);
            }
        }

        protected override void DisposeResources()
        {

        }

        private static string GetTableName()
        {
            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["AzureTableStorageExternalLoginStoreTableName"]))
                return "ExternalLogins";
            else
                return ConfigurationManager.AppSettings["AzureTableStorageExternalLoginStoreTableName"];
        }
    }

    public class ExternalLoginEntity : TableEntity
    {
        public ExternalLoginEntity() { }

        public ExternalLoginEntity(int userId, string loginProvider, string providerKey)
        {
            this.PartitionKey = userId.ToString();
            this.RowKey = Guid.NewGuid().ToString();
            this.UserId = userId;
            this.LoginProvider = loginProvider;
            this.ProviderKey = providerKey;
        }

        public int UserId { get; set; }
        public string LoginProvider { get; set; }
        public string ProviderKey { get; set; }
    }
}
