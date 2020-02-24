## 2.0.4 (24.02.2020)
Added `MinimumLevel` setting.

## 2.0.3 (23.12.2019)
Create `loggerFactory.AddVostok(log)` extension method

## 2.0.2 (19.12.2019):
Added `VostokLoggerProviderSettings` that allows to ignore some scopes.

## 2.0.0 (26.06.2019):

* Breaking change: BeginScope() method no longer adds a `Scope` property to log events. Instead, it utilizes well-known operation context property.

## 1.0.0 (11.03.2019):

* Adapted to ForContext() in changes in abstractions module.

## 0.1.1

* Downgrade version of `Microsoft.Extensions.Logging.Abstractions` to 2.0.0

## 0.1.0

* Create `VostokLoggerProvider` - an implementation of `ILoggerProvider`
* Create `loggerFactory.AddVostok(log)` extension method
* Support concurrent `ILogger.BeginScope()`
