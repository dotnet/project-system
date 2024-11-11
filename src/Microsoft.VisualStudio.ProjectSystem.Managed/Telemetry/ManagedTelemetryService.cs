// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Security.Cryptography;
using System.Text;

namespace Microsoft.VisualStudio.Telemetry;

[Export(typeof(ITelemetryService))]
internal class ManagedTelemetryService : ITelemetryService
{
#if DEBUG
    private const string EventNamePrefix = "vs/projectsystem/managed/";
    private const string PropertyNamePrefix = "vs.projectsystem.managed.";
#endif

    public void PostEvent(string eventName)
    {
        Requires.NotNullOrEmpty(eventName);

        PostTelemetryEvent(new TelemetryEvent(eventName));
    }

    public void PostProperty(string eventName, string propertyName, object? propertyValue)
    {
        Requires.NotNullOrEmpty(eventName);
        Requires.NotNullOrEmpty(propertyName);

        TelemetryEvent telemetryEvent = new(eventName);
        telemetryEvent.Properties.Add(propertyName, propertyValue);

        PostTelemetryEvent(telemetryEvent);
    }

    public void PostProperties(string eventName, IEnumerable<(string propertyName, object? propertyValue)> properties)
    {
        Requires.NotNullOrEmpty(eventName);
        Requires.NotNullOrEmpty(properties);

        TelemetryEvent telemetryEvent = new(eventName);
        AddPropertiesToEvent(properties, telemetryEvent);

        PostTelemetryEvent(telemetryEvent);
    }

    private static void AddPropertiesToEvent(IEnumerable<(string propertyName, object? propertyValue)> properties, TelemetryEvent telemetryEvent)
    {
        foreach ((string propertyName, object? propertyValue) in properties)
        {
            if (propertyValue is ComplexPropertyValue complexProperty)
            {
                telemetryEvent.Properties.Add(propertyName, new TelemetryComplexProperty(complexProperty.Data));
            }
            else
            {
                telemetryEvent.Properties.Add(propertyName, propertyValue);
            }
        }
    }

    private void PostTelemetryEvent(TelemetryEvent telemetryEvent)
    {
#if DEBUG
        Assumes.True(telemetryEvent.Name.StartsWith(EventNamePrefix, StringComparisons.TelemetryEventNames));

        foreach ((string propertyName, _) in telemetryEvent.Properties)
        {
            Assumes.True(propertyName.StartsWith(PropertyNamePrefix, StringComparisons.TelemetryEventNames));
        }
#endif

        PostEventToSession(telemetryEvent);
    }

    protected virtual void PostEventToSession(TelemetryEvent telemetryEvent)
    {
        TelemetryService.DefaultSession.PostEvent(telemetryEvent);
    }

    public ITelemetryOperation BeginOperation(string eventName)
    {
        Requires.NotNullOrEmpty(eventName);

#if DEBUG
        Assumes.True(eventName.StartsWith(EventNamePrefix, StringComparisons.TelemetryEventNames));
#endif
        return new TelemetryOperation(TelemetryService.DefaultSession.StartOperation(eventName));
    }

    /// <summary>Internal, for testing purposes only.</summary>
    internal bool IsUserMicrosoftInternal { get; set; } = TelemetryService.DefaultSession.IsUserMicrosoftInternal;

    public string HashValue(string value)
    {
        // Don't hash PII for internal users since we don't need to.
        if (IsUserMicrosoftInternal)
        {
            return value;
        }

        byte[] inputBytes = Encoding.UTF8.GetBytes(value);

#if NET8_0_OR_GREATER
        byte[] hash = SHA256.HashData(inputBytes);
#else
        using var cryptoServiceProvider = SHA256.Create();
        byte[] hash = cryptoServiceProvider.ComputeHash(inputBytes);
#endif

        return ByteArrayToHexString(hash, byteCount: 8);

        static string ByteArrayToHexString(byte[] bytes, int byteCount)
        {
            char[] hexChars = new char[byteCount * 2];
            const string hexDigits = "0123456789abcdef";

            for (int i = 0; i < byteCount; i++)
            {
                int b = bytes[i];
                hexChars[i * 2] = hexDigits[b >> 4];
                hexChars[i * 2 + 1] = hexDigits[b & 0x0F];
            }

            return new string(hexChars);
        }
    }

    private sealed class TelemetryOperation(TelemetryScope<OperationEvent> scope) : ITelemetryOperation
    {
        public void Dispose()
        {
#if DEBUG
            Assumes.True(scope.IsEnd, $"Failed to call '{nameof(ITelemetryOperation.End)}' on {nameof(ITelemetryOperation)} instance.");
#endif
            if (!scope.IsEnd)
            {
                scope.End(TelemetryResult.None);
            }
        }

        public void End(TelemetryResult result)
        {
            scope.End(result);
        }

        public void SetProperties(IEnumerable<(string propertyName, object? propertyValue)> properties)
        {
            Requires.NotNullOrEmpty(properties);
            
#if DEBUG
            foreach ((string propertyName, _) in properties)
            {
                Assumes.True(propertyName.StartsWith(PropertyNamePrefix, StringComparisons.TelemetryEventNames));
            }
#endif

            AddPropertiesToEvent(properties, scope.EndEvent);
        }
    }
}

