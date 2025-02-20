﻿namespace Lattice.Orchestrator.Application

open Lattice.Orchestrator.Domain
open System
open System.Threading.Tasks

type IPersistence =
    abstract UpsertUser: user: User -> Task<Result<User, unit>>

    abstract GetApplicationById: id: string -> Task<Result<Application, unit>>
    abstract UpsertApplication: application: Application -> Task<Result<Application, unit>>
    abstract DeleteApplicationById: id: string -> Task<Result<unit, unit>>

    abstract GetNodeById: id: Guid -> Task<Result<Node, unit>>
    abstract UpsertNode: node: Node -> Task<Result<Node, unit>>
    abstract DeleteNodeById: id: string -> Task<Result<unit, unit>>

    abstract GetShardById: id: string -> Task<Result<Shard, unit>>
    abstract GetShardsByApplicationId: id: string -> Task<Result<Shard list, unit>>
    abstract UpsertShard: shard: Shard -> Task<Result<Shard, unit>>
    abstract DeleteShardById: id: string -> Task<Result<unit, unit>>
    