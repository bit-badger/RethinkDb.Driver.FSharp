namespace RethinkDb.Driver.FSharp

open Newtonsoft.Json.Linq
open RethinkDb.Driver
open RethinkDb.Driver.Net

/// Parameters for the RethinkDB configuration
type DataConfigParameter =
  | Hostname of string
  | Port of int
  | User of string * string
  | AuthKey of string
  | Timeout of int
  | Database of string

/// RethinDB configuration
type DataConfig =
  { Parameters : DataConfigParameter list }
with
  static member empty =
    { Parameters = [] }
  /// Create a RethinkDB connection
  member this.CreateConnection () : IConnection =
    let folder (builder : Connection.Builder) block =
      match block with
      | Hostname x     -> builder.Hostname x
      | Port     x     -> builder.Port     x
      | User    (x, y) -> builder.User    (x, y)
      | AuthKey  x     -> builder.AuthKey  x
      | Timeout  x     -> builder.Timeout  x
      | Database x     -> builder.Db       x
    let bldr =
      this.Parameters
      |> Seq.fold folder (RethinkDB.R.Connection ())
    upcast bldr.Connect ()
  /// The effective hostname
  member this.Hostname =
    match this.Parameters
          |> List.tryPick (fun x -> match x with Hostname _ -> Some x | _ -> None) with
    | Some (Hostname x) -> x
    | _ -> RethinkDBConstants.DefaultHostname    
  /// The effective port
  member this.Port =
    match this.Parameters
          |> List.tryPick (fun x -> match x with Port _ -> Some x | _ -> None) with
    | Some (Port x) -> x
    | _ -> RethinkDBConstants.DefaultPort
  /// The effective connection timeout
  member this.Timeout =
    match this.Parameters
          |> List.tryPick (fun x -> match x with Timeout _ -> Some x | _ -> None) with
    | Some (Timeout x) -> x
    | _ -> RethinkDBConstants.DefaultTimeout        
  /// The effective database
  member this.Database =
    match this.Parameters
          |> List.tryPick (fun x -> match x with Database _ -> Some x | _ -> None) with
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
    let isNotNull = not << isNull
    let parsed = JObject.Parse json
    let config =
      seq {
        match parsed.["hostname"] with x when isNotNull x -> yield Hostname <| x.Value<string> () | _ -> ()
        match parsed.["port"]     with x when isNotNull x -> yield Port     <| x.Value<int>    () | _ -> ()
        match parsed.["auth-key"] with x when isNotNull x -> yield AuthKey  <| x.Value<string> () | _ -> ()
        match parsed.["timeout"]  with x when isNotNull x -> yield Timeout  <| x.Value<int>    () | _ -> ()
        match parsed.["database"] with x when isNotNull x -> yield Database <| x.Value<string> () | _ -> ()
        let userName = parsed.["username"]
        let password = parsed.["password"]
        match isNotNull userName && isNotNull password with
        | true -> yield User (userName.Value<string> (), password.Value<string> ())
        | _ -> ()
      }
      |> List.ofSeq
    { Parameters = config }
  /// Parse settings from a JSON text file
  ///
  /// See doc for FromJson for the expected JSON format.
  static member FromJsonFile = System.IO.File.ReadAllText >> DataConfig.FromJson