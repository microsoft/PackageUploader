// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Moq;

namespace PackageUploader.Application.Test.Tools;

internal static class LoggerMockExtensions
{
    public static void VerifyLogErrorContains<T>(this Mock<ILogger<T>> loggerMock, string expectedSubstring) =>
        VerifyLogContains(loggerMock, LogLevel.Error, expectedSubstring);

    public static void VerifyLogWarningContains(this Mock<ILogger> loggerMock, string expectedSubstring) =>
        VerifyLogContains(loggerMock, LogLevel.Warning, expectedSubstring);

    public static void VerifyLogWarningContains<T>(this Mock<ILogger<T>> loggerMock, string expectedSubstring) =>
        VerifyLogContains(loggerMock, LogLevel.Warning, expectedSubstring);

    /// <summary>
    /// Asserts that no log entry at any level contains the given text. Used to prove credentials never reach
    /// the logger, so it deliberately checks every level rather than the one the caller happens to expect.
    /// </summary>
    public static void VerifyNeverLogged<T>(this Mock<ILogger<T>> loggerMock, string forbiddenSubstring) =>
        loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(forbiddenSubstring)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);

    private static void VerifyLogContains<TLogger>(Mock<TLogger> loggerMock, LogLevel level, string expectedSubstring)
        where TLogger : class, ILogger =>
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(expectedSubstring)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
}
