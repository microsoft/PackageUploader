// Copyright (C) Microsoft. All rights reserved.

using Karambolo.Extensions.Logging.File;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Text;

namespace GameStoreBroker.Application
{
    public class CustomFileLogEntryTextBuilder : FileLogEntryTextBuilder
    {
        public string TimestampFormat { get; set; }

        protected override void AppendTimestamp(StringBuilder sb, DateTimeOffset timestamp)
        {
            sb.Append(timestamp.ToLocalTime().ToString(TimestampFormat ?? "o", CultureInfo.InvariantCulture));
        }

        protected override void AppendLogScopeInfo(StringBuilder sb, IExternalScopeProvider scopeProvider)
        {
            scopeProvider.ForEachScope((scope, builder) =>
            {
                builder.Append(' ');
                AppendLogScope(builder, scope);
            }, sb);
        }

        protected override void AppendMessage(StringBuilder sb, string message)
        {
            sb.Append(' ');
            var length = sb.Length;
            sb.AppendLine(message);
            sb.Replace(Environment.NewLine, " ", length, message.Length);
        }

        public override void BuildEntryText(
            StringBuilder sb,
            string categoryName,
            LogLevel logLevel,
            EventId eventId,
            string message,
            Exception exception,
            IExternalScopeProvider scopeProvider,
            DateTimeOffset timestamp)
        {
            this.AppendTimestamp(sb, timestamp);
            this.AppendLogLevel(sb, logLevel);
            this.AppendCategoryName(sb, categoryName);
            this.AppendEventId(sb, eventId);
            if (scopeProvider != null)
                this.AppendLogScopeInfo(sb, scopeProvider);
            if (!string.IsNullOrEmpty(message))
                this.AppendMessage(sb, message);
            if (exception == null)
                return;
            this.AppendException(sb, exception);
        }
    }
}