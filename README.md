# StructuredLoggerMessage

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Continuous Deployment](https://github.com/JakenVeina/SeqLoggerProvider/workflows/Continuous%20Deployment/badge.svg)](https://github.com/JakenVeina/SeqLoggerProvider/actions?query=workflow%3A%22Continuous+Deployment%22)
[![NuGet](https://img.shields.io/nuget/v/SeqLoggerProvider.svg)](https://www.nuget.org/packages/SeqLoggerProvider/)

An implementation of `ILoggerProvider`, from the [.NET Extensions Logging Framework](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line) framework, for writing log entries to a [Seq](https://datalust.co/seq) server.

This library provides an alternative to the [official Seq logger provider](https://datalust.co/seq), with a goal of providing a slimmer and more extensible implementation. This library includes the following key features that separate it from the official one:

 - Includes only first-party and .NET dependencies, including...
 -- [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/api/system.text.json?view=net-5.0),
 -- [System.Threading.Channels](https://docs.microsoft.com/en-us/dotnet/api/system.threading.channels?view=net-5.0),
 -- [Microsoft.Extensions.Http](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-5.0),
 -- [Microsoft.Extensions.Configuration](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration?view=dotnet-plat-ext-5.0)
 -- [Microsoft.Extensions.DependencyInjection](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
 - Allows for global-scope log data, I.E. data fields that are included upon every log event sent to the server.
 - Allows for more granular control of event batching, including both size-based and time-based thresholds, and flood control
 - Provides extension points for JSON serialization, based on [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/api/system.text.json?view=net-5.0), allowing for consumers to write custom serializers for log entries that can optimize for performance or data size.
 - Provides extension points for HTTP transmission, allowing for consumers to apply any custom configurations to the `HttpClient` instances that are used to deliver log entries to the Seq server.

## Usage

### Basic Setup

To setup the logger provider within a .NET application, simply call the setup method, while setting up your logging system. Either upon your `IHostBuilder`...

```cs
.ConfigureLogging(builder => builder
    .AddSeq())
```

...or upon your `IServiceCollection`...

```cs
.AddLogging(builder => builder
    .AddSeq());
```

### Configuration

Configuration is automatically extracted from the ambient `IConfiguration` system, if present, in the same fashion as all first-party logger providers. For example, if you're using an `appsettings.json` file...

```json
{
  "Logging": {
    "Seq": {
      "ServerUrl": "http://localhost:5341/"
      "ApiKey": "...",
      "GlobalScopeState": {
        "Application": "SeqLogger.Test"
      },
      "LogLevel": {
        "Default": "Debug",
        "SeqLoggerProvider": "None"
      }
    }
  }
}
```

If you would like to customize the configuration manually, the `.AddSeq()` method supports a standard configuration delegate being passed in...

```cs
builder.AddSeq(options =>
{
	var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
	if (assemblyVersion is not null)
        options.GlobalScopeState.Add("Version", assemblyVersion.ToString();
});
```

All configuration properties are optional, except for `ServerUrl`, for obvious reasons.

### JSON Customization

In order to customize JSON serialization behavior, simply supply an options configuration delegate, when adding the provider.

```cs
builder.AddSeq(configureJsonSerializer: options =>
{
    options.Converters.Add(new MyJsonConverter());
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
```

### HTTP Customization

In order to customize HTTP transmission behavior, simply use the `configureHttpClient` parameter supported by the `.AddSeq()` method:

```cs
builder.AddSeq(configureHttpClient: builder => builder
    .RedactLoggedHeaders(new[] { SeqLoggerConstants.ApiKeyHeaderName }));
```
