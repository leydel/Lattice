﻿namespace Lattice.Orchestrator.Application

open Lattice.Orchestrator.Domain
open System.Threading.Tasks

type IPersistence =
    abstract UpsertUser: user: User -> Task<Result<User, unit>>

    abstract GetApplicationById: id: string -> Task<Result<Application, unit>>
    abstract UpsertApplication: application: Application -> Task<Result<Application, unit>>
    abstract DeleteApplicationById: id: string -> Task<Result<unit, unit>>

    abstract GetCachedApplicationTeam: applicationId: string -> Task<Result<Team, unit>>
    abstract UpsertCachedApplicationTeam: team: Team -> Task<Result<Team, unit>>
    abstract DeleteCachedApplicationTeam: applicationId: string -> Task<Result<unit, unit>>
