[<AutoOpen>]
module RethinkDb.Driver.FSharp.RethinkBuilder

open RethinkDb.Driver
open RethinkDb.Driver.Ast
open RethinkDb.Driver.FSharp
open RethinkDb.Driver.Net
open System.Threading.Tasks

/// Computation Expression builder for RethinkDB queries
type RethinkBuilder<'T> () =
    
    /// Await a Task (avoids using the task CE when we may raise exceptions)
    let await = Async.AwaitTask >> Async.RunSynchronously

    /// Create a RethinkDB hash map of the given field/value pairs
    let fieldsToMap (fields : (string * obj) list) =
        fields
        |> List.fold (fun (m : Model.MapObject) item -> m.With (fst item, snd item)) (RethinkDB.R.HashMap ())
    
    /// Split a table name with a "." separator into database and table parts
    let dbAndTable (table : string) =
        match table.Contains "." with
        | true ->
            let parts = table.Split '.'
            Some parts[0], parts[1]
        | false -> None, table
    
    member _.Bind (expr : ReqlExpr, f : ReqlExpr -> ReqlExpr) = f expr
  
    member this.For (expr, f) = this.Bind (expr, f)
    
    member _.Yield _ = RethinkDB.R
    
    // database/table identification
    
    /// Specify a database for further commands
    [<CustomOperation "withDb">]
    member _.Db (r : RethinkDB, db : string) =
        match db with "" -> invalidArg db "db name cannot be blank" | _ -> r.Db db
    
    /// Identify a table (of form "dbName.tableName"; if no db name, uses default database)
    [<CustomOperation "withTable">]
    member this.Table (r : RethinkDB, table : string) =
        match dbAndTable table with
        | Some db, tbl -> this.Db(r, db).Table tbl
        | None, _ -> r.Table table
    
    /// Create an equality join with another table
    [<CustomOperation "eqJoin">]
    member _.EqJoin (expr : ReqlExpr, field : string, otherTable : string) =
        expr.EqJoin (field, RethinkDB.R.Table otherTable)
    
    // meta queries (tables, indexes, etc.)
    
    /// List all databases
    [<CustomOperation "dbList">]
    member _.DbList (r : RethinkDB) = r.DbList ()
    
    /// Create a database
    [<CustomOperation "dbCreate">]
    member _.DbCreate (r : RethinkDB, db : string) = r.DbCreate db
    
    /// Drop a database
    [<CustomOperation "dbDrop">]
    member _.DbDrop (r : RethinkDB, db : string) = r.DbDrop db
    
    /// List all tables for the default database
    [<CustomOperation "tableList">]
    member _.TableList (r : RethinkDB) = r.TableList ()
    
    /// List all tables for the specified database
    [<CustomOperation "tableList">]
    member this.TableList (r : RethinkDB, db : string) =
        match db with "" -> this.TableList r | _ -> this.Db(r, db).TableList ()
    
    /// Create a table (of form "dbName.tableName"; if no db name, uses default database)
    [<CustomOperation "tableCreate">]
    member this.TableCreate (r : RethinkDB, table : string) =
        match dbAndTable table with
        | Some db, tbl -> this.Db(r, db).TableCreate tbl
        | None, _ -> r.TableCreate table
    
    /// Drop a table (of form "dbName.tableName"; if no db name, uses default database)
    [<CustomOperation "tableDrop">]
    member this.TableDrop (r : RethinkDB, table : string) =
        match dbAndTable table with
        | Some db, tbl -> this.Db(r, db).TableDrop tbl
        | None, _ -> r.TableDrop table
    
    /// List all indexes for a table
    [<CustomOperation "indexList">]
    member _.IndexList (tbl : Table) = tbl.IndexList ()
    
    /// Create an index for a table
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl : Table, index : string) = tbl.IndexCreate index
    
    /// Create an index for a table, specifying an optional argument
    [<CustomOperation "indexCreate">]
    member this.IndexCreate (tbl : Table, index : string, opts : IndexCreateOptArg list) =
        this.IndexCreate (tbl, index) |> IndexCreateOptArg.apply opts
    
    /// Create an index for a table, using a function to calculate the index
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl : Table, index : string, f : ReqlExpr -> obj) = tbl.IndexCreate (index, ReqlFunction1 f)
    
    /// Create an index for a table, using a function to calculate the index
    [<CustomOperation "indexCreate">]
    member this.IndexCreate (tbl : Table, index : string, f : ReqlExpr -> obj, opts : IndexCreateOptArg list) =
        this.IndexCreate (tbl, index, f) |> IndexCreateOptArg.apply opts

    /// Drop an index for a table
    [<CustomOperation "indexDrop">]
    member _.IndexDrop (tbl : Table, index : string) = tbl.IndexDrop index
    
    /// Rename an index on a table
    [<CustomOperation "indexRename">]
    member _.IndexRename (tbl : Table, oldName : string, newName : string) = tbl.IndexRename (oldName, newName)
    
    /// Rename an index on a table, specifying an overwrite option
    [<CustomOperation "indexRename">]
    member this.IndexRename (tbl : Table, oldName : string, newName : string, arg : IndexRenameOptArg) =
        this.IndexRename(tbl, oldName, newName) |> IndexRenameOptArg.apply arg
    
    /// Get the status of all indexes on a table
    [<CustomOperation "indexStatus">]
    member _.IndexStatus (tbl : Table) = tbl.IndexStatus ()
    
    /// Get the status of specific indexes on a table
    [<CustomOperation "indexStatus">]
    member _.IndexStatus (tbl : Table, indexes : string list) = tbl.IndexStatus (Array.ofList indexes)
    
    /// Wait for all indexes on a table to become ready
    [<CustomOperation "indexWait">]
    member _.IndexWait (tbl : Table) = tbl.IndexWait ()
    
    /// Wait for specific indexes on a table to become ready
    [<CustomOperation "indexWait">]
    member _.IndexWait (tbl : Table, indexes : string list) = tbl.IndexWait (Array.ofList indexes)
    
    // data retrieval / manipulation
    
    /// Get a document from a table by its ID
    [<CustomOperation "get">]
    member _.Get (tbl : Table, key : obj) = tbl.Get key
    
    /// Get all documents matching the given primary key value
    [<CustomOperation "getAll">]
    member _.GetAll (tbl : Table, keys : obj list) =
        tbl.GetAll (Array.ofList keys)
    
    /// Get all documents matching the given index value
    [<CustomOperation "getAll">]
    member this.GetAll (tbl : Table, keys : obj list, index : string) =
        this.GetAll(tbl, keys).OptArg ("index", index)
    
    /// Skip a certain number of results
    [<CustomOperation "skip">]
    member _.Skip (expr : ReqlExpr, toSkip : int) = expr.Skip toSkip

    /// Limit the results of this query
    [<CustomOperation "limit">]
    member _.Limit (expr : ReqlExpr, limit : int) = expr.Limit limit

    /// Count documents for the current query
    [<CustomOperation "count">]
    member _.Count (expr : ReqlExpr) = expr.Count ()
    
    /// Filter a query by a single field value
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, field : string, value : obj) = expr.Filter (fieldsToMap [ field, value ])
    
    /// Filter a query by a single field value, including an optional argument
    [<CustomOperation "filter">]
    member this.Filter (expr : ReqlExpr, field : string, value : obj, opt : FilterOptArg) =
        this.Filter (expr, field, value) |> FilterOptArg.apply opt
    
    /// Filter a query by multiple field values
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, filter : (string * obj) list) = expr.Filter (fieldsToMap filter)
    
    /// Filter a query by multiple field values, including an optional argument
    [<CustomOperation "filter">]
    member this.Filter (expr : ReqlExpr, filter : (string * obj) list, opt : FilterOptArg) =
        this.Filter (expr, filter) |> FilterOptArg.apply opt
    
    /// Filter a query by a function
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, f : ReqlExpr -> obj) = expr.Filter (ReqlFunction1 f)
    
    /// Filter a query by a function, including an optional argument
    [<CustomOperation "filter">]
    member this.Filter (expr : ReqlExpr, f : ReqlExpr -> obj, opt : FilterOptArg) =
        this.Filter (expr, f) |> FilterOptArg.apply opt
    
    /// Filter a query by multiple functions (has the effect of ANDing them)
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, fs : (ReqlExpr -> obj) list) : Filter =
        (fs |> List.fold (fun (e : ReqlExpr) f -> e.Filter (ReqlFunction1 f)) expr) :?> Filter
    
    /// Filter a query by multiple functions (has the effect of ANDing them), including an optional argument
    [<CustomOperation "filter">]
    member this.Filter (expr : ReqlExpr, fs : (ReqlExpr -> obj) list, opt : FilterOptArg) =
        this.Filter (expr, fs) |> FilterOptArg.apply opt
    
    /// Filter a query using a JavaScript expression
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, js : string) = expr.Filter (Javascript js)
    
    /// Filter a query using a JavaScript expression, including an optional argument
    [<CustomOperation "filter">]
    member this.Filter (expr : ReqlExpr, js : string, opt : FilterOptArg) =
        this.Filter (expr, js) |> FilterOptArg.apply opt
    
    /// Filter a query by a range of values
    [<CustomOperation "between">]
    member _.Between (expr : ReqlExpr, lower : obj, upper : obj) =
        expr.Between (lower, upper)
    
    /// Filter a query by a range of values, using optional arguments
    [<CustomOperation "between">]
    member this.Between (expr : ReqlExpr, lower : obj, upper : obj, opts : BetweenOptArg list) =
        this.Between (expr, lower, upper) |> BetweenOptArg.apply opts
        
    /// Map fields for the current query
    [<CustomOperation "map">]
    member _.Map (expr : ReqlExpr, f : ReqlExpr -> obj) = expr.Map (ReqlFunction1 f)

    /// Exclude the given fields from the output
    [<CustomOperation "without">]
    member _.Without (expr : ReqlExpr, fields : obj list) = expr.Without (Array.ofList fields)
    
    /// Combine a left and right selection into a single record
    [<CustomOperation "zip">]
    member _.Zip (expr : ReqlExpr) = expr.Zip ()
    
    /// Merge a document into the current query
    [<CustomOperation "merge">]
    member _.Merge (expr : ReqlExpr, f : ReqlExpr -> obj) = expr.Merge (ReqlFunction1 f)
    
    /// Pluck (select only) specific fields from the query
    [<CustomOperation "pluck">]
    member _.Pluck (expr : ReqlExpr, fields : string list) = expr.Pluck (Array.ofList fields)
    
    /// Order the results by the given field value (ascending)
    [<CustomOperation "orderBy">]
    member _.OrderBy (expr : ReqlExpr, field : string) = expr.OrderBy field

    /// Order the results by the given function value
    [<CustomOperation "orderBy">]
    member _.OrderBy (expr : ReqlExpr, f : ReqlExpr -> obj) = expr.OrderBy (ReqlFunction1 f)

    /// Order the results by the given field value (descending)
    [<CustomOperation "orderByDescending">]
    member _.OrderByDescending (expr : ReqlExpr, field : string) = expr.OrderBy (RethinkDB.R.Desc field)

    /// Insert a document into the given table
    [<CustomOperation "insert">]
    member _.Insert (tbl : Table, doc : obj) = tbl.Insert doc
    
    /// Insert a document into the given table, using optional arguments
    [<CustomOperation "insert">]
    member this.Insert (tbl : Table, doc : obj, opts : InsertOptArg list) =
        this.Insert (tbl, doc) |> InsertOptArg.apply opts
    
    /// Update specific fields in a document
    [<CustomOperation "update">]
    member _.Update (expr : ReqlExpr, fields : (string * obj) list) = expr.Update (fieldsToMap fields)
    
    /// Update specific fields in a document, using optional arguments
    [<CustomOperation "update">]
    member this.Update (expr : ReqlExpr, fields : (string * obj) list, args : UpdateOptArg list) =
        this.Update (expr, fields) |> UpdateOptArg.apply args
    
    /// Update specific fields in a document using a function
    [<CustomOperation "update">]
    member _.Update (expr : ReqlExpr, f : ReqlExpr -> obj) = expr.Update (ReqlFunction1 f)
    
    /// Update specific fields in a document using a function, with optional arguments
    [<CustomOperation "update">]
    member this.Update (expr : ReqlExpr, f : ReqlExpr -> obj, args : UpdateOptArg list) =
        this.Update (expr, f) |> UpdateOptArg.apply args
    
    /// Update specific fields in a document using a JavaScript function
    [<CustomOperation "update">]
    member _.Update (expr : ReqlExpr, js : string) = expr.Update (Javascript js)
    
    /// Update specific fields in a document using a JavaScript function, with optional arguments
    [<CustomOperation "update">]
    member this.Update (expr : ReqlExpr, js : string, args : UpdateOptArg list) =
        this.Update (expr, js) |> UpdateOptArg.apply args
    
    /// Replace the current query with the specified document
    [<CustomOperation "replace">]
    member _.Replace (expr : ReqlExpr, doc : obj) = expr.Replace doc
    
    /// Replace the current query with the specified document, using optional arguments
    [<CustomOperation "replace">]
    member this.Replace (expr : ReqlExpr, doc : obj, args : ReplaceOptArg list) =
        this.Replace (expr, doc) |> ReplaceOptArg.apply args
    
    /// Replace the current query with document(s) created by a function
    [<CustomOperation "replace">]
    member _.Replace (expr : ReqlExpr, f : ReqlExpr -> obj) = expr.Replace (ReqlFunction1 f)
    
    /// Replace the current query with document(s) created by a function, using optional arguments
    [<CustomOperation "replace">]
    member this.Replace (expr : ReqlExpr, f : ReqlExpr -> obj, args : ReplaceOptArg list) =
        this.Replace (expr, f) |> ReplaceOptArg.apply args
    
    /// Replace the current query with document(s) created by a JavaScript function
    [<CustomOperation "replace">]
    member _.Replace (expr : ReqlExpr, js : string) = expr.Replace (Javascript js)
    
    /// Replace the current query with document(s) created by a JavaScript function, using optional arguments
    [<CustomOperation "replace">]
    member this.Replace (expr : ReqlExpr, js : string, args : ReplaceOptArg list) =
        this.Replace (expr, js) |> ReplaceOptArg.apply args
    
    /// Delete the document(s) identified by the current query
    [<CustomOperation "delete">]
    member _.Delete (expr : ReqlExpr) = expr.Delete ()

    /// Delete the document(s) identified by the current query
    [<CustomOperation "delete">]
    member this.Delete (expr : ReqlExpr, opts : DeleteOptArg list) =
        this.Delete expr |> DeleteOptArg.apply opts

    /// Wait for updates to a table to be synchronized to disk
    [<CustomOperation "sync">]
    member _.Sync (tbl : Table) = tbl.Sync ()

    // executing queries
    
    /// Execute the query, returning the result of the type specified
    [<CustomOperation "result">]
    member _.Result (expr : ReqlExpr) : IConnection -> Task<'T> = 
        fun conn -> backgroundTask {
            return! expr.RunResultAsync<'T> conn
        }
    
    /// Execute the query, returning the result of the type specified
    [<CustomOperation "result">]
    member this.Result (expr, conn) =
        this.Result expr conn
    
    /// Execute the query, returning the result of the type specified, or None if no result is found
    [<CustomOperation "resultOption">]
    member _.ResultOption (expr : ReqlExpr) : IConnection -> Task<'T option> = 
        fun conn -> backgroundTask {
            let! result = expr.RunResultAsync<'T> conn
            return match (box >> isNull) result with true -> None | false -> Some result
        }
    
    /// Execute the query, returning the result of the type specified, or None if no result is found
    [<CustomOperation "resultOption">]
    member this.ResultOption (expr, conn) =
        this.ResultOption expr conn
    
    /// Perform a write operation
    [<CustomOperation "write">]
    member _.Write (expr : ReqlExpr) : IConnection -> Task<Model.Result> =
        fun conn ->
            let result = expr.RunWriteAsync conn |> await
            match result.Errors with
            | 0UL -> Task.FromResult result
            | _ -> raise <| ReqlRuntimeError result.FirstError

    /// Perform a write operation
    [<CustomOperation "write">]
    member this.Write (expr, conn) =
        this.Write expr conn

    /// Ignore the result of an operation
    [<CustomOperation "ignoreResult">]
    member _.IgnoreResult<'T> (f : IConnection -> Task<'T>) =
        fun conn -> task {
            let! _ = (f conn).ConfigureAwait false
            ()
        }

    /// Ignore the result of an operation
    [<CustomOperation "ignoreResult">]
    member _.IgnoreResult (f : IConnection -> Task<'T option>) =
        fun conn -> task {
            let! _ = (f conn).ConfigureAwait false
            ()
        }

    /// Ignore the result of an operation
    [<CustomOperation "ignoreResult">]
    member this.IgnoreResult (f : IConnection -> Task<'T>, conn) =
        this.IgnoreResult f conn

    /// Ignore the result of an operation
    [<CustomOperation "ignoreResult">]
    member this.IgnoreResult (f : IConnection -> Task<'T option>, conn) =
        this.IgnoreResult f conn

    // Reconnection

    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withRetry">]
    member _.WithRetry (f : IConnection -> Task<'T>, retries) =
        Retry.withRetry f retries

    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withRetry">]
    member _.WithRetry (f : IConnection -> Task<'T option>, retries) =
        Retry.withRetry f retries

    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withRetry">]
    member this.WithRetry (f : IConnection -> Task<'T>, retries, conn) =
        this.WithRetry (f, retries) conn
   
    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withRetry">]
    member this.WithRetry (f : IConnection -> Task<'T option>, retries, conn) =
        this.WithRetry (f, retries) conn
   
    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withRetryDefault">]
    member _.WithRetryDefault (f : IConnection -> Task<'T>) =
        Retry.withRetryDefault f

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withRetryDefault">]
    member _.WithRetryDefault (f : IConnection -> Task<'T option>) =
        Retry.withRetryDefault f

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withRetryDefault">]
    member this.WithRetryDefault (f : IConnection -> Task<'T>, conn) =
        this.WithRetryDefault f conn

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withRetryDefault">]
    member this.WithRetryDefault (f : IConnection -> Task<'T option>, conn) =
        this.WithRetryDefault f conn

    /// Retries once immediately
    [<CustomOperation "withRetryOnce">]
    member _.WithRetryOnce (f : IConnection -> Task<'T>) =
        Retry.withRetryOnce f

    /// Retries once immediately
    [<CustomOperation "withRetryOnce">]
    member _.WithRetryOnce (f : IConnection -> Task<'T option>) =
        Retry.withRetryOnce f

    /// Retries once immediately
    [<CustomOperation "withRetryOnce">]
    member this.WithRetryOnce (f : IConnection -> Task<'T>, conn) =
        this.WithRetryOnce f conn

    /// Retries once immediately
    [<CustomOperation "withRetryOnce">]
    member this.WithRetryOnce (f : IConnection -> Task<'T option>, conn) =
        this.WithRetryOnce f conn


/// RethinkDB computation expression
let rethink<'T> = RethinkBuilder<'T> ()
