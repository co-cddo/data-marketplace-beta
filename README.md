# CDDO Data Marketplace - UI and Catalogue API

<!-- [![Join us on Slack](https://img.shields.io/badge/slack-chat-green.svg?logo=slack)](https://TBC)
![Contributors](https://img.shields.io/badge/contributors-11-blue?logo=github)
![Issues](https://img.shields.io/badge/issues-9_open-purple)
[![Licence](https://img.shields.io/badge/license-MIT-orange)](http://choosealicense.com/licenses/mit/) -->

## Contribution Guidelines

The contribution guidelines are as per the guide [HERE](./readme/CONTRIBUTING.md). Please read before continuing with your development setup.

## Description

The CDDO Data Marketplace is a website service which enables the cataloguing of data assets owned by governnent organisations, the search and discovery of data assets by users trying to find data, and the management of requests for data assets.

CDDO Data Marketplace implments the [Data Catalogue Metadata](https://github.com/co-cddo/data-catalogue-metadata) profile as its underlying data asset model, and has a dependency on the [GOV.UK Single Sign On](https://sso.service.security.gov.uk/) service to manage authentication.

The Data Marketplace solution is split between the following repositories:

- [cddo.ui](https://github.com/co-cddo/data-marketplace-beta), which contains:
  - UI microservice
  - Catalogue microservice
- [cddo.users](https://github.com/co-cddo/data-marketplace-beta-users), which contains:
  - Users microservice
- [cddo.datashare](https://github.com/co-cddo/data-marketplace-beta-datashare) which contains:
  - Datashare microservice

The Data Marketplace has the following runtime architecture:

```mermaid
  
   architecture-beta
    group dm(cloud)[Data Marketplace]

    group focus in dm
    service ui(server)[UI microservice] in focus
    service cat(server)[Catalogue microservice] in focus
    service users(server)[Users microservice] in dm
    service ds(server)[Datashare microservice] in dm

    ui:B --> T:cat
    ui:L --> R:users
    ui:R --> L:ds
```

## Local development environment setup

This section contains instructions which should only have to be performed once, ever. 

### IDE setup 

Although you can use a different IDE, we recommend installing the latest version of [Microsoft Visual Studio Community Edition](https://visualstudio.microsoft.com/vs/community/) to run the solution.  

Once you have installed Visual Studio, add a package source in the NuGet Package Manager pointing to the following URL:

    https://pkgs.dev.azure.com/agrimetrics-projects/_packaging/agm-intellectual-property%40Local/nuget/v3/index.json

![NuGet package sources](./readme/package%20source.png) 

Next, open the Exception Settings window (Debug -> Windows -> Exception Settings) and select the checkbox labeled "Common Language Runtime Exceptions". 

![Exception settings](./readme/exception%20settings.png) 

### Local machine set up

Configure a local certificate to manage redirects from UK Government Security. To do this, run the following command from Powershell:

``` sh
$cert = New-SelfSignedCertificate -DnsName "dev.datamarketplace.gov.uk" -CertStoreLocation "cert:\LocalMachine\My" -KeyUsage DigitalSignature -KeyExportPolicy Exportable -NotAfter (Get-Date).AddYears(1)
$pwd = ConvertTo-SecureString -String "<strong-password>" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "C:\Users\<username>\Documents\dev-cddo.pfx" -Password $pwd
```

Next, open your hosts file (usually found in the folder `C:\Windows\System32\drivers\etc`) and add the following line to the bottom of it:

    127.0.0.1 dev.datamarketplace.gov.uk

![hosts file](./readme/hosts.png) 

## UI and Catalogue API configuration  

This section contains instructions on configuring the `Cddo.Data.Marketplace` solution so that it can be run locally. 

This section assumes you have completed all the steps in *Local development environment setup*.

### Local settings

Take a copy of the file [here](./Cddo.Data.Marketplace.UI/appsettings.json) and rename it to `../Cddo.Data.Marketplace.UI/appsettings.Development.json`.

Open the file and update any of the placeholder values to their actual runtime values. 

Next, take a copy of the file [here](./Cddo.Data.Marketplace.Api/appsettings.json) and rename it to `../Cddo.Data.Marketplace.Api/appsettings.Development.json`.

Open the file and update any of the placeholder values to their actual runtime values. 

### Certificate management for local execution 

Next, open the file `\cddo-ui\Cddo.Data.Marketplace.UI\Program.cs`, uncomment the following lines of code, and change the placeholder (denoted by angled brackets) with the thumbprint of the local certificate you generated in *Local machine set up* above: 

``` csharp
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(System.Net.IPAddress.Any, 443, listenOptions =>
    {
        listenOptions.UseHttps(httpsOptions =>
        {
            httpsOptions.ServerCertificate = LoadCertificateFromStore("<certificate thumbprint>");
        });
    });
});

X509Certificate2 LoadCertificateFromStore(string thumbprintOrSubjectName)
{
    using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
    {
        store.Open(OpenFlags.ReadOnly);
        var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprintOrSubjectName, validOnly: false);
        if (certificates.Count > 0)
        {
            return certificates[0];
        }
        else
        {
            throw new FileNotFoundException($"Certificate not found for {thumbprintOrSubjectName}.");
        }
    }
}
```
Next, set the following projects to start with the debug target of "http" (right click solution -> Configure Startup Projects):

- Cddo.Data.Marketplace.UI
- Cddo.Data.Marketplace.Api

![Configure Startup Projects in Visual Studio](./readme/startup.png)

## Running the solution in your local development environment

Now run the solution!

Assuming there are no errors on startup you should be able to see the Catalogue API swagger page on local port 7276

![Catalogue API swagger page](./readme/swagger.png)

Now open a browser window and navigate to http://dev.datamarketplace.gov.uk and you should see the Data Marketplace user interface.  

![Catalogue API swagger page](./readme/web.png)

You can then sign in and make sure the redirect from security.gov.uk comes back.

![Catalogue API swagger page](./readme/web2.png)











