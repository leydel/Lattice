﻿open Azure.Identity
open Azure.Messaging.EventGrid
open Azure.Messaging.ServiceBus
open FSharp.Discord.Rest
open Lattice.Orchestrator.Application
open Lattice.Orchestrator.Infrastructure.Discord
open Lattice.Orchestrator.Presentation
open Microsoft.Azure.Cosmos
open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions
open Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions
open Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.OpenApi.Models
open System
open System.IO
open Microsoft.DurableTask.Client

let (!) f = f |> ignore

HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(fun builder ->
        // Add environment variables to configuration
        !builder
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", true)
            .AddJsonFile("appsettings.json", true)
            .AddEnvironmentVariables()
    )
    .ConfigureServices(fun ctx services ->
        !services.Configure<SecretsOptions>(ctx.Configuration.GetSection(nameof SecretsOptions))

        !services.AddHttpClient()
        !services.AddLogging()
        !services.AddApplicationInsightsTelemetryWorkerService()
        !services.ConfigureFunctionsApplicationInsights()

        !services.AddSingleton<IDiscordClientFactory, DiscordClientFactory>()
        !services.AddSingleton<CosmosClient>(fun _ -> new CosmosClient(ctx.Configuration.GetValue<string>("CosmosDb")))
        !services.AddSingleton<EventGridPublisherClient>(fun sp -> new EventGridPublisherClient(Uri (ctx.Configuration.GetValue<string>("EventGridEndpoint")), DefaultAzureCredential()))
        !services.AddSingleton<ServiceBusClient>(fun _ -> new ServiceBusClient(ctx.Configuration.GetValue<string>("ServiceBus")))
        !services.AddDurableTaskClient(fun builder -> !builder.UseGrpc())

        !services.AddSingleton<IEnv, Env>()
        !services.AddSingleton<IDiscord>(fun sp -> sp.GetRequiredService<IEnv>() :> IDiscord)
        !services.AddSingleton<IEvents>(fun sp -> sp.GetRequiredService<IEnv>() :> IEvents)
        !services.AddSingleton<IPersistence>(fun sp -> sp.GetRequiredService<IEnv>() :> IPersistence)

        // Setup OpenAPI
        !services.AddSingleton<IOpenApiConfigurationOptions>(fun _ ->
            let license = OpenApiLicense()
            license.Name <- "MIT"
            license.Url <- Uri("https://github.com/leydel/Lattice/blob/main/LICENSE")

            let info = OpenApiInfo()
            info.Version <- DefaultOpenApiConfigurationOptions.GetOpenApiDocVersion()
            info.Title <- DefaultOpenApiConfigurationOptions.GetOpenApiDocTitle()
            info.Description <- DefaultOpenApiConfigurationOptions.GetOpenApiDocDescription()
            info.License <- license

            let options = OpenApiConfigurationOptions()
            options.Info <- info
            options.Servers <- DefaultOpenApiConfigurationOptions.GetHostNames()
            options.OpenApiVersion <- DefaultOpenApiConfigurationOptions.GetOpenApiVersion()
            options.IncludeRequestingHostName <- DefaultOpenApiConfigurationOptions.IsFunctionsRuntimeEnvironmentDevelopment()
            options.ForceHttps <- DefaultOpenApiConfigurationOptions.IsHttpsForced()
            options.ForceHttp <- DefaultOpenApiConfigurationOptions.IsHttpForced()
            options :> IOpenApiConfigurationOptions
        )

        // TODO: Setup auth then implement swagger auth like: https://github.com/Azure/azure-functions-openapi-extension/blob/main/samples/Microsoft.Azure.Functions.Worker.Extensions.OpenApi.FunctionApp.OutOfProc/Program.cs
        // TODO: Add OpenApiSecurity attributes to endpoints once above is done (and potential response types)
    )
    .ConfigureOpenApi()
    .Build()
    .Run()
