## 1.0.0 (11.03.2019):

* Adapted to ForContext() in changes in abstractions module.

## 0.1.1

* Downgrade version of `Microsoft.Extensions.Logging.Abstractions` to 2.0.0

## 0.1.0

* Create `VostokLoggerProvider` - an implementation of `ILoggerProvider`
* Create `loggerFactory.AddVostok(log)` extension method
* Support concurrent `ILogger.BeginScope()`