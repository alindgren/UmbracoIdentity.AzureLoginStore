# Azure Table Storage External Login Store for ASP.NET Identity for Umbraco

An external login store for [ASP.NET Identity for Umbraco](https://github.com/Shazwazza/UmbracoIdentity).

The [default external login store](https://github.com/Shazwazza/UmbracoIdentity/blob/master/src/UmbracoIdentity/ExternalLoginStore.cs) for ASP.NET Identity for Umbraco uses SQL CE.  This project provides an alternative external login store that uses [Azure Table Storage](https://azure.microsoft.com/en-us/services/storage/tables/).

## Installation and Usage

Install via nuget:

`Install-Package UmbracoIdentity.AzureLoginStore`

Note that the latest build is available at https://www.myget.org/feed/alexlindgren/package/nuget/UmbracoIdentity.AzureLoginStore

You will need an [Azure Table Storage](https://azure.microsoft.com/en-us/services/storage/tables/) account.

Add a connection string called `AzureTableStorageExternalLoginStoreConnectionString` in the `web.config` for the Azure Table Storage account.

UmbracoIdentity needs to be configured to use this package as it's External Login Store. This is done by changing how the `app.ConfigureUserManagerForUmbracoMembers` method is called in the `ConfigureServices` method of `UmbracoIdentityStartup` as follows:

```csharp
ApplicationContext appContext = ApplicationContext.Current;
var membershipProvider = Membership.Providers["UmbracoMembershipProvider"] as IdentityEnabledMembersMembershipProvider;
AzureTableStorageExternalLoginStore externalLoginStore = new AzureTableStorageExternalLoginStore();
UmbracoMembersUserStore<UmbracoApplicationMember> customUserStore = new UmbracoMembersUserStore<UmbracoApplicationMember>(appContext.Services.MemberService, appContext.Services.MemberTypeService, appContext.Services.MemberGroupService, membershipProvider, externalLoginStore);
app.ConfigureUserManagerForUmbracoMembers<UmbracoApplicationMember>(customUserStore, appContext, null);
```