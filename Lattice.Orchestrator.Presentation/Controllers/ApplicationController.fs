﻿namespace Lattice.Orchestrator.Presentation

open Lattice.Orchestrator.Application
open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Http
open Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes
open Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums
open Microsoft.OpenApi.Models
open System
open System.Net

type ApplicationController (env: IEnv) =
    [<Function "RegisterApplication">]
    [<OpenApiOperation(operationId = "RegisterApplication", tags = [| "application" |], Summary = "Register an application", Description = "Uses the Discord bot token to setup and handle the Discord gateway connection", Visibility = OpenApiVisibilityType.Advanced)>]
    [<OpenApiRequestBody("application/json", typeof<RegisterApplicationPayload>, Required = true, Description = "The required payload for this endpoint")>]
    [<OpenApiResponseWithBody(HttpStatusCode.Created, "application/json", typeof<ApplicationResponse>, Description = "Application successfully registered")>]
    [<OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof<ErrorResponse>, Description = "Invalid values provided in payload")>]
    [<OpenApiResponseWithBody(HttpStatusCode.Conflict, "application/json", typeof<ErrorResponse>, Description = "Application already registered")>]
    member _.RegisterApplication (
        [<HttpTrigger(AuthorizationLevel.Anonymous, "post", "applications")>] req: HttpRequestData,
        [<FromBody>] payload: RegisterApplicationPayload
    ) = task {
        let! res = RegisterApplicationCommand.run env {
            DiscordBotToken = payload.DiscordBotToken
        }

        match res with
        | Error RegisterApplicationCommandError.InvalidToken ->
            let res = req.CreateResponse HttpStatusCode.BadRequest
            do! res.WriteAsJsonAsync (ErrorResponse.fromCode ErrorResponseCode.INVALID_TOKEN)
            return res

        | Error RegisterApplicationCommandError.RegistrationFailed ->
            let res = req.CreateResponse HttpStatusCode.InternalServerError
            do! res.WriteAsJsonAsync (ErrorResponse.fromCode ErrorResponseCode.INTERNAL_SERVER_ERROR)
            return res

        | Ok application ->
            let res = req.CreateResponse HttpStatusCode.OK
            do! res.WriteAsJsonAsync (ApplicationResponse.fromDomain application)
            return res
    }

    [<Function "GetApplications">]
    [<OpenApiOperation(operationId = "GetApplications", tags = [| "application" |], Summary = "Get a paginated list of applications setup", Description = "Returns a paginated list of applications setup", Visibility = OpenApiVisibilityType.Advanced)>]
    [<OpenApiParameter("before", In = ParameterLocation.Query, Required = false, Type = typeof<string>, Summary = "List applications before this ID", Description = "The ID of the first application included in the previous query")>]
    [<OpenApiParameter("after", In = ParameterLocation.Query, Required = false, Type = typeof<string>, Summary = "List applications after this ID", Description = "The ID of the last application included in the previous query")>]
    [<OpenApiParameter("limit", In = ParameterLocation.Query, Required = false, Type = typeof<string>, Summary = "The maximum number of results returned", Description = "Limits the number of results returned per query to this amount")>]
    [<OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof<ApplicationResponse list>, Description = "List of applications registered (can be empty)")>]
    member _.GetApplications (
        [<HttpTrigger(AuthorizationLevel.Anonymous, "get", "applications")>] req: HttpRequestData
    ) = task {
        let before =
            req.Query
            |> NameValueCollection.tryGetValue "before"

        let after =
            req.Query
            |> NameValueCollection.tryGetValue "after"

        let limit = 
            req.Query
            |> NameValueCollection.tryGetValue "limit"
            |> Option.map Int32.TryParse
            |> Option.filter fst
            |> Option.map snd

        let! res = GetApplicationsQuery.run env {
            Before = before
            After = after
            Limit = limit
        }

        match res with
        | _ -> return req.CreateResponse HttpStatusCode.NotImplemented
    }

    [<Function "GetApplication">]
    [<OpenApiOperation(operationId = "GetApplication", tags = [| "application" |], Summary = "Fetch an application by its ID", Description = "Checks if a bot is setup, and if so returns information about it", Visibility = OpenApiVisibilityType.Advanced)>]
    [<OpenApiParameter("applicationId", In = ParameterLocation.Path, Required = true, Type = typeof<string>, Summary = "The ID of the application", Description = "The ID of the application")>]
    [<OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof<ApplicationResponse>, Description = "Application found")>]
    [<OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof<ErrorResponse>, Description = "Application not found")>]
    member _.GetApplication (
        [<HttpTrigger(AuthorizationLevel.Anonymous, "get", "applications/{applicationId}")>] req: HttpRequestData,
        applicationId: string
    ) = task {
        let! res = GetApplicationQuery.run env {
            ApplicationId = applicationId
        }

        match res with
        | Error GetApplicationQueryError.ApplicationNotFound ->
            let res = req.CreateResponse HttpStatusCode.NotFound
            do! res.WriteAsJsonAsync (ErrorResponse.fromCode ErrorResponseCode.APPLICATION_NOT_FOUND)
            return res

        | Ok application ->
            let res = req.CreateResponse HttpStatusCode.OK
            do! res.WriteAsJsonAsync (ApplicationResponse.fromDomain application)
            return res
    }

    [<Function "UpdateApplication">]
    [<OpenApiOperation(operationId = "UpdateApplication", tags = [| "application" |], Summary = "Update properties on an application", Description = "Used to update various configurations and details for an application", Visibility = OpenApiVisibilityType.Advanced)>]
    [<OpenApiParameter("applicationId", In = ParameterLocation.Path, Required = true, Type = typeof<string>, Summary = "The ID of the application", Description = "The ID of the application")>]
    [<OpenApiRequestBody("application/json", typeof<UpdateApplicationPayload>, Required = true, Description = "The required payload for this endpoint")>]
    [<OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof<ApplicationResponse>, Description = "Application updated")>]
    [<OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof<ErrorResponse>, Description = "Invalid values provided in payload")>]
    [<OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof<ErrorResponse>, Description = "Application not found")>]
    member _.UpdateApplication (
        [<HttpTrigger(AuthorizationLevel.Anonymous, "put", "applications/{applicationId}")>] req: HttpRequestData,
        [<FromBody>] payload: UpdateApplicationPayload,
        applicationId: string
    ) = task {
        let! res = UpdateApplicationCommand.run env {
            ApplicationId = applicationId
            DiscordBotToken = payload.DiscordBotToken
            Intents = payload.Intents
            ShardCount = payload.ShardCount
            DisabledReasons = payload.DisabledReasons
        }

        match res with
        | _ -> return req.CreateResponse HttpStatusCode.NotImplemented
    }

    [<Function "DeleteApplication">]
    [<OpenApiOperation(operationId = "DeleteApplication", tags = [| "application" |], Summary = "Remove an application", Description = "Removes any data associated with the given application", Visibility = OpenApiVisibilityType.Advanced)>]
    [<OpenApiParameter("applicationId", In = ParameterLocation.Path, Required = true, Type = typeof<string>, Summary = "The ID of the application", Description = "The ID of the application")>]
    [<OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Application deleted")>]
    [<OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof<ErrorResponse>, Description = "Application not found")>]
    member _.DeleteApplication (
        [<HttpTrigger(AuthorizationLevel.Anonymous, "delete", "applications/{applicationId}")>] req: HttpRequestData,
        applicationId: string
    ) = task {
        let! res = DeleteApplicationCommand.run env {
            ApplicationId = applicationId
        }

        match res with
        | Error DeleteApplicationCommandError.ApplicationNotFound ->
            let res = req.CreateResponse HttpStatusCode.NotFound
            do! res.WriteAsJsonAsync (ErrorResponse.fromCode ErrorResponseCode.APPLICATION_NOT_FOUND)
            return res

        | _ ->
            return req.CreateResponse HttpStatusCode.NoContent
    }

    [<Function "SetApplicationHandler">]
    [<OpenApiOperation(operationId = "SetApplicationHandler", tags = [| "application"; "handler" |], Summary = "Sets the handler for an application", Description = "Replaces any existing handler with the given handler", Visibility = OpenApiVisibilityType.Advanced)>]
    [<OpenApiParameter("applicationId", In = ParameterLocation.Path, Required = true, Type = typeof<string>, Summary = "The ID of the application", Description = "The ID of the application")>]
    [<OpenApiRequestBody("application/json", typeof<SetApplicationHandlerPayload>, Required = true, Description = "The required payload for this endpoint")>]
    [<OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof<HandlerResponse>, Description = "Application handler set")>]
    [<OpenApiResponseWithBody(HttpStatusCode.BadRequest, "application/json", typeof<ErrorResponse>, Description = "Invalid values provided in payload")>]
    [<OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof<ErrorResponse>, Description = "Application not found")>]
    member _.SetApplicationHandler (
        [<HttpTrigger(AuthorizationLevel.Anonymous, "put", "applications/{applicationId}/handler")>] req: HttpRequestData,
        [<FromBody>] payload: SetApplicationHandlerPayload,
        applicationId: string
    ) = task {
        match payload with
        | SetApplicationHandlerPayload.WEBHOOK payload ->
            let! res = SetWebhookApplicationHandlerCommand.run env {
                ApplicationId = applicationId
                Endpoint = payload.Endpoint
            }

            match res with
            | _ -> return req.CreateResponse HttpStatusCode.NotImplemented

        | SetApplicationHandlerPayload.SERVICE_BUS payload ->
            let! res = SetServiceBusApplicationHandlerCommand.run env {
                ApplicationId = applicationId
                ConnectionString = payload.ConnectionString
                QueueName = payload.QueueName
            }

            match res with
            | _ -> return req.CreateResponse HttpStatusCode.NotImplemented
    }

    [<Function "RemoveApplicationHandler">]
    [<OpenApiOperation(operationId = "RemoveApplicationHandler", tags = [| "application"; "handler" |], Summary = "Removes the handler for the application", Description = "Removes the handler for the application, which also deactivates the bot", Visibility = OpenApiVisibilityType.Advanced)>]
    [<OpenApiParameter("applicationId", In = ParameterLocation.Path, Required = true, Type = typeof<string>, Summary = "The ID of the application", Description = "The ID of the application")>]
    [<OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Application handler removed")>]
    [<OpenApiResponseWithBody(HttpStatusCode.NotFound, "application/json", typeof<ErrorResponse>, Description = "Application not found")>]
    member _.RemoveApplicationHandler (
        [<HttpTrigger(AuthorizationLevel.Anonymous, "delete", "applications/{applicationId}/handler")>] req: HttpRequestData,
        applicationId: string
    ) = task {
        let! res = RemoveApplicationHandlerCommand.run env {
            ApplicationId = applicationId
        }

        match res with
        | _ -> return req.CreateResponse HttpStatusCode.NotImplemented
    }
