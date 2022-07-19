namespace RethinkDb.Driver.FSharp

open System
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Newtonsoft.Json.Linq
open RethinkDb.Driver
open RethinkDb.Driver.Net
open System.Threading.Tasks

/// Parameters for the RethinkDB configuration
type DataConfigParameter =
    | Hostname of string
    | Port of int
    | User of string * string
    | AuthKey of string
    | Timeout of int
    | Database of string


/// Connection builder function
module private ConnectionBuilder =
    let build (builder : Connection.Builder) block =
        match block with
        | Hostname x     -> builder.Hostname x
        | Port     x     -> builder.Port     x
        | User    (x, y) -> builder.User    (x, y)
        | AuthKey  x     -> builder.AuthKey  x
        | Timeout  x     -> builder.Timeout  x
        | Database x     -> builder.Db       x


/// RethinkDB configuration
type DataConfig =
    { Parameters : DataConfigParameter list }

    /// An empty configuration
    static member empty =
        { Parameters = [] }
    
    /// Build the connection from the given parameters
    member private this.BuildConnection () =
        this.Parameters
        |> Seq.fold ConnectionBuilder.build (RethinkDB.R.Connection ())
        
    /// Create a RethinkDB connection
    member this.CreateConnection () : IConnection =
        (this.BuildConnection ()).Connect ()

    /// Create a RethinkDB connection, logging the connection settings
    member this.CreateConnection (log : ILogger) : IConnection =
        let builder = this.BuildConnection ()
        if not (isNull log) then log.LogInformation $"Connecting to {this.EffectiveUri}"
        builder.Connect ()
    
    /// Create a RethinkDB connection
    member this.CreateConnectionAsync () : Task<Connection> =
        (this.BuildConnection ()).ConnectAsync ()
    
    /// Create a RethinkDB connection, logging the connection settings
    member this.CreateConnectionAsync (log : ILogger) : Task<Connection> =
        let builder = this.BuildConnection ()
        if not (isNull log) then log.LogInformation $"Connecting to {this.EffectiveUri}"
        builder.ConnectAsync ()
    
    /// The effective hostname
    member this.Hostname =
        match this.Parameters |> List.tryPick (fun x -> match x with Hostname _ -> Some x | _ -> None) with
        | Some (Hostname x) -> x
        | _ -> RethinkDBConstants.DefaultHostname    
    
    /// The effective port
    member this.Port =
        match this.Parameters |> List.tryPick (fun x -> match x with Port _ -> Some x | _ -> None) with
        | Some (Port x) -> x
        | _ -> RethinkDBConstants.DefaultPort
    
    /// The effective connection timeout
    member this.Timeout =
        match this.Parameters |> List.tryPick (fun x -> match x with Timeout _ -> Some x | _ -> None) with
        | Some (Timeout x) -> x
        | _ -> RethinkDBConstants.DefaultTimeout        
    
    /// The effective database
    member this.Database =
        match this.Parameters |> List.tryPick (fun x -> match x with Database _ -> Some x | _ -> None) with
        | Some (Database x) -> x
        | _ -> RethinkDBConstants.DefaultDbName
    
    /// The effective configuration URI (excludes password / auth key)
    member this.EffectiveUri =
        seq {
            "rethinkdb://"
            match this.Parameters |> List.tryPick (fun x -> match x with User _ -> Some x | _ -> None) with
            | Some (User (username, _)) -> $"{username}:***pw***@"
            | _ ->
                match this.Parameters |> List.tryPick (fun x -> match x with AuthKey _ -> Some x | _ -> None) with
                | Some (AuthKey _) -> "****key****@"
                | _ -> ()
            $"{this.Hostname}:{this.Port}/{this.Database}?timeout={this.Timeout}"
        }
        |> String.concat ""
    
    /// Parse settings from JSON
    ///
    /// A sample JSON object with all the possible properties filled in:
    /// {
    ///   "hostname" : "my-host",
    ///   "port" : 12345,
    ///   "username" : "my-user-name",
    ///   "password" : "my-password",
    ///   "auth-key" : "my-auth-key",
    ///   "timeout" : 77,
    ///   "database" : "default-db"
    /// }
    ///
    /// None of these properties are required, and properties not matching any of the above listed ones will be ignored.
    static member FromJson json =
        let parsed = JObject.Parse json
        { Parameters =
            [   match parsed["hostname"] with null -> () | x -> Hostname <| x.Value<string> ()
                match parsed["port"]     with null -> () | x -> Port     <| x.Value<int>    ()
                match parsed["auth-key"] with null -> () | x -> AuthKey  <| x.Value<string> ()
                match parsed["timeout"]  with null -> () | x -> Timeout  <| x.Value<int>    ()
                match parsed["database"] with null -> () | x -> Database <| x.Value<string> ()
                match parsed["username"], parsed["password"] with
                | null, _ | _, null -> () 
                | userName, password -> User (userName.Value<string> (), password.Value<string> ())
            ]
        }
    
    /// Parse settings from a JSON text file
    ///
    /// See doc for FromJson for the expected JSON format.
    static member FromJsonFile = System.IO.File.ReadAllText >> DataConfig.FromJson

    /// Parse settings from application configuration
    static member FromConfiguration (cfg : IConfigurationSection) =
        { Parameters =
            [   match cfg["hostname"] with null -> () | host -> Hostname host
                match cfg["port"]     with null -> () | port -> Port (int port)
                match cfg["auth-key"] with null -> () | key  -> AuthKey key
                match cfg["timeout"]  with null -> () | time -> Timeout (int time)
                match cfg["database"] with null -> () | db   -> Database db
                match cfg["username"], cfg["password"] with null, _ | _, null -> () | user -> User user
            ]
        }
    
    /// Parse settings from a URI
    ///
    /// rethinkdb://user:password@host:port/database?timeout=##
    ///   OR
    /// rethinkdb://authkey@host:port/database?timeout=##
    ///
    /// Scheme and host are required; all other settings optional
    static member FromUri (uri : string) =
        let it = Uri uri
        if it.Scheme <> "rethinkdb" then invalidArg "Scheme" $"""URI scheme must be "rethinkdb" (was {it.Scheme})"""
        { Parameters =
            [   Hostname it.Host
                if it.Port <> -1 then Port it.Port
                if it.UserInfo <> "" then
                    if it.UserInfo.Contains ":" then
                        let parts = it.UserInfo.Split ':' |> Array.truncate 2
                        User (parts[0], parts[1])
                    else AuthKey it.UserInfo
                if it.Segments.Length > 1 then Database it.Segments[1]
                if it.Query.Contains "?timeout=" then Timeout (int it.Query[9..])
            ]
        }