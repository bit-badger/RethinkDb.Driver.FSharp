namespace RethinkDb.Driver.FSharp

open Microsoft.Extensions.Configuration
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


/// RethinDB configuration
type DataConfig =
    { Parameters : DataConfigParameter list }

    /// An empty configuration
    static member empty =
        { Parameters = [] }

    /// Create a RethinkDB connection
    member this.CreateConnection () : IConnection =
        this.Parameters
        |> Seq.fold ConnectionBuilder.build (RethinkDB.R.Connection ())
        |> function builder -> builder.Connect ()

    /// Create a RethinkDB connection
    member this.CreateConnectionAsync () : Task<Connection> =
        this.Parameters
        |> Seq.fold ConnectionBuilder.build (RethinkDB.R.Connection ())
        |> function builder -> builder.ConnectAsync ()
    
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
            [ match parsed["hostname"] with null -> () | x -> Hostname <| x.Value<string> ()
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
            [ match cfg["hostname"] with null -> () | host -> Hostname host
              match cfg["port"]     with null -> () | port -> Port (int port)
              match cfg["auth-key"] with null -> () | key  -> AuthKey key
              match cfg["timeout"]  with null -> () | time -> Timeout (int time)
              match cfg["database"] with null -> () | db   -> Database db
              match cfg["username"], cfg["password"] with null, _ | _, null -> () | user -> User user
            ]
        }
