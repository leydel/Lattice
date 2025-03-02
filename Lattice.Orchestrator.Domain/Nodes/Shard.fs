﻿namespace Lattice.Orchestrator.Domain

open System

type ShardId = ShardId of applicationId: string * formulaId: int * numShards: int

module ShardId =
    let [<Literal>] SEPARATOR = ':'

    let create applicationId formulaId numShards =
        ShardId (applicationId, formulaId, numShards)

    let toString (ShardId (applicationId, formulaId, numShards)) =
        $"{applicationId}{SEPARATOR}{formulaId}{SEPARATOR}{numShards}"

    let fromString (str: string) =
        try
            match str.Split SEPARATOR with
            | [| applicationId; formulaId; numShards |] -> Some (ShardId (applicationId, int formulaId, int numShards))
            | _ -> None
        with | _ ->
            None

type ShardState =
    | NotStarted
    | Starting of next: Guid * startAt: DateTime
    | Active of current: Guid
    | Transferring of current: Guid * next: Guid * transferAt: DateTime
    | ShuttingDown of current: Guid * shutdownAt: DateTime
    | Shutdown of shutdownAt: DateTime

type Shard = {
    Id: ShardId
    Instances: (DateTime * Guid option) list
}

module Shard =
    let create applicationId formulaId numShards =
        {
            Id = ShardId.create applicationId formulaId numShards
            Instances = []
        }

    let addInstance nodeId createAt shard =
        { shard with Instances = shard.Instances @ [createAt, Some nodeId] }

    let shutdown shutdownAt shard =
        { shard with Instances = shard.Instances @ [shutdownAt, None] }

    let getState currentTime shard =
        match shard with
        | { Instances = [] } -> NotStarted
        | { Instances = (createAt, Some current) :: _ } when createAt <= currentTime -> Active current
        | { Instances = (transferAt, Some next) :: (_, Some current) :: _ } when transferAt > currentTime -> Transferring (current, next, transferAt)
        | { Instances = (createAt, Some next) :: _ } -> Starting (next, createAt)
        | { Instances = (shutdownAt, None) :: (_, Some current) :: _ } when shutdownAt > currentTime -> ShuttingDown (current, shutdownAt)
        | { Instances = (shutdownAt, None) :: _ } -> Shutdown shutdownAt
