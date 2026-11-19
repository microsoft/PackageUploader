// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging.Abstractions;
using PackageUploader.Application.Models;
using PackageUploader.Application.Operations;
using PackageUploader.ClientApi.Client.Ingestion.Models;

namespace PackageUploader.Application.Test.Operations;

[TestClass]
public class GetPackagesOperationTest
{
    [TestMethod]
    public async Task PackagesToJson_SerializationFailure_ReachesOperationErrorHandler()
    {
        static IEnumerable<Package> Packages()
        {
            yield return new Package(new GamePackage { FileName = "example.msixvc" });
            throw new InvalidOperationException("Synthetic serialization failure.");
        }

        Assert.ThrowsExactly<InvalidOperationException>(() => GetPackagesOperation.PackagesToJson(Packages()));

        var operation = new TestOperation(NullLogger.Instance, _ =>
        {
            GetPackagesOperation.PackagesToJson(Packages());
            return Task.CompletedTask;
        });
        Assert.AreEqual(3, await operation.RunAsync(CancellationToken.None));
    }
}
