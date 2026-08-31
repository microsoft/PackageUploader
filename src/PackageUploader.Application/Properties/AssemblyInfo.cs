// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("PackageUploader.Application.Test")]

// Allows Moq to create proxies for internal interfaces (IMsixvc2UploadToolProvider, IMsixvc2ProcessRunner) in tests.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]