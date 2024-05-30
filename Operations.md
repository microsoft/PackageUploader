# GetProduct
###### Returns metadata of the product
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/GetProduct.json))
##### Definition:
- **operationName**: "GetProduct",
- **aadAuthInfo**: required when using authentication method *AppCert* or *AppSecret*
  - **tenantId**: required
  - **clientId**: required
  - **certificateThumbprint**: required when using authentication method *AppCert*
  - **certificateStore**: optional when using authentication method *AppCert* (default *My*)
  - **certificateLocation**: optional when using authentication method *AppCert* (default *CurrentUser*)
- **productId**: *productId* or *bigId* required
- **bigId**: *productId* or *bigId* required

# GetPackages
###### Returns list of game packages from a branch
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/GetPackages.json))
##### Definition:
- **operationName**: "GetPackages",
- **aadAuthInfo**: required when using authentication method *AppCert* or *AppSecret*
  - **tenantId**: required
  - **clientId**: required
  - **certificateThumbprint**: required when using authentication method *AppCert*
  - **certificateStore**: optional when using authentication method *AppCert* (default *My*)
  - **certificateLocation**: optional when using authentication method *AppCert* (default *CurrentUser*)
- **productId**: *productId* or *bigId* required
- **bigId**: *productId* or *bigId* required
- **branchFriendlyName**: *flightName* or *branchFriendlyName* required
- **flightName**: *flightName* or *branchFriendlyName* required
- **marketGroupName**: optional - if not set, it will use *default* as the market group (case sensitive)

# UploadUwpPackage
###### Uploads Uwp game package
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/UploadUwpPackage.json))
##### Definition:
- **operationName**: "UploadUwpPackage",
- **aadAuthInfo**: required when using authentication method *AppCert* or *AppSecret*
  - **tenantId**: required
  - **clientId**: required
  - **certificateThumbprint**: required when using authentication method *AppCert*
  - **certificateStore**: optional when using authentication method *AppCert* (default *My*)
  - **certificateLocation**: optional when using authentication method *AppCert* (default *CurrentUser*)
- **productId**: *productId* or *bigId* required
- **bigId**: *productId* or *bigId* required
- **branchFriendlyName**: *flightName* or *branchFriendlyName* required
- **flightName**: *flightName* or *branchFriendlyName* required
- **marketGroupName**: optional - if not set, it will use *default* as the market group (case sensitive)
- **packageFilePath**: required - path to the package file
- **minutesToWaitForProcessing**: optional (default 30) - it will check the package processing status every minute for this long, until it succeeds or fails
- **availabilityDate**: optional - if informed it will configure custom availability date for your UWP market groups [Learn more](http://go.microsoft.com/fwlink/?LinkId=825239)
   - **isEnabled**: optional (default false) - it will enable/disable custom availability date
   - **effectiveDate**: optional - if informed it will set the package availability date (date format example: "2021-10-24T21:00:00.000Z")
- **mandatoryDate**: optional - if informed it will configure custom mandatory date for your UWP market groups [Learn more](https://docs.microsoft.com/en-gb/windows/uwp/publish/upload-app-packages#mandatory-update)
   - **isEnabled**: optional (default false) - it will enable/disable custom mandatory date
   - **effectiveDate**: optional - if informed it will set the mandatory date (date format example: "2021-10-24T21:00:00.000Z")
- **gradualRollout**: optional - if informed it will configure gradual rollout for your UWP packages [Learn more](https://docs.microsoft.com/en-gb/windows/uwp/publish/upload-app-packages#gradual-package-rollout)
   - **isEnabled**:  optional (default false) - it will enable/disable gradual rollout
   - **percentage**: optional - rollout to start with
   - **isSeekEnabled**: optional - enable/disable always provide the newest packages when customers manually check for updates
- **uploadConfig**: optional - httpClient configuration to be used to upload the files
   - **httpTimeoutMs**: (default and recommended: 5000)
   - **httpUploadTimeoutMs**: (default and recommended: 300000)
   - **maxParallelism**: (default and recommended: 24)
   - **defaultConnectionLimit**: (default and recommended: -1)
   - **expect100Continue**: (default and recommended: false)
   - **useNagleAlgorithm**: (default and recommended: false)

# UploadXvcPackage
###### Uploads Xvc game package and assets
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/UploadXvcPackage.json))
##### Definition:
- **operationName**: "UploadXvcPackage",
- **aadAuthInfo**: required when using authentication method *AppCert* or *AppSecret*
  - **tenantId**: required
  - **clientId**: required
  - **certificateThumbprint**: required when using authentication method *AppCert*
  - **certificateStore**: optional when using authentication method *AppCert* (default *My*)
  - **certificateLocation**: optional when using authentication method *AppCert* (default *CurrentUser*)
- **productId**: *productId* or *bigId* required
- **bigId**: *productId* or *bigId* required
- **branchFriendlyName**: *flightName* or *branchFriendlyName* required
- **flightName**: *flightName* or *branchFriendlyName* required
- **marketGroupName**: optional - if not set, it will use *default* as the market group (case sensitive)
- **packageFilePath**: required - path to the package file
- **deltaUpload**: optional - determine if delta upload should be used
- **gameAssets**: required - paths to the game assets
  - **ekbFilePath**: required - path to the EKB file
  - **subValFilePath**: required - path to the SubVal File
  - **symbolsFilePath**: optional - path to the Symbols File
  - **discLayoutFilePath**: optional - path to the Disc Layout File
- **minutesToWaitForProcessing**: optional (default 30) - it will check the package processing status every minute for this long, until it succeeds or fails
- **availabilityDate**: optional - if informed it will configure custom availability date for your XVC/MSIXVC packages [Learn more](http://go.microsoft.com/fwlink/?LinkId=825239)
   - **isEnabled**: optional (default false) - it will enable/disable custom availability date
   - **effectiveDate**: optional - if informed it will set the package availability date (date format example: "2021-10-24T21:00:00.000Z")
- **preDownloadDate**: optional (default is equivalent to availabilityDate) - it will configure when package will be available for download (date format example: "2021-10-24T21:00:00.000Z")
   - **isEnabled**: optional (default false) - it will enable/disable custom availability date
   - **effectiveDate**: optional - if informed it will set the package availability date (date format example: "2021-10-24T21:00:00.000Z")
- **uploadConfig**: optional - httpClient configuration to be used to upload the files
   - **httpTimeoutMs**: (default and recommended: 5000)
   - **httpUploadTimeoutMs**: (default and recommended: 300000)
   - **maxParallelism**: (default and recommended: 24)
   - **defaultConnectionLimit**: (default and recommended: -1)
   - **expect100Continue**: (default and recommended: false)
   - **useNagleAlgorithm**: (default and recommended: false)
  
# RemovePackages
###### Removes game packages and assets from a branch
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/RemovePackages.json))
##### Definition:
- **operationName**: "RemovePackages",
- **aadAuthInfo**: required when using authentication method *AppCert* or *AppSecret*
  - **tenantId**: required
  - **clientId**: required
  - **certificateThumbprint**: required when using authentication method *AppCert*
  - **certificateStore**: optional when using authentication method *AppCert* (default *My*)
  - **certificateLocation**: optional when using authentication method *AppCert* (default *CurrentUser*)
- **productId**: *productId* or *bigId* required
- **bigId**: *productId* or *bigId* required
- **branchFriendlyName**: *flightName* or *branchFriendlyName* required
- **flightName**: *flightName* or *branchFriendlyName* required
- **marketGroupName**: optional - if not set, it will remove packages from all market groups (case sensitive)
- **packageFileName**: required - it will delete packages with this file name (wildcards * and ? supported)

# ImportPackages
###### Imports all game packages from a branch to a destination branch
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/ImportPackages.json))
##### Definition:
- **operationName**: "ImportPackages",
- **aadAuthInfo**: required when using authentication method *AppCert* or *AppSecret*
  - **tenantId**: required
  - **clientId**: required
  - **certificateThumbprint**: required when using authentication method *AppCert*
  - **certificateStore**: optional when using authentication method *AppCert* (default *My*)
  - **certificateLocation**: optional when using authentication method *AppCert* (default *CurrentUser*)
- **productId**: *productId* or *bigId* required
- **bigId**: *productId* or *bigId* required
- **branchFriendlyName**: *flightName* or *branchFriendlyName* required
- **flightName**: *flightName* or *branchFriendlyName* required
- **destinationBranchFriendlyName**: *destinationFlightName* or *destinationBranchFriendlyName* required
- **destinationFlightName**: *destinationFlightName* or *destinationBranchFriendlyName* required
- **overwrite**: optional - it will replace the packages in the destination branch/flight
- **marketGroupName**: optional - if not set, it will import all market groups packages (case sensitive)
- **availabilityDate**: optional - if informed it will configure custom availability date for your UWP market groups and your XVC/MSIXVC packages in the destination branch/flight  [Learn more](http://go.microsoft.com/fwlink/?LinkId=825239)
   - **isEnabled**: optional (default false) - it will enable/disable custom availability date
   - **effectiveDate**: optional - if informed it will set the package availability date (date format example: "2021-10-24T21:00:00.000Z")
- **mandatoryDate**: optional - if informed it will configure custom mandatory date for your UWP market groups in the destination branch/flight [Learn more](https://docs.microsoft.com/en-gb/windows/uwp/publish/upload-app-packages#mandatory-update)
   - **isEnabled**: optional (default false) - it will enable/disable custom mandatory date
   - **effectiveDate**: optional - if informed it will set the mandatory date (date format example: "2021-10-24T21:00:00.000Z")
- **preDownloadDate**: optional (default is equivalent to availabilityDate) - it will configure when package will be available for download (date format example: "2021-10-24T21:00:00.000Z")
   - **isEnabled**: optional (default false) - it will enable/disable custom availability date
   - **effectiveDate**: optional - if informed it will set the package availability date (date format example: "2021-10-24T21:00:00.000Z")
- **gradualRollout**: optional - if informed it will configure gradual rollout for your UWP packages in the destination branch/flight [Learn more](https://docs.microsoft.com/en-gb/windows/uwp/publish/upload-app-packages#gradual-package-rollout)
   - **isEnabled**:  optional (default false) - it will enable/disable gradual rollout
   - **percentage**: optional - rollout to start with
   - **isSeekEnabled**: optional - enable/disable always provide the newest packages when customers manually check for updates

# PublishPackages
###### Publishes all game packages from a branch or flight to a destination sandbox or flight
If you want to deploy to RETAIL, you need to use the parameter --Retail in the command line
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/PublishPackages.json))
##### Definition:
- **operationName**: "PublishPackages",
- **aadAuthInfo**: required when using authentication method *AppCert* or *AppSecret*
  - **tenantId**: required
  - **clientId**: required
  - **certificateThumbprint**: required when using authentication method *AppCert*
  - **certificateStore**: optional when using authentication method *AppCert* (default *My*)
  - **certificateLocation**: optional when using authentication method *AppCert* (default *CurrentUser*)
- **productId**: *productId* or *bigId* required
- **bigId**: *productId* or *bigId* required
- **flightName**: *flightName* or (*branchFriendlyName* and *destinationSandboxName*) required
- **branchFriendlyName**: *flightName* or (*branchFriendlyName* and *destinationSandboxName*) required
- **destinationSandboxName**: *flightName* or (*branchFriendlyName* and *destinationSandboxName*) required
- **minutesToWaitForPublishing**: optional (default 0 is fire and forget) - it will check the package processing status every minute for this long, until it succeeds or fails
- **publishConfiguration**: optional - configuration of the publish submission [Learn more](https://docs.microsoft.com/en-gb/windows/uwp/publish/manage-submission-options)
  - **releaseTime**: optional - publish release time, it will publish as soon as it passes certification if it is not set (date format example: "2021-10-24T21:00:00.000Z")
  - **IsManualPublish**: optional - will enable/disable manual publish 
  - **CertificationNotes**: optional - Certification notes
