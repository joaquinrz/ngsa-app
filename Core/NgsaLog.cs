﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.CorrelationVector;
using Microsoft.Extensions.Logging;

namespace Ngsa.Middleware
{
    public class NgsaLog
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            IgnoreNullValues = true,
        };

        public string Name { get; set; } = string.Empty;
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
        public string ErrorMessage { get; set; } = string.Empty;
        public string NotFoundError { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Exception Exception { get; set; } = null;
        public EventId EventId { get; set; } = new EventId(-1, string.Empty);
        public HttpContext Context { get; set; } = null;
        public Dictionary<string, object> Data { get; } = new Dictionary<string, object>();

        public NgsaLog GetLogger(string method, HttpContext context = null)
        {
            NgsaLog logger = new NgsaLog
            {
                Name = Name,
                ErrorMessage = ErrorMessage,
                NotFoundError = NotFoundError,
                LogLevel = LogLevel,
                Method = method,
                Context = context,
            };

            return logger;
        }

        public void LogInformation(string message)
        {
            if (LogLevel > LogLevel.Information)
            {
                return;
            }

            UpdateDictionary(message, LogLevel.Information);
            WriteLog();
        }

        public void LogWarning(string message)
        {
            if (LogLevel > LogLevel.Warning)
            {
                return;
            }

            UpdateDictionary(message, LogLevel.Warning);
            WriteLog();
        }

        public void LogError(string message, Exception ex = null)
        {
            if (LogLevel > LogLevel.Error)
            {
                return;
            }

            // set the exception
            if (ex != null)
            {
                Exception = ex;
            }

            UpdateDictionary(message, LogLevel.Error);

            // display the error
            WriteLog();
        }

        private void WriteLog()
        {
            Console.ForegroundColor = LogLevel >= LogLevel.Error ? ConsoleColor.Red :
                LogLevel == LogLevel.Warning ? ConsoleColor.Yellow :
                LogLevel == LogLevel.Information ? ConsoleColor.Green : Console.ForegroundColor;

            if (LogLevel >= LogLevel.Error)
            {
                Console.Error.WriteLine(JsonSerializer.Serialize(Data, Options));
            }
            else
            {
                Console.WriteLine(JsonSerializer.Serialize(Data, Options));
            }

            Console.ResetColor();
        }

        private void SetDataValue(string key, object value)
        {
            if (!Data.TryAdd(key, value))
            {
                Data[key] = value;
            }
        }

        private void UpdateDictionary(string message, LogLevel logLevel)
        {
            SetDataValue("Date", DateTime.UtcNow);
            SetDataValue("LogName", Name);
            SetDataValue("Method", Method);
            SetDataValue("Message", message);
            SetDataValue("LogLevel", logLevel);

            if (EventId.Id > 0)
            {
                SetDataValue("EventId", EventId.Id);
            }

            if (!string.IsNullOrWhiteSpace(EventId.Name))
            {
                SetDataValue("EventName", EventId.Name);
            }

            if (Exception != null)
            {
                SetDataValue("ExceptionType", Exception.GetType().FullName);
                SetDataValue("ExceptionMessage", Exception.Message);
            }

            if (Context != null && Context.Items != null)
            {
                // todo - causing xss error
                // SetDataValue("Path", Context.Request.Path + (string.IsNullOrWhiteSpace(Context.Request.QueryString.Value) ? string.Empty : Context.Request.QueryString.Value));

                CorrelationVector cv = CorrelationVectorExtensions.GetCorrelationVectorFromContext(Context);

                if (cv != null)
                {
                    SetDataValue("CVector", cv.Value);
                }
            }
        }
    }
}
