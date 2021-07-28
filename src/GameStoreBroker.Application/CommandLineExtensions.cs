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

        public static T GetOptionValue<T>(this InvocationContext invocationContext, Option<T> option)
        {
            return invocationContext.ParseResult.ValueForOption(option);
        }
    }
}