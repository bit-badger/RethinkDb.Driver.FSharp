namespace RethinkDb.Driver.FSharp

open RethinkDb.Driver
open RethinkDb.Driver.Ast


/// Delineates the type of bound when specifying bounded value ranges
type BoundType =
    /// The bound includes the bounded value
    | Open
    /// The bound excludes the bounded value
    | Closed
    
    /// The string representation of this bound used in the ReQL query
    member this.reql = match this with Open -> "open" | Closed -> "closed"


/// Optional arguments for the `between` statement
type BetweenOptArg =
    /// Use the specified index
    | Index of string
    /// The lower bound type
    | LowerBound of BoundType
    /// The upper bound type
    | UpperBound of BoundType

/// Function to support `between` optional arguments
module BetweenOptArg =
    
    /// Apply a list of optional arguments to a between statement
    let apply opts (b : Between) =
        opts
        |> List.fold (fun (btw : Between) arg ->
            match arg with
            | Index      idx -> btw.OptArg ("index",       idx)
            | LowerBound typ -> btw.OptArg ("lower_bound", typ.reql)
            | UpperBound typ -> btw.OptArg ("upper_bound", typ.reql))
            b


/// The durability of a write command
type Durability =
    /// Wait for write acknowledgement before returning
    | Hard
    /// Return before the write has been completely acknowledged
    | Soft
    
    /// The ReQL value of this argument
    member this.reql = "durability", match this with Hard -> "hard" | Soft -> "soft" 


/// Whether changes should be returned
type ReturnChanges =
    /// Return all documents considered for change ("always")
    | All
    /// Return changes
    | Changed
    /// Do not return changes
    | Nothing
    
    /// The ReQL value of this argument
    member this.reql = "return_changes", match this with All -> "always" :> obj | Changed -> true | Nothing -> false


/// Optional arguments for the `delete` statement
type DeleteOptArg =
    /// The durability of the command
    | Durability of Durability
    /// Whether changes should be returned
    | ReturnChanges of ReturnChanges
    /// Whether write hooks should be ignored (assuming the user has the permission to ignore hooks)
    | IgnoreWriteHook of bool

/// Function to support `delete` optional arguments
module DeleteOptArg =
    
    /// Apply a list of optional arguments to a delete statement
    let apply opts (d : Delete) =
        opts
        |> List.fold (fun (del : Delete) arg ->
            match arg with
            | Durability      dur -> del.OptArg dur.reql
            | ReturnChanges   chg -> del.OptArg chg.reql
            | IgnoreWriteHook ign -> del.OptArg ("ignore_write_hook", ign))
            d


/// How a filter command should handle filtering on a field that does not exist
type FilterDefaultHandling =
    /// Return documents where the filtered field is missing
    | Return
    /// Skip documents where the filtered field is missing
    | Skip
    /// Raise an error if the filtered field is missing from a document
    | Error
    
    /// The ReQL value for this default handling
    member this.reql = "default", match this with Return -> true :> obj | Skip -> false | Error -> RethinkDB.R.Error ()

    
/// Optional arguments for the `filter` statement
type FilterOptArg =
    | Default of FilterDefaultHandling

/// Function to support `filter` optional arguments
module FilterOptArg =
    
    /// Apply an option argument to the filter statement
    let apply arg (f : Filter) =
        match arg with Default d -> f.OptArg d.reql
        

/// Optional arguments for the `indexCreate` statement
type IndexCreateOptArg =
    /// Index multiple values in the given field
    | Multi
    /// Create a geospatial index
    | Geospatial
    
    /// The parameter for the ReQL OptArg call
    member this.reql = (match this with Multi -> "multi" | Geospatial -> "geo"), true

/// Function to support `indexCreate` optional arguments
module IndexCreateOptArg =
    
    /// Apply a list of optional arguments to an indexCreate statement
    let apply (opts : IndexCreateOptArg list) (ic : IndexCreate) =
        opts |> List.fold (fun (idxC : IndexCreate) arg -> idxC.OptArg arg.reql) ic


/// How to handle an insert conflict
type Conflict =
    /// Return an error
    | Error
    /// Replace the existing document with the current one
    | Replace
    /// Update fields in the existing document with fields from the current one
    | Update
    /// Use a function to resolve conflicts
    | Resolve of (ReqlExpr -> ReqlExpr -> ReqlExpr -> obj)
    
    /// The ReQL for the conflict parameter
    member this.reql =
        let value : obj =
            match this with
            | Error     -> "error"
            | Replace   -> "replace"
            | Update    -> "update"
            | Resolve f -> ReqlFunction3 f :> obj
        "conflict", value


/// Optional arguments for the `indexRename` statement
type IndexRenameOptArg =
    | FailIfExists
    | Overwrite

/// Function to support `indexRename` optional argument
module IndexRenameOptArg =
    
    /// Apply an optional argument to an indexRename statement
    let apply opt (ir : IndexRename) =
        ir.OptArg("overwrite", match opt with FailIfExists -> false | Overwrite -> true)

    
/// Optional arguments for the `insert` statement
type InsertOptArg =
    /// The durability of the command
    | Durability of Durability
    /// Whether changes should be returned
    | ReturnChanges of ReturnChanges
    /// How to handle conflicts
    | OnConflict of Conflict
    /// Whether write hooks should be ignored (assuming the user has the permission to ignore hooks)
    | IgnoreWriteHook of bool

/// Function to support `insert` optional arguments
module InsertOptArg =
    
    /// Apply a list of optional arguments to an insert statement
    let apply (opts : InsertOptArg list) (i : Insert) =
        opts
        |> List.fold (fun (ins : Insert) arg ->
            match arg with
            | Durability      dur -> ins.OptArg dur.reql
            | ReturnChanges   chg -> ins.OptArg chg.reql
            | OnConflict      con -> ins.OptArg con.reql
            | IgnoreWriteHook ign -> ins.OptArg ("ignore_write_hook", ign))
            i


/// Optional arguments for the `replace` statement
type ReplaceOptArg =
    /// The durability of the command
    | Durability of Durability
    /// Whether changes should be returned
    | ReturnChanges of ReturnChanges
    /// Allow the command to succeed if it is non-deterministic
    | NonAtomic of bool
    /// Whether write hooks should be ignored (assuming the user has the permission to ignore hooks)
    | IgnoreWriteHook of bool

/// Function to support `replace` optional arguments
module ReplaceOptArg =
    
    /// Apply a list of optional arguments to a replace statement
    let apply opts (r : Replace) =
        opts
        |> List.fold (fun (rep : Replace) arg ->
            match arg with
            | Durability      dur -> rep.OptArg dur.reql
            | ReturnChanges   chg -> rep.OptArg chg.reql
            | NonAtomic       non -> rep.OptArg ("non_atomic", non)
            | IgnoreWriteHook ign -> rep.OptArg ("ignore_write_hook", ign))
            r


/// How RethinkDB should read the data
type ReadMode =
    /// Return values in memory from the primary replica (default)
    | Single
    /// Return values committed to a majority of replicas
    | Majority
    /// Return values in memory from an arbitrary replica
    | Outdated
    
    /// The ReQL representation of the read mode
    member this.reql = match this with Single -> "single" | Majority -> "majority" | Outdated -> "outdated"


/// How the returned data should be formatted
type ReturnFormat =
    /// The native representation for the calling environment
    | Native
    /// The raw representation
    | Raw
    
    /// The ReQL representation of the format
    member this.reql = match this with Native -> "native" | Raw -> "raw"


/// Optional arguments for the `run` command
type RunOptArg =
    /// How data should be read
    | ReadMode of ReadMode
    /// The time format (raw is JSON)
    | TimeFormat of ReturnFormat
    /// Whether to profile the query
    | Profile of bool
    /// The durability of the command
    | Durability of Durability
    /// The format of grouped data and streams
    | GroupFormat of ReturnFormat
    /// The database against which this run should be executed
    | Db of string
    /// The maximum size of a returned array (RethinkDB default is 100,000)
    | ArrayLimit of int
    /// The format of binary data
    | BinaryFormat of ReturnFormat
    /// Minimum number of rows to wait before batching results (RethinkDB default is 8)
    | MinBatchRows of int
    /// Maximum number of rows to wait before batching results (RethinkDB default is no upper bound)
    | MaxBatchRows of int
    /// Maximum number of bytes to wait before batching results (RethinkDB default is 1MB)
    | MaxBatchBytes of int
    /// Maximum number of seconds to wait before batching results (RethinkDB default is 0.5)
    | MaxBatchSeconds of float
    /// Factor to reduce other values for the first batch
    | FirstBatchScaleDownFactor of int
    
    /// The ReQL representation of the optional argument
    member this.reql =
        let pair =
            match this with
            | ReadMode                  mde -> "read_mode",   mde.reql :> obj
            | TimeFormat                fmt -> "time_format", fmt.reql
            | Profile                   prf -> "profile",     prf
            | Durability                dur -> match dur.reql with k, v -> k, v
            | GroupFormat               fmt -> "group_format",                 fmt.reql
            | Db                        db  -> "db",                           db
            | ArrayLimit                lim -> "array_limit",                  lim
            | BinaryFormat              fmt -> "binary_format",                fmt.reql
            | MinBatchRows              min -> "min_batch_rows",               min
            | MaxBatchRows              max -> "max_batch_rows",               max
            | MaxBatchBytes             max -> "max_batch_bytes",              max
            | MaxBatchSeconds           max -> "max_batch_seconds",            max
            | FirstBatchScaleDownFactor fac -> "first_batch_scaledown_factor", fac
        fst pair, RethinkDB.R.Expr (snd pair)

/// Function to support `run` optional arguments
module RunOptArg =
    
    open RethinkDb.Driver.Model
    
    /// Create optional argument for a run command
    let create (opts : RunOptArg list) =
        let args = OptArgs ()
        opts |> List.iter (fun arg -> args.Add arg.reql)
        args


/// Definition of server tag/replica count
type ReplicaTag =
    /// A tagged server replica, along with the number of replicas per shard for that server 
    | ReplicaTag of string * int

/// Definition of replicas per shard when creating a table
type ReplicaSpec =
    /// Create this number of replicas per shard
    | Number of int
    /// Create the replicas across tagged servers, using the specified tag as the primary server
    | WithTags of string * ReplicaTag list

/// Optional arguments for creating tables
type TableCreateOptArg =
    /// The name of the primary key field (default is "id")
    | PrimaryKey of string
    /// The durability of the command
    | Durability of Durability
    /// The number of shards to create (1 to 64)
    | Shards of int
    /// The replicas per shard for this table
    | Replicas of ReplicaSpec

/// Functions to support `tableCreate` optional arguments
module TableCreateOptArg =
    
    /// Apply a list of optional arguments to a tableCreate statement
    let apply opts (tc : TableCreate) =
        opts
        |> List.fold (fun (tc : TableCreate) arg ->
            match arg with
            | PrimaryKey  pk -> tc.OptArg ("primary_key", pk)
            | Durability dur -> tc.OptArg dur.reql
            | Shards      sh -> tc.OptArg ("shards", sh)
            | Replicas   rep ->
                match rep with
                | Number count -> tc.OptArg ("replicas", count)
                | WithTags (primary, all) ->
                    let (ReplicaTag (firstTag, firstCount)) = List.head all
                    let replica =
                        all
                        |> List.skip 1
                        |> List.fold (fun (h : Model.MapObject) arg ->
                            let (ReplicaTag (tag, count)) = arg
                            h.With (tag, count))
                            (RethinkDB.R.HashMap (firstTag, firstCount))
                    tc.OptArg("replicas", replica).OptArg ("primary_replica_tag", primary)
            )
            tc

    
/// Optional arguments for the `update` statement
type UpdateOptArg =
    /// The durability of the command
    | Durability of Durability
    /// Whether changes should be returned
    | ReturnChanges of ReturnChanges
    /// Allow the command to succeed if it is non-deterministic
    | NonAtomic of bool
    /// Whether write hooks should be ignored (assuming the user has the permission to ignore hooks)
    | IgnoreWriteHook of bool

/// Function to support `update` optional arguments
module UpdateOptArg =
    
    /// Apply a list of optional arguments to an update statement
    let apply opts (u : Update) =
        opts
        |> List.fold (fun (upd : Update) arg ->
            match arg with
            | Durability      dur -> upd.OptArg dur.reql
            | ReturnChanges   chg -> upd.OptArg chg.reql
            | NonAtomic       non -> upd.OptArg ("non_atomic", non)
            | IgnoreWriteHook ign -> upd.OptArg ("ignore_write_hook", ign))
            u

