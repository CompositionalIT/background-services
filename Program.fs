open Giraffe
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Saturn
open System
open System.Threading
open System.Threading.Tasks

type ApplicationBuilder with
    /// Custom keyword to more easily add a background worker to ASP .NET
    [<CustomOperation "background_service">]
    member _.BackgroundService(state: ApplicationState, serviceBuilder) =
        { state with
            ServicesConfig =
                (fun svcCollection -> svcCollection.AddHostedService serviceBuilder)
                :: state.ServicesConfig }

    member this.BackgroundService(state: ApplicationState, backgroundSvc) =
        let worker serviceProvider =
            { new BackgroundService() with
                member _.ExecuteAsync cancellationToken =
                    backgroundSvc serviceProvider cancellationToken }

        this.BackgroundService(state, worker)

/// A full background service using a dedicated type.
type FullBackgroundService(logger:ILogger<unit>) =
    inherit BackgroundService()

    /// Called when the background service needs to run.
    override _.ExecuteAsync cancellationToken =
        task {
            while true do
                logger.LogInformation "Background service running."
                do! Task.Delay(2000, cancellationToken)
        }

    /// Called when a background service needs to gracefully shut down.
    override _.StopAsync cancellationToken =
        task { logger.LogInformation "Background service shutting down." }

/// This is essentially just the "ExecuteAsync" function. It is adapted into a full Background Service.
let workerAsFunction (sp: IServiceProvider) (cancellationToken: CancellationToken) : Task =
    let logger = sp.GetService<ILogger<unit>>()

    task {
        while true do
            logger.LogInformation "Functional background service running."
            do! Task.Delay(2000, cancellationToken)
    }

/// Using the IServiceProvider instead of a full DI approach.
type FullBackgroundServiceSP(serviceProvider:IServiceProvider) =
    inherit BackgroundService()

    let logger = serviceProvider.GetService<ILogger<unit>>()

    override _.ExecuteAsync cancellationToken =
        task {
            while true do
                logger.LogInformation "Service Factory service running."
                do! Task.Delay(2000, cancellationToken)
        }


let app =
    application {
        no_router

        // Using single function
        background_service workerAsFunction

        // Using service provider approach
        background_service (fun s -> new FullBackgroundServiceSP(s))

        // Using classic OO dependency injection
        service_config (fun s -> s.AddHostedService<FullBackgroundService>())
    }

run app

