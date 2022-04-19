module RethinkDb.Driver.FSharp.Retry

open System
open System.Threading.Tasks
open Polly
open RethinkDb.Driver
open RethinkDb.Driver.Net

/// Create a retry policy that attempts to reconnect to RethinkDB on each retry
let retryPolicy (intervals : float seq) (conn : IConnection) =
    Policy
        .Handle<ReqlDriverError>()
        .WaitAndRetryAsync(
            intervals |> Seq.map TimeSpan.FromSeconds,
            System.Action<exn, TimeSpan, int, Context> (fun ex _ _ _ ->
                printf $"Encountered RethinkDB exception: {ex.Message}"
                match ex.Message.Contains "socket" with
                | true ->
                    printf "Reconnecting to RethinkDB"
                    (conn :?> Connection).Reconnect false
                | false -> ()))

/// Perform a query, retrying after each delay specified
let withRetry (f : IConnection -> Task<'T>) retries =
    fun conn -> backgroundTask {
        return! (retryPolicy retries conn).ExecuteAsync(fun () -> f conn)
    }

/// Retry three times, after 200ms, 500ms, and 1 second
let withRetryDefault f =
    withRetry f [ 0.2; 0.5; 1.0 ]

/// Retry one time immediately
let withRetryOnce f =
    withRetry f [ 0.0 ]
