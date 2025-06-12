# Package Uploader

This article describes how to automate uploading of game packages to Partner Center by using the Package Uploader tool.

You can download the latest version of the Package Uploader tool on GitHub [https://github.com/microsoft/PackageUploader](https://github.com/microsoft/PackageUploader).

This article covers the following:

* [Introduction](#introduction)
* [Prerequisites](#prerequisites)
* [Service creation and authentication](#service-creation-and-authentication)
* [Get the Package Uploader](#get-the-package-uploader)
* [Run the Package Uploader](#run-the-package-uploader)
* [Putting it all together](#putting-it-all-together)
* [Example GetProduct operation](#example-getproduct-operation)
* [Example UploadXvcPackage operation](#example-uploadxvcpackage-operation)
* [Example PublishPackages operation](#example-publishpackages-operation)
* [Xbox Game Package Manager](#xbox-game-package-manager)
* [Troubleshooting](#troubleshooting)
* [Q &amp; A](#q-and-a)

<a id="introduction"></a>

## Introduction

Package Uploader is a .NET 8.0-based cross-platform application and library that enables game developers to interact programmatically with Partner Center.

Package Uploader has a command-line tool and a dynamic linked library (DLL) that you can integrate into your build pipelines or other development workflows.

There is also a Windows desktop application called the Xbox Game Package Manager that provides a user-friendly graphical interface for Package Uploader and MakePkg. This tool simplifies both package creation and upload operations.

The current release of Package Uploader provides the following functionality.

* Retrieves metadata about a particular product
* Uploads a new Xbox XVC, UWP, or MSIXVC package
* Removes existing or previous packages
* Imports packages from one branch to another
* Publishes packages to a sandbox

<a id="prerequisites"></a>

## Prerequisites

Programmatic manipulation of packages for a particular product requires the following:

* The product must already exist in Partner Center. The tool currently doesn't support product creation.
* The branch for the target upload must already exist in Partner Center. The tool currently doesn't support branch creation.
* Access to create Azure application registrations in the Azure Active Directory (Azure AD) tenant connected to the target Partner Center account if you plan on setting up certificate-based or secret-based authorization.
* You only need a valid certificate if you plan on setting up certificate-based authorization.
* The Package Uploader executable.

<a id="service-creation-and-authentication"></a>

## Service creation and authentication for Azure AD applications

### Recommendation: Use Browser based authentication if not in a headless scenario

You can avoid service creation, secrets, and authentication when using the Package Uploader by using the 'Browser' authentication option.

1. Specify the '-a Browser' authentication method when running the Package Uploader.
2. Use '-t <TenantId>' if your account has access to multiple tenants. This will ensure that the correct tenant is used for authentication.

### Create an Azure AD application for authentication

1. Sign in to your company's [Azure portal](https://portal.azure.com) and select **Azure Active Directory**.<br>**NOTE:** Ensure that this is the same Azure AD that's linked to your Partner Center account.
2. Select **App registrations**.
3. Select **New registration**.
4. Enter a name for the application.
5. From the list of supported account types, select **Single tenant**.
6. Leave **Redirect URI** blank. It isn't required for this application.
7. Select **Register**.
8. Make note of the following information for your new application.
    * Application (client) ID
    * Directory (tenant) ID

You’ll use the Client ID and Directory ID that you’ve generated and noted for the remaining sections in this article. 

For more information, see [Quickstart: Register an app in the Microsoft identity platform](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app#register-an-application). 

### Setup authorization 

For headless scenarios, the tool supports app secrets or certificates. Perform one of the following procedures to setup authorization.

#### Create a secret key

1. Select **Certificates & secrets**.
2. In the **Client secrets** section, select **New client secret**.
3. Enter a description of the secret, and then specify a duration. When you're done, select **Add**.
4. After adding the client secret, the value of the secret key is displayed.<br>**NOTE:** Copy this value because you won't be able to retrieve the secret key later.

#### Register a certificate

1. Select **Certificates & secrets**.
2. Select **Upload certificate**.
3. Navigate to the certificate you want to use and select it for uploading.
4. After the certificate has uploaded you should see it in the table of certificates, showing its start date and expiry date..<br>**NOTE:** Copy the thumbprint because you will use it later.


### Grant the newly created service permission to the game product

1.  Sign into Partner Center as a user with Manager level account permissions.
2.  Select **Settings** (gear icon) in the top right, and then select **Account Settings**.
3.  In the left pane select **User Management** and then select **Azure AD Applications**.
4.  Select **Add Azure AD Application**, then click **Add Azure AD Application** again in the pop up that appears. Select the Azure application you created in the previous steps and then select **Next**.<br>**NOTE:** If your application doesn’t appear, ensure that you created it in the same tenant that’s linked to Partner Center.
5.  On the top tab, select **Customize permissions**.
6.  Find the product that you want to programmatically upload builds to. Grant your Azure application the **Read/Write** permission under **Publishing**. For more information, see [Add an Azure AD tenant to your account](https://docs.microsoft.com/partner-center/multi-tenant-account#add-an-azure-ad-tenant-to-your-account).

<a id="get-the-package-uploader"></a>

## Get the Package Uploader

To use the Package Uploader, you can download the latest executable from the [Releases page](https://github.com/microsoft/PackageUploader/releases/latest).<br>**NOTE:** No need to install the .NET Runtime to run the tool since it is included inside the binary.

Alternatively, you can also build it.

1. [Download .NET 8 SDK](https://dotnet.microsoft.com/en-us/download) or the latest version.
2. Open a powershell prompt, and then browse to the folder where you downloaded the Package Uploader.
3. Browse to the `src` folder and then run `./publish.win-x64.ps1`.
4. When it's built, PackageUploader.exe is in the `src\PackageUploader.Application\bin\Release\net8.0\win-x64\publish` directory.

<a id="run-the-package-uploader"></a>

## Run the Package Uploader

Important arguments for running the package uploader.

| Argument | Description |
| --- | ---|
| **Operation name** | The [operation](#available-operations) represents the action that you want to perform. It should match the name of the configuration file that you pass when running the tool.
| **Configuration file** | The configuration file contains all the information that's required to perform the intended action. It specifies your product information, file locations, and the tenant authorized to complete the action. Copy a template from the templates folder and fill in the required fields to ensure you have all the data necessary.
| **Client secret** | This is the alphanumeric value that you previously generated, which is required to authenticate with Partner Center and your product.

### Available operations

| Operation | Description |
| --- | ---|
| **[GetProduct](https://github.com/microsoft/PackageUploader/blob/main/Operations.md#GetProduct)** | Gets metadata for the product. This is useful for getting the productId, BigId, product name that's used in all configuration files. And a list of the BranchFriendlyNames and FlightNames of the product. |
| **[GetPackages](https://github.com/microsoft/PackageUploader/blob/main/Operations.md#GetPackages)** | Gets a list of the packages in a branch or flight. |
| **[UploadUwpPackage](https://github.com/microsoft/PackageUploader/blob/main/Operations.md#UploadUwpPackage)** | Uploads a UWP game package. |
| **[UploadXvcPackage](https://github.com/microsoft/PackageUploader/blob/main/Operations.md#UploadXvcPackage)** | Uploads a XVC game package and assets, including EKB, SubVal, and layout files. |
| **[RemovePackages](https://github.com/microsoft/PackageUploader/blob/main/Operations.md#RemovePackages)** | Removes game packages and assets from a branch. We recommend keeping only your 10 most recent packages to ensure optimal performance. |
| **[ImportPackages](https://github.com/microsoft/PackageUploader/blob/main/Operations.md#ImportPackages)** | Imports all game packages from a branch to a destination branch. This enables you to copy your previously uploaded and published packages from one branch to another. |
| **[PublishPackages](https://github.com/microsoft/PackageUploader/blob/main/Operations.md#PublishPackages)** | Publishes all game packages from a branch or flight to a destination sandbox or flight. You can set specific availability times in the configuration file. |

For more information on operation parameters, see [Operations](https://github.com/microsoft/PackageUploader/blob/main/Operations.md).

### Available parameters

| Parameter | Description |
| --- | ---|
| **-c, --ConfigFile <ConfigFile> (REQUIRED)** | The location of the configuration file |
| **-f, --ConfigFileFormat <Ini\|Json\|Xml>** | The format of the configuration file (default: Json) |
| **-s, --ClientSecret <ClientSecret>** | The client secret of the Azure AD application (only for AppSecret) |
| **-a, --Authentication <[Authentication method](#available-authentication-methods)>** | The authentication method (default: AppSecret) |
| **-t, --TenantId <TenantId>** | The tenant ID of the Azure AD application (only for Browser or CacheableBrowser authentication) |
| **-v, --Verbose** | Log verbose messages, such as HTTP calls |
| **-d, --Data** | Do not log on console and only return data (only for Get operations) |
| **-l, --LogFile <LogFile>** | The location of the log file |
| **-?, -h, --help** | Show Help and usage information |

<a id="available-authentication-methods"></a>

### Available Authentication methods

| Authentication method | Description |
| --- | ---|
| AppSecret | Uses a confidential client application to authenticate with Microsoft Entra using a client secret. |
| AppCert | Uses a confidential client application to authenticate with Microsoft Entra using a client certificate. |
| Default | Uses the Azure Identity DefaultAzureCredential method to authenticate with Microsoft Entra. Simplifies authentication by combining credentials. See Usage guidance for [DefaultAzureCredential](https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/credential-chains?tabs=dac#defaultazurecredential-overview). |
| Browser | Uses the Azure Identity InteractiveBrowserCredential method to authenticate with Microsoft Entra. A TokenCredential implementation which launches the system default browser to interactively authenticate a user, and obtain an access token. The browser will only be launched to authenticate the user once, then will silently acquire access tokens through the users refresh token as long as it's valid. |
| CacheableBrowser | Uses the Azure Identity InteractiveBrowserCredential method to authenticate with Microsoft Entra. A TokenCredential implementation which launches the system default browser to interactively authenticate a user, and obtain an access token. The browser will only be launched to authenticate the user once, then will silently acquire access tokens through the users refresh token as long as it's valid. This authentication method caches the access token for later use. |
| AzureCli | Uses the Azure Identity AzureCliCredential method to authenticate with Microsoft Entra. If the user is authenticated to Azure using Azure CLI's az login command, authenticate the app to Azure using that same account. |
| ManagedIdentity | Uses the Azure Identity ManagedIdentityCredential method to authenticate with Microsoft Entra. Attempts authentication using a managed identity that has been assigned to the deployment environment. This authentication type works for all Azure-hosted environments that support managed identity. More information about configuring managed identities can be found [here](https://learn.microsoft.com/entra/identity/managed-identities-azure-resources/overview). |
| Environment | Uses the Azure Identity EnvironmentCredential method to authenticate with Microsoft Entra. Enables authentication to Microsoft Entra ID using a client secret or certificate, or as a user with a username and password, using environment variables. Order and environment variables can be found [here](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.environmentcredential). |
| AzurePipelines | Uses the Azure Identity AzurePipelinesCredential method to authenticate with Microsoft Entra. Credential which authenticates using an Azure Pipelines service connection. For usage instructions, see [Authenticating in Azure Pipelines with service connections](https://aka.ms/azsdk/net/identity/azurepipelinescredential/usage). |
| ClientSecret | Uses the Azure Identity ClientSecretCredential method to authenticate with Microsoft Entra. Enables authentication to Microsoft Entra ID using a client secret that was generated for an App Registration. More information on how to configure a client secret can be found [here](https://learn.microsoft.com/entra/identity-platform/quickstart-configure-app-access-web-apis#add-credentials-to-your-web-application). |
| ClientCertificate | Uses the Azure Identity ClientCertificateCredential method to authenticate with Microsoft Entra. Enables authentication of a service principal to Microsoft Entra ID using a X509 certificate that is assigned to its App Registration. More information on how to configure certificate authentication can be found [here](https://learn.microsoft.com/entra/identity-platform/certificate-credentials#register-your-certificate-with-microsoft-identity-platform). |

<a id="putting-it-all-together"></a>

## Putting it all together

1. Open PowerShell by using the **Start** menu.
2. Browse to the root of your wrapper directory, and then run the following command:<br>
`.\PackageUploader.exe <OperationName> -c <ConfigFile> -a <AuthenticationMethod>`

## Operation config file creation

To perform operations, a configuration file is required. Configuration files can be set with different values, some of which may be mandatory. For example, certain operations can use either `productId` or `bigId`, but the program will require at least one of them.

For full documentation on each property of each operation, please refer to the [operations documentation](https://github.com/microsoft/PackageUploader/blob/main/Operations.md).

## Example GetProduct operation
   
**NOTE:** When using a certificate you need to include a few more values in the aadAuthInfo section. `certificateStore` represents the certificate location in the store on the machine. For information on other supported values, see [StoreName Enum (System.Security.Cryptography.X509Certificates](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.storename?view=net-8.0). <br>`certificateLocation` can be set to `LocalMachine` or `CurrentUser`.
   
### Example GetProduct configuration file using certificate authorization

```json
{
  "operationName": "GetProduct",

  "bigId": "9FAKEBIGID"
}
```

### Example running GetProduct operation

```
.\PackageUploader.exe GetProduct -c .\GetProduct.json -a Browser
```

### Example GetProduct operation output

```json
Product: {
  "bigId":"9FAKEBIGID",
  "productName":" Test product (Hidden)",
  "branchFriendlyNames": ["Main", "Branch1", "Branch2", "Branch3"],
  "flightNames": ["Flight 1", "Flight 2", "Flight 3"]
}
```

<a id="example-uploadxvcpackage-operation"></a>

## Example UploadXvcPackage operation

### Example UploadXvcPackage configuration file using certificate authentication

**NOTE:** When using a certificate you need to include a few more values in the aadAuthInfo section. `certificateStore` represents the certificate location in the store on the machine. For information on other supported values, see [StoreName Enum (System.Security.Cryptography.X509Certificates](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.storename?view=net-8.0). <br>`certificateLocation` can be set to `LocalMachine` or `CurrentUser`.

```json
{
  "operationName": "UploadXvcPackage",

  "bigId": "9FAKEBIGID ",

  "branchFriendlyName": "Main",

  "marketGroupName": "default",
  
  "deltaUpload": false,

  "packageFilePath": "C:\\Users\\someone\\Desktop\\StubPackage\\Builds\\TestPublisher.SomeDemoProductName_0.9.1.0_x64__fjtqkg6rpm1hy.msixvc",
  "gameAssets": {
    "ekbFilePath": "C:\\Users\\someone\\Desktop\\StubPackage\\Builds\\ TestPublisher.SomeDemoProductName_0.9.1.0_x64__fjtqkg6rpm1hy_Full_33ec8436-5a0e-4f0d-b1ce-3f29c3955039.ekb",
    "subValFilePath": "C:\\Users\\someone\\Desktop\\StubPackage\\Builds\\Validator_ TestPublisher.SomeDemoProductName_0.9.1.0_x64__fjtqkg6rpm1hy.xml",
    "symbolsFilePath": "",
    "discLayoutFilePath": ""
  },
  "minutesToWaitForProcessing": 60,

  "availabilityDate": {
    "isEnabled": false,
    "effectiveDate": ""
  },

  "uploadConfig": {
    "httpTimeoutMs": 5000,
    "httpUploadTimeoutMs": 300000,
    "maxParallelism": 24,
    "defaultConnectionLimit": -1,
    "expect100Continue": false,
    "useNagleAlgorithm": false
  }
} 
```

### Example running UploadXvcPackage operation

```
.\PackageUploader.exe UploadXvcPackage -c .\UploadXvcPackage.json -a Browser
```

### Example UploadXvcPackage operation output

```
2021-10-22 17:11:36.965 info: PackageUploader.Application.Operations.UploadXvcPackageOperation[0] Starting UploadXvcPackage operation.
2021-10-22 17:11:42.387 info: PackageUploader.ClientApi.Client.Ingestion.IngestionHttpClient[0] Package id: 11931042-2f6c-4ccb-8c53-fccbe6760d67
2021-10-22 17:11:43.553 info: PackageUploader.ClientApi.Client.Xfus.XfusUploader[0] XFUS Asset Initialized. Will upload TestPublisher.SomeDemoProductName_0.9.1.0_x64__fjtqkg6rpm1hy.msixvc at size of 10.12MB.
2021-10-22 17:11:43.553 info: PackageUploader.ClientApi.Client.Xfus.XfusUploader[0] Upload 0% complete.
2021-10-22 17:11:45.889 info: PackageUploader.ClientApi.Client.Xfus.XfusUploader[0] Upload 33% complete.
2021-10-22 17:11:46.129 info: PackageUploader.ClientApi.Client.Xfus.XfusUploader[0] Upload 67% complete.
2021-10-22 17:11:46.813 info: PackageUploader.ClientApi.Client.Xfus.XfusUploader[0] Upload 100% complete.
2021-10-22 17:11:47.901 info: PackageUploader.ClientApi.Client.Xfus.XfusUploader[0] TestPublisher.SomeDemoProductName_0.9.1.0_x64__fjtqkg6rpm1hy.msixvc Upload complete in: (HH:MM:SS) 00:00:05.
2021-10-22 17:11:50.874 info: PackageUploader.ClientApi.PackageUploaderService[0] Package is uploaded and is in processing.
2021-10-22 17:11:57.015 info: PackageUploader.ClientApi.PackageUploaderService[0] Will wait 60 minute(s) for package processing, checking every 1 minute(s).
2021-10-22 17:11:57.015 info: PackageUploader.ClientApi.PackageUploaderService[0] Package processed.
2021-10-22 17:12:00.024 info: PackageUploader.ClientApi.Client.Xfus.XfusUploader[0] XFUS Asset Initialized. Will upload TestPublisher.SomeDemoProductName_0.9.1.0_x64__fjtqkg6rpm1hy_Full_33ec8436-5a0e-4f0d-b1ce-3f29c3955039.ekb at size of 467B.
2021-10-22 17:12:00.024 info: PackageUploader.ClientApi.Client.Xfus.XfusUploader[0] Upload 0% complete.
2021-10-22 17:12:00.757 info: PackageUploader.ClientApi.Client.Xfus.XfusUploader[0] Upload 100% complete.
2021-10-22 17:12:01.964 info: PackageUploader.ClientApi.Client.Xfus.XfusUploader[0] TestPublisher.SomeDemoProductName_0.9.1.0_x64__fjtqkg6rpm1hy_Full_33ec8436-5a0e-4f0d-b1ce-3f29c3955039.ekb Upload complete in: (HH:MM:SS) 00:00:02.
2021-10-22 17:12:03.978 warn: PackageUploader.ClientApi.PackageUploaderService[0] No SymbolsZip asset file path provided, will continue to upload Package on its own.
2021-10-22 17:12:06.524 info: PackageUploader.ClientApi.Client.Xfus.XfusUploader[0] XFUS Asset Initialized. Will upload Validator_ TestPublisher.SomeDemoProductName_0.9.1.0_x64__fjtqkg6rpm1hy.xml at size of 6.9KB.
2021-10-22 17:12:06.524 info: PackageUploader.ClientApi.Client.Xfus.XfusUploader[0] Upload 0% complete.
2021-10-22 17:12:07.264 info: PackageUploader.ClientApi.Client.Xfus.XfusUploader[0] Upload 100% complete.
2021-10-22 17:12:08.377 info: PackageUploader.ClientApi.Client.Xfus.XfusUploader[0] Validator_TestPublisher.SomeDemoProductName_0.9.1.0_x64__fjtqkg6rpm1hy.xml Upload complete in: (HH:MM:SS) 00:00:02.
2021-10-22 17:12:09.872 warn: PackageUploader.ClientApi.PackageUploaderService[0] No DiscLayoutFile asset file path provided, will continue to upload Package on its own.
2021-10-22 17:12:09.872 info: PackageUploader.Application.Operations.UploadXvcPackageOperation[0] Uploaded package with id: 11931042-2f6c-4ccb-8c53-fccbe6760d67
2021-10-22 17:12:13.811 info: PackageUploader.Application.Operations.UploadXvcPackageOperation[0] Availability date set
2021-10-22 17:12:13.812 info: PackageUploader.Application.Operations.UploadXvcPackageOperation[0] PackageUploader has finished running.
```
<a id="example-publishpackages-operation"></a>

## Example PublishPackages operation

### Example PublishPackages configuration file

```json
{
  "operationName": "PublishPackages",

  "bigId": "9FAKEBIGID",
  
  "branchFriendlyName": "Main",
  "destinationSandboxName": "QXNKBL.1",

  "minutesToWaitForPublishing": 60,
  
  "publishConfiguration": {
    "releaseTime": "",
    "isManualPublish" : false,
    "certificationNotes": "No Notes for CERT at this time"
  }
} 
```

### Example running PublishPackages operation

```
.\PackageUploader.exe PublishPackages -c .\PublishPackages.json -s superlongsecretalphannumericstring
Example PublishPackages operation output
2021-10-22 17:28:47.315 info: PackageUploader.Application.Operations.PublishPackagesOperation[0] Starting PublishPackages operation.
2021-10-22 17:29:03.399 info: PackageUploader.ClientApi.PackageUploaderService[0] Package still in publishing, waiting another 1 minute. Will wait a further 60 minute(s) after this.
2021-10-22 17:30:04.375 info: PackageUploader.ClientApi.PackageUploaderService[0] Package still in publishing, waiting another 1 minute. Will wait a further 59 minute(s) after this.
2021-10-22 17:31:05.698 info: PackageUploader.ClientApi.PackageUploaderService[0] Package still in publishing, waiting another 1 minute. Will wait a further 58 minute(s) after this.
2021-10-22 17:32:07.109 info: PackageUploader.ClientApi.PackageUploaderService[0] Package still in publishing, waiting another 1 minute. Will wait a further 57 minute(s) after this.
2021-10-22 17:33:08.008 info: PackageUploader.ClientApi.PackageUploaderService[0] Package still in publishing, waiting another 1 minute. Will wait a further 56 minute(s) after this.
2021-10-22 17:34:08.661 info: PackageUploader.ClientApi.PackageUploaderService[0] Package still in publishing, waiting another 1 minute. Will wait a further 55 minute(s) after this.
2021-10-22 17:35:09.339 info: PackageUploader.ClientApi.PackageUploaderService[0] Package still in publishing, waiting another 1 minute. Will wait a further 54 minute(s) after this.
2021-10-22 17:36:10.341 info: PackageUploader.ClientApi.PackageUploaderService[0] Package still in publishing, waiting another 1 minute. Will wait a further 53 minute(s) after this.
2021-10-22 17:37:11.342 info: PackageUploader.ClientApi.PackageUploaderService[0] Package still in publishing, waiting another 1 minute. Will wait a further 52 minute(s) after this.
2021-10-22 17:38:11.981 info: PackageUploader.ClientApi.PackageUploaderService[0] Package still in publishing, waiting another 1 minute. Will wait a further 51 minute(s) after this.
2021-10-22 17:39:14.187 info: PackageUploader.ClientApi.PackageUploaderService[0] Game published.
2021-10-22 17:39:14.188 info: PackageUploader.Application.Operations.PublishPackagesOperation[0] PackageUploader has finished running.
```
<a id="xbox-game-package-manager"></a>

## Xbox Game Package Manager

The Xbox Game Package Manager is a Windows desktop application that provides a user-friendly graphical interface for Package Uploader and MakePkg. This tool simplifies both package creation and upload operations with the following key features:

### Package Creation
- **GDK Integration**: Leverages your installed Xbox Game Development Kit (GDK) to create game packages directly from loose game files
- **Simple Configuration**: Point to the folder containing your MicrosoftGame.config file and configure settings
- **Custom Layout Support**: Option to specify a custom layout XML file for advanced packaging scenarios
- **Output Directory Selection**: Choose where the packaged files will be saved

### Package Upload
- **Streamlined Workflow**: Select your package, branch, and market group in a simple interface
- **Status Monitoring**: Track upload progress in real-time with visual indicators
- **Browser-based Authentication**: Authenticate seamlessly through your default browser
- **Multi-tenant Support**: Advanced option to specify a tenant ID if your account has access to multiple tenants

### Benefits
- **Smart Packaging**: Automatically analyzes your config and loose files to optimize packaging, with manual override options when needed.
- **No Configuration Files**: Create and upload packages without writing JSON configuration files
- **Visual Progress**: Monitor packaging and upload progress through a visual interface
- **Simplified Authentication**: Uses browser-based authentication to eliminate the need for client secrets or certificates
- **Error Handling**: Provides clear error messages and troubleshooting guidance through the UI

To use the Xbox Game Package Manager, download the latest version from the [Releases page](https://github.com/microsoft/PackageUploader/releases/latest) and run the XboxGamePackageManager.exe application.

<a id="troubleshooting"></a>

## Troubleshooting
In some cases, Package Uploader might not work as you intended. Here's a list of some of the most common errors that users have encountered while using Package Uploader, and their possible solutions. A best practice for debugging is to open the folder where the tool runs from and your configuration files are saved. Open the specific configuration file that you've selected to ensure you're not using the blank template.

**NOTE:** A schema is available for validating configuration files in Visual Studio or any tool of your choice. The schema is located at [https://product-ingestion-int.azureedge.net/schema/package-uploader-operation-configuration/2021-11-30](https://product-ingestion-int.azureedge.net/schema/package-uploader-operation-configuration/2021-11-30).

* `Exception: System.Net.Http.HttpRequestException: Response status code does not indicate success: 401`<br>
You haven't correctly setup your application authentication. Ensure that you've added your application to the Azure AD Applications section with the appropriate access.

* `Invalid client secret is provided.`<br>
Check the format of your client secret because it doesn't match the values specified for your ClientId and TenetId.

* `Product with BigId 'SomeBigID' not found`<br>
Ensure that you've entered your BigID or StoreId correctly.

* `'OperationName' with the error: 'OperationName field is not GetProduct.'`<br>
You've specified an unsupported operation. Check the list of supported operations and ensure you've entered the operation correctly.

* `Check to make sure you have the correct tenant ID and are signing into the correct cloud.`<br>
Either your ClientId or TenantId are incorrect in your configuration file. Ensure that you're using the correct configuration file and that the IDs are correct.

* `Unhandled exception: System.FormatException: Could not parse the JSON file. ---> System.Text.Json.JsonReaderException: 'U' is an invalid escapable character within a JSON string. The string should be correctly escaped
`<br>
You must escape all backslashes and parentheses in JSON with a backslash, for example: `C:\\`.

* `Failed to publish.`<br>
Ensure your destination branch or sandbox already exists. If not, initialize it via the Partner Center portal and try again 

* `Failed to publish. - You can only have one MSIXVC package per market group.`<br>
You have uploaded two packages to the same branch. Use the Partner Center portal to delete all extra packages for the given market group.

<a id="q-and-a"></a>

## Q &amp; A

**Q:** Can I use one Azure app name for multiple products?<br>
**A:** No. We're currently working with the Partner Center team to make this possible. For now, each product you upload requires the creation of a unique Azure AD application.

**Q:** Are delta uploads supported?<br>
**A:** Yes! For XVC and MSIXVC packages, delta upload will work the same as uploading direct in to Partner Center. Consult your DAM for how this works with your product and packages. UWP unfortunately is not supported and will not be in the future either.

**Q:** Can I upload multiple packages for the same product at the same time?<br>
**A:** No. You'll need to upload packages one at a time.

**Q:** Can I source my builds off another machine?  
**A:** No. At this time the tool only supports local file uploads and publishing. <br>

**Q:** Can I use the Package Uploader to automate and update other parts of Partner Center besides uploads?<br>
**A:** No. This isn't currently available. The scope was only uploads and publishing. The API teams are currently working to expand this to automate other parts of Partner Center.

**Q:** Can I use the Package Uploader for an application that isn't a game?<br>
**A:** No. This tool only works for games and cannot be used for apps.

**Q:** If I want to change how the wrapper works who do I reach out to?<br>
**A:** It's completely open source. You can change the wrapper as you want. Use, adjust, and contribute on GitHub!

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
