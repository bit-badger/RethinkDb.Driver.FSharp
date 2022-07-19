module Tests

open System
open RethinkDb.Driver
open RethinkDb.Driver.FSharp.Functions
open Types
open Xunit

let private r = RethinkDB.R

[<Fact>]
let ``My test`` () =
    Assert.True(true)

let ``dbCreate succeeds`` () =
    dbCreate "fsharp_driver_test"
    |> runWrite