# GetProduct
###### Returns metadata of the product
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/GetProduct.json))
##### Definition:
- **operationName**: "GetProduct",
- **aadAuthInfo**: required
  - **tenantId**: required
  - **clientId**: required
- **productId**: *productId* or *bigId* required
- **bigId**: *productId* or *bigId* required

# UploadUwpPackage
###### Uploads Uwp game package
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/UploadUwpPackage.json))
##### Definition:
- **operationName**: "GetProduct",
- **aadAuthInfo**: required
  - **tenantId**: required
  - **clientId**: required
- **productId**: *productId* or *bigId* required
- **bigId**: *productId* or *bigId* required
- **branchFriendlyName**: *flightName* or *branchFriendlyName* required
- **flightName**: *flightName* or *branchFriendlyName* required
- **marketGroupId**: optional - if not set, it will use *default* as the market group
- **packageFilePath**: required - path to the package file
- **minutesToWaitForProcessing**: optional (default 30) - it will check the package processing status every minute for this long, until it succeeds or fails
- **availabilityDate**: optional - if informed it will configure custom availability date for your UWP market groups [Learn more](http://go.microsoft.com/fwlink/?LinkId=825239)
   - **isEnabled**: optional (default false) - it will enable/disable custom availability date
   - **effectiveDate**: optional - if informed it will set the package availability date
- **mandatoryDate**: optional - if informed it will configure custom mandatory date for your UWP market groups [Learn more](https://go.microsoft.com/fwlink/?linkid=2008878)
   - **isEnabled**: optional (default false) - it will enable/disable custom mandatory date
   - **effectiveDate**: optional - if informed it will set the mandatory date
- **gradualRollout**: optional - if informed it will configure gradual rollout for your UWP packages [Learn more](http://go.microsoft.com/fwlink/?LinkId=733680)
   - **isEnabled**:  optional (default false) - it will enable/disable gradual rollout
   - **percentage**: optional - rollout to start with
   - **isSeekEnabled**: optional - enable/disable always provide the newest packages when customers manually check for updates

# UploadXvcPackage
###### Uploads Xvc game package and assets
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/UploadXvcPackage.json))

# RemovePackages
###### Removes all game packages and assets from a branch
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/RemovePackages.json))

# ImportPackages
###### Imports all game packages from a branch to a destination branch
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/ImportPackages.json))

# PublishPackages
###### Publishes all game packages from a branch or flight to a destination sandbox or flight
#### Config file ([template](https://github.com/microsoft/GameStoreBroker/blob/main/templates/PublishPackages.json))
