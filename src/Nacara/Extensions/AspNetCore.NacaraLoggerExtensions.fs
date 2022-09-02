namespace AspNetCore

open System
open Microsoft.Extensions.Logging
open System.Collections.Generic
open System.Collections.Concurrent
open System.Runtime.Versioning
open Microsoft.Extensions.Options
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection.Extensions
open Microsoft.Extensions.Logging.Configuration
open Nacara

module NacaraLoggerExtensions =

    type NacaraLoggerConfiguration() =

        let _logLevels = new Dictionary<LogLevel, string -> unit>()

        do
            _logLevels.Add(LogLevel.Trace, Log.debug)
            _logLevels.Add(LogLevel.Debug, Log.debug)
            _logLevels.Add(LogLevel.Information, Log.info)
            _logLevels.Add(LogLevel.Warning, Log.warn)
            _logLevels.Add(LogLevel.Error, Log.error)
            _logLevels.Add(LogLevel.Critical, Log.error)
            _logLevels.Add(LogLevel.None, ignore)

        member _.EventId: int = 0

        member _.LogLevels: Dictionary<LogLevel, string -> unit> = _logLevels

    type NacaraLogger
        (
            name: string,
            getCurrentConfig: Func<unit, NacaraLoggerConfiguration>
        ) =

        member _.IsEnabled(logLevel: LogLevel) =
            getCurrentConfig.Invoke().LogLevels.ContainsKey(logLevel)

        interface ILogger with

            member _.BeginScope<'TState>(state: 'TState) = null

            member this.IsEnabled(logLevel: LogLevel) = this.IsEnabled logLevel

            member this.Log<'TState>
                (
                    logLevel: LogLevel,
                    eventId: EventId,
                    state: 'TState,
                    exn: Exception,
                    formatter: Func<'TState, Exception, string>
                ) =
                if this.IsEnabled(logLevel) then
                    let config = getCurrentConfig.Invoke()

                    if (config.EventId = 0 || config.EventId = eventId.Id) then
                        let logFunc =
                            config.LogLevels[logLevel]

                        logFunc $"{formatter.Invoke(state, exn)}"

                else
                    ()

    [<UnsupportedOSPlatform("browser")>]
    [<ProviderAlias("NacaraLogger")>]
    type NacaraLoggerProvider
        (
            config: IOptionsMonitor<NacaraLoggerConfiguration>
        ) =

        let mutable _currentConfig = config.CurrentValue

        let _onChangeToken =
            config.OnChange(fun updatedConfig -> _currentConfig <- updatedConfig
            )

        let _loggers =
            new ConcurrentDictionary<string, NacaraLogger>(
                StringComparer.OrdinalIgnoreCase
            )

        member private _.GetCurrentConfig() = _currentConfig

        interface ILoggerProvider with

            member this.CreateLogger(categoryName: string) =
                _loggers.GetOrAdd(
                    categoryName,
                    fun name ->
                        new NacaraLogger(
                            name,
                            Func<unit, NacaraLoggerConfiguration>
                                this.GetCurrentConfig
                        )
                )

            member _.Dispose() =
                _loggers.Clear()
                _onChangeToken.Dispose()

    type ILoggingBuilder with

        member this.AddNacaraLogger() =
            this.AddConfiguration()

            this.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILoggerProvider, NacaraLoggerProvider>
                    ()
            )


            LoggerProviderOptions.RegisterProviderOptions<NacaraLoggerConfiguration, NacaraLoggerProvider>(
                this.Services
            )

            this

        member this.AddNacaraLogger
            (configure: Action<NacaraLoggerConfiguration>)
            =
            this.AddNacaraLogger() |> ignore
            this.Services.Configure(configure) |> ignore

            this
