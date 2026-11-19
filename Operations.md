# GetProduct
###### Returns metadata of the product
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/GetProduct.json))
##### Definition:
- **operationName**: "GetProduct",
- **aadAuthInfo**: required when using authentication method *AppCert*, *AppSecret* or *ManagedIdentity*
  - **tenantId**: required when using authentication method *AppCert* or *AppSecret*
  - **clientId**: required
  - **certificateThumbprint**: required when using authentication method *AppCert*
  - **certificateStore**: optional when using authentication method *AppCert* (default *My*)
  - **certificateLocation**: optional when using authentication method *AppCert* (default *CurrentUser*)
- **productId**: *productId* or *bigId* required
- **bigId**: *productId* or *bigId* required

# GetPackages
###### Returns list of game packages from a branch or flight
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/GetPackages.json))
##### Definition:
- **operationName**: "GetPackages",
- **aadAuthInfo**: required when using authentication method *AppCert*, *AppSecret* or *ManagedIdentity*
  - **tenantId**: required when using authentication method *AppCert* or *AppSecret*
  - **clientId**: required
  - **certificateThumbprint**: required when using authentication method *AppCert*
  - **certificateStore**: optional when using authentication method *AppCert* (default *My*)
  - **certificateLocation**: optional when using authentication method *AppCert* (default *CurrentUser*)
- **productId**: *productId* or *bigId* required
- **bigId**: *productId* or *bigId* required
- **branchFriendlyName**: *flightName* or *branchFriendlyName* required
- **flightName**: *flightName* or *branchFriendlyName* required
- **marketGroupName**: optional - if not set, it will use *default* as the market group (case sensitive)

#### Machine-readable output

Use `GetPackages --ConfigFile GetPackages.json --Data` (or `-d`) to write a bare JSON array to standard output. Successful execution returns exit code 0; diagnostics go to standard error in data mode, even with `--Verbose`. Package lookup or serialization failures return a nonzero exit code, not a partial array or an empty-array fallback.

The command reads the selected branch or flight's **current draft package configuration**, then fetches the packages in the selected, case-sensitive market group. It is not a product-wide or historical package search. An existing market group with no packages returns `[]`; a missing configuration or market group is an error.

Each array item uses this explicit metadata allowlist. Null values are omitted, so consumers must tolerate missing fields and older server responses.

| Field | JSON type | Meaning |
| --- | --- | --- |
| `fileName` | string | Package file name. |
| `fileSize` | number | Package size in bytes, preserved as a 64-bit integer; may exceed 32-bit range. |
| `id` | string | Package resource ID returned by the service, including the canonical ID after a redirect. This is not the build ID. |
| `state` | string | Processing state: `Unknown`, `PendingUpload`, `Uploaded`, `InProcessing`, `Processed`, or `ProcessFailed`. Unrecognized or missing server states map to `Unknown`. |
| `packageType` | string | Package type reported by the service, when available. |
| `xvcTargetPlatform` | string | XVC target platform reported by the service, when available. |
| `buildId` | string | Authoritative extracted package PDUID, when provided by the service. |
| `contentId` | string | Extracted package content identifier, when provided by the service. |
| `version` | string | Extracted package-version text, for example `1.0.0.0`; not normalized to SemVer or prefixed with `v`. |

For example, a response with available identity metadata produces:

```json
[
  {
    "fileName": "example.msixvc",
    "fileSize": 8589934592,
    "id": "package-resource-1",
    "state": "Processed",
    "packageType": "Msixvc",
    "xvcTargetPlatform": "PC",
    "buildId": "11111111-2222-4333-8444-555555555555",
    "contentId": "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
    "version": "1.0.0.0"
  }
]
```

PackageUploader requests identity using `GET /products/{productId}/packages/{packageId}?$expand=PackageIdentity` on the existing API. It preserves this opt-in when rereading a package redirect and fetching its canonical resource, and on package-processing polls. The expansion is available to authorized callers; it is not restricted by caller identity or a tool-identification header and does not introduce a new API version. Unexpanded package details and package list/create/update responses retain their legacy shape.

Identity fields may be absent on older servers, older packages, or packages whose metadata is not yet available. Older servers can ignore `$expand=PackageIdentity` and return a successful legacy response; this is treated as unavailable identity, not a package mismatch. The client does not retry without the expansion to hide lookup, authorization, timeout, or service failures; existing authentication refresh and transient retries are unchanged. Missing identity is never derived from resource IDs, filenames, or placeholder GUIDs. Upload information, tokens, SAS URLs, and ETags are not part of this output.

Pipelines can use available identity as an advisory pre-upload check within the selected draft and market group. This is not transactional idempotency: another upload can race with the check, and missing metadata does not prove a package was never uploaded. The command does not automatically skip or deduplicate uploads.

# UploadUwpPackage
###### Uploads Uwp game package
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/UploadUwpPackage.json))
##### Definition:
- **operationName**: "UploadUwpPackage",
- **aadAuthInfo**: required when using authentication method *AppCert*, *AppSecret* or *ManagedIdentity*
  - **tenantId**: required when using authentication method *AppCert* or *AppSecret*
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
   - **httpTimeoutMs**: (default and recommended: 300000)
   - **httpUploadTimeoutMs**: (default and recommended: 300000)
   - **maxParallelism**: (default and recommended: 24)
   - **defaultConnectionLimit**: (default and recommended: -1)
   - **expect100Continue**: (default and recommended: false)
   - **useNagleAlgorithm**: (default and recommended: false)
   - **retryCount**: (default and  recommended: 3)

# UploadXvcPackage
###### Uploads Xvc game package and assets
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/UploadXvcPackage.json))
##### Definition:
- **operationName**: "UploadXvcPackage",
- **aadAuthInfo**: required when using authentication method *AppCert*, *AppSecret* or *ManagedIdentity*
  - **tenantId**: required when using authentication method *AppCert* or *AppSecret*
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
  - **sodbFilePath**: optional - path to the SODB (Shader Object Database) file
- **minutesToWaitForProcessing**: optional (default 30) - it will check the package processing status every minute for this long, until it succeeds or fails
- **availabilityDate**: optional - if informed it will configure custom availability date for your XVC/MSIXVC packages [Learn more](http://go.microsoft.com/fwlink/?LinkId=825239)
   - **isEnabled**: optional (default false) - it will enable/disable custom availability date
   - **effectiveDate**: optional - if informed it will set the package availability date (date format example: "2021-10-24T21:00:00.000Z")
- **preDownloadDate**: optional (default is equivalent to availabilityDate) - it will configure when package will be available for download for your XVC/MSIXVC packages
   - **isEnabled**: optional (default false) - it will enable/disable custom pre-download date
   - **effectiveDate**: optional - if informed it will set the package pre-download date (date format example: "2021-10-24T21:00:00.000Z")
- **uploadConfig**: optional - httpClient configuration to be used to upload the files
   - **httpTimeoutMs**: (default and recommended: 300000)
   - **httpUploadTimeoutMs**: (default and recommended: 300000)
   - **maxParallelism**: (default and recommended: 24)
   - **defaultConnectionLimit**: (default and recommended: -1)
   - **expect100Continue**: (default and recommended: false)
   - **useNagleAlgorithm**: (default and recommended: false)
   - **retryCount**: (default and  recommended: 3)
  
# RemovePackages
###### Removes game packages and assets from a branch
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/RemovePackages.json))
##### Definition:
- **operationName**: "RemovePackages",
- **aadAuthInfo**: required when using authentication method *AppCert*, *AppSecret* or *ManagedIdentity*
  - **tenantId**: required when using authentication method *AppCert* or *AppSecret*
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
- **aadAuthInfo**: required when using authentication method *AppCert*, *AppSecret* or *ManagedIdentity*
  - **tenantId**: required when using authentication method *AppCert* or *AppSecret*
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
- **preDownloadDate**: optional (default is equivalent to availabilityDate) - it will configure when package will be available for download for your XVC/MSIXVC packages
   - **isEnabled**: optional (default false) - it will enable/disable custom pre-download date
   - **effectiveDate**: optional - if informed it will set the package pre-download date (date format example: "2021-10-24T21:00:00.000Z")
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
- **aadAuthInfo**: required when using authentication method *AppCert*, *AppSecret* or *ManagedIdentity*
  - **tenantId**: required when using authentication method *AppCert* or *AppSecret*
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
