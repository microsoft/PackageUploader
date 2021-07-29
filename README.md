# Project GameStoreBroker

A .NET 5.0 cross platform application and library that enables game developers to upload Xbox and PC game packages to [Partner Center](https://partner.microsoft.com/).

The application can be used directly with the input config for ease of use, or the API library could be consumed by your own application.

## Prerequisite

- Download and install the .NET 5.0 Runtime. Go here https://dotnet.microsoft.com/download/dotnet/5.0/runtime for download and installation.
- The product must already exist in partner center before uploading.
- The branch/sandbox must already exist in partner center before uploading.
- Build the solution from source, or download a release.
- The product needs to be published at least once manually through UX.

## Step 1: Creating your app in Azure.

- Go to https://portal.azure.com and log in with your Azure account.
- Under Azure services find _App registrations_.
- Under _App registrations_ click _New registration_.
  - Enter in your _name_.
  - Choose your _Supported account types_.
  - Click _Register_ at the bottom of the page.
- Under your newly created App navigate to _Client credentials_ and create a _New client secret_. Note the _clientID_, _tenantID_, and your _Secret key_ for future use.
- It is recommended that you add your team as backup to maintain your app going forward.
  - Navigate into the app, on the left under _manage_ find _owners_ and _add_ your back up.
  
## Step 2: Add your app in Partner Center and give it the proper permissions.

- Log in to Partner Center with a manager level account. 
- Click on the _cogs_ wheel on the top right of the page and navigate to _Account settings_.
- On the left navigation pane click on _User management_ then _Azure AD applications_.
- Click on _Create Azure AD application_ and search for your Azure created app in the search pane on the right, check the box for your app and click _Next_.
- On the top tab click on _Customize permissions_.
- Under _Product-level permissions_ search for your product and apply the _Read/write_ permission under _Publishing_.
  - **NOTE** We will be changing this with future iterations of the tool to more accurately apply permissions.


- Tony to update - update permissions



## Step 3: Configure your wrapper

- Navigate to the root of your wrapper directory.
- Edit the _UploadPcPackageTest.json_ with your favorite editor and update the following fields:
  - bigId (From Partner Center)
  - branchFriendlyName (name of the branch/sandbox from Partner Center)
    - XBOX RECOMMEND: In order to facilitate a quick and efficient development process, while at the same time ensuring that you are maintaining control over your production releases, we at Xbox highly recommend that you only use this feature on QA and dev branches and import packages to your Main branch in Partner Center, after they’ve been pre-certified.
  - flightName (From Partner Center - if you're doing a flight)
  - packageFilePath (Location of the game package)
  - clientId (From Azure Portal)
  - tenantId (From Azure Portal)
  - Save

## Step 4: Fire Away

- Open _powershell_ via the start menu.
- Navigate to the root of your wrapper directory and run the following command:
  -  .\GameStoreBroker.Application.exe UploadPcPackage -c .\UploadPcPackageTest.json -s *secretkey*

- Extra parameters to pass:
  - -c, --ConfigFile Required. The location of json config file
  - -s, --ClientSecret Required. The client secret of the AAD app.
  - -v, --Verbose (Default: false) Log verbose messages such as http calls.
  - --help Display this help screen.
  - --version Display version information.

## Q & A

Question: When will the tool start working with Flight branches? <br>
Answer: We're working on this now and with the new iteration of the wrapper uploading to a flight should work.

Question: Is this only for PC? <br>
Answer: The naming of the action and config is only temporary - this will get changed later. All packages are supported: UWP, MSIXVC, XVC.

Question: Can I use the API directly? <br>
Answer: You may and documentation will be written however there will be no support. Please reference the document and the wrapper source for call patterns. 

Question: Can I use one App name for multiple products? <br>
Answer: In theory this should work, but currently it is not. We're working with the Partner Center Accounts Team to identify the issue. 

Question: Will this have delta uploads? <br>
Answer: Delta uploads was out of scope for the initial target of the project - however this has been commited for future implementation.

Question: Will I need to do anything different for delta uploads to work? <br>
Answer: You will not - this will all be done at the service layer and client shouldn't have to be touched. 

Question: Will the wrapper support other actions? <br>
Answer: We will indeed! Deleting/removing packages, moving packages between branches and publishing will all be added.

Question: Could I use this wrapper to automate and update other parts of partner center apart from uploads? <br>
Answer: Unfortunately not right now and our only scope was uploads. The API teams are working to further expand this out to other parts of partner center. 

Question: I don't like the secret key in the config - can we change it? <br>
Answer: We will be changing this most likely, potentially inputting at runtime. 

Question: If I want to change how the wrapper works who do I reach out to? <br>
Answer: It's completely open source and you can change the wrapper as you wish! Use, adjust and contribute! 

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
