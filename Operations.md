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
- **minutesToWaitForProcessing**: optional - if not set, it will be 30 
- **availabilityDate**: optional
   - **isEnabled**: optional 
   - **effectiveDate**: optional - if informed it will set the availability date in this branch/marketGroupId for all the Uwp packages
- **mandatoryDate**: optional
   - **isEnabled**: optional,
   - **effectiveDate**: optional - if informed it will set the mandatory date in this branch/marketGroupId for all the Uwp packages
- **gradualRollout**: optional
   - **isEnabled**: true,
   - **percentage**: 33,
   - **isSeekEnabled**: false

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
