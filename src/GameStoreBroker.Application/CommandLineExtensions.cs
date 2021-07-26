// Copyright (C) Microsoft. All rights reserved.

using System.CommandLine;
using System.CommandLine.Invocation;

namespace GameStoreBroker.Application
{
    internal static class CommandLineExtensions
    {
        public static Command AddHandler(this Command command, ICommandHandler handler)
        {
            command.Handler = handler;
            return command;
        }
    }
}