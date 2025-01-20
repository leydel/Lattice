﻿namespace rec Lattice.Orchestrator.Domain

type RegisteredApplication = {
    Id:              string
    DiscordBotToken: string
    DisabledReasons: int
}

module RegisteredApplication =
    let activate intents provisionedShardCount handler (app: RegisteredApplication) = {
        Id = app.Id
        DiscordBotToken = app.DiscordBotToken
        Intents = intents
        ProvisionedShardCount = provisionedShardCount
        Handler = handler
        DisabledReasons = app.DisabledReasons
    }

type ActivatedApplication = {
    Id:                    string
    DiscordBotToken:       string
    Intents:               int
    ProvisionedShardCount: int
    DisabledReasons:       int
    Handler:               Handler option
}

module ActivatedApplication =
    let addIntent intent (app: ActivatedApplication) =
        { app with Intents = app.Intents ||| intent }

    let removeIntent intent (app: ActivatedApplication) =
        { app with Intents = app.Intents &&& (~~~intent) }

    let setIntents intents (app: ActivatedApplication) =
        { app with Intents = intents }

    let setProvisionedShardCount provisionedShardCount (app: ActivatedApplication) =
        { app with ProvisionedShardCount = provisionedShardCount }

    let setHandler handler (app: ActivatedApplication) =
        { app with Handler = Some handler }

    let removeHandler (app: ActivatedApplication) =
        { app with Handler = None }
    
type Application =
    | REGISTERED of RegisteredApplication
    | ACTIVATED  of ActivatedApplication
    
module Application =
    let register id discordBotToken = {
        Id = id
        DiscordBotToken = discordBotToken
        DisabledReasons = 0
    }

    let setDiscordBotToken discordBotToken (app: Application) =
        match app with
        | REGISTERED app -> REGISTERED { app with DiscordBotToken = discordBotToken }
        | ACTIVATED app -> ACTIVATED { app with DiscordBotToken = discordBotToken }

    let addDisabledReason reason (app: Application) =
        match app with
        | REGISTERED app -> REGISTERED { app with DisabledReasons = app.DisabledReasons ||| reason }
        | ACTIVATED app -> ACTIVATED { app with DisabledReasons = app.DisabledReasons ||| reason }

    let removeDisabledReason reason (app: Application) =
        match app with
        | REGISTERED app -> REGISTERED { app with DisabledReasons = app.DisabledReasons &&& (~~~reason) }
        | ACTIVATED app -> ACTIVATED { app with DisabledReasons = app.DisabledReasons &&& (~~~reason) }
        