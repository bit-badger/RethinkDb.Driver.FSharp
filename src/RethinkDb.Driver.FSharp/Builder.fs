[<AutoOpen>]
module RethinkDb.Driver.FSharp.RethinkBuilder

open System.Threading
open System.Threading.Tasks
open RethinkDb.Driver
open RethinkDb.Driver.Ast
open RethinkDb.Driver.FSharp
open RethinkDb.Driver.FSharp.Functions
open RethinkDb.Driver.Net

/// Computation Expression builder for RethinkDB queries
type RethinkBuilder<'T> () =
    
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
    member _.Db (_ : RethinkDB, dbName : string) =
        match dbName with "" -> invalidArg dbName "db name cannot be blank" | _ -> db dbName
    
    /// Identify a table (of form "dbName.tableName"; if no db name, uses default database)
    [<CustomOperation "withTable">]
    member this.Table (r : RethinkDB, tableName : string) =
        match dbAndTable tableName with
        | Some dbName, tblName -> this.Db (r, dbName) |> table tblName
        | None, _ -> fromTable tableName
    
    /// Create an equality join with another table
    [<CustomOperation "eqJoin">]
    member this.EqJoin (expr : ReqlExpr, field : string, otherTable : string) =
        eqJoin field (this.Table (RethinkDB.R, otherTable)) expr
    
    // meta queries (tables, indexes, etc.)
    
    /// List all databases
    [<CustomOperation "dbList">]
    member _.DbList (_ : RethinkDB) = dbList ()
    
    /// Create a database
    [<CustomOperation "dbCreate">]
    member _.DbCreate (_ : RethinkDB, dbName : string) = dbCreate dbName
    
    /// Drop a database
    [<CustomOperation "dbDrop">]
    member _.DbDrop (_ : RethinkDB, dbName : string) = dbDrop dbName
    
    /// List all tables for the default database
    [<CustomOperation "tableList">]
    member _.TableList (_ : RethinkDB) = tableListFromDefault ()
    
    /// List all tables for the specified database
    [<CustomOperation "tableList">]
    member this.TableList (r : RethinkDB, dbName : string) =
        match dbName with "" -> tableListFromDefault () | _ -> this.Db (r, dbName) |> tableList
    
    /// Create a table (of form "dbName.tableName"; if no db name, uses default database)
    [<CustomOperation "tableCreate">]
    member this.TableCreate (r : RethinkDB, tableName : string) =
        match dbAndTable tableName with
        | Some dbName, tblName -> this.Db (r, dbName) |> tableCreate tblName
        | None, _ -> tableCreateInDefault tableName
    
    /// Drop a table (of form "dbName.tableName"; if no db name, uses default database)
    [<CustomOperation "tableDrop">]
    member this.TableDrop (r : RethinkDB, tableName : string) =
        match dbAndTable tableName with
        | Some dbName, tblName -> this.Db (r, dbName) |> tableDrop tblName
        | None, _ -> tableDropFromDefault tableName
    
    /// List all indexes for a table
    [<CustomOperation "indexList">]
    member _.IndexList (tbl : Table) = indexList tbl
    
    /// Create an index for a table
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl : Table, index : string) = indexCreate index tbl
    
    /// Create an index for a table, specifying an optional argument
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl : Table, index : string, opts : IndexCreateOptArg list) =
        indexCreateWithOptArgs index opts tbl
    
    /// Create an index for a table, using a function to calculate the index
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl : Table, index : string, f : ReqlExpr -> obj) = indexCreateFunc index f tbl
    
    /// Create an index for a table, using a function to calculate the index
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl : Table, index : string, f : ReqlExpr -> obj, opts : IndexCreateOptArg list) =
        indexCreateFuncWithOptArgs index f opts tbl

    /// Create an index for a table, using a JavaScript function to calculate the index
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl : Table, index : string, js : string) = indexCreateJS index js tbl
    
    /// Create an index for a table, using a JavaScript function to calculate the index
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl : Table, index : string, js : string, opts : IndexCreateOptArg list) =
        indexCreateJSWithOptArgs index js opts tbl

    /// Drop an index for a table
    [<CustomOperation "indexDrop">]
    member _.IndexDrop (tbl : Table, index : string) = indexDrop index tbl
    
    /// Rename an index on a table
    [<CustomOperation "indexRename">]
    member _.IndexRename (tbl : Table, oldName : string, newName : string) = indexRename oldName newName tbl
    
    /// Rename an index on a table, specifying an overwrite option
    [<CustomOperation "indexRename">]
    member _.IndexRename (tbl : Table, oldName : string, newName : string, opt : IndexRenameOptArg) =
        indexRenameWithOptArg oldName newName opt tbl
    
    /// Get the status of all indexes on a table
    [<CustomOperation "indexStatus">]
    member _.IndexStatus (tbl : Table) = indexStatusAll tbl
    
    /// Get the status of specific indexes on a table
    [<CustomOperation "indexStatus">]
    member _.IndexStatus (tbl : Table, indexes : string list) = indexStatus indexes tbl
    
    /// Wait for all indexes on a table to become ready
    [<CustomOperation "indexWait">]
    member _.IndexWait (tbl : Table) = indexWaitAll tbl
    
    /// Wait for specific indexes on a table to become ready
    [<CustomOperation "indexWait">]
    member _.IndexWait (tbl : Table, indexes : string list) = indexWait indexes tbl
    
    // data retrieval / manipulation
    
    /// Get a document from a table by its ID
    [<CustomOperation "get">]
    member _.Get (tbl : Table, key : obj) = get key tbl
    
    /// Get all documents matching the given primary key value
    [<CustomOperation "getAll">]
    member _.GetAll (tbl : Table, keys : obj list) = getAll (Seq.ofList keys) tbl
    
    /// Get all documents matching the given index value
    [<CustomOperation "getAll">]
    member _.GetAll (tbl : Table, keys : obj list, index : string) = getAllWithIndex (Seq.ofList keys) index tbl
    
    /// Skip a certain number of results
    [<CustomOperation "skip">]
    member _.Skip (expr : ReqlExpr, toSkip : int) = skip toSkip expr

    /// Limit the results of this query
    [<CustomOperation "limit">]
    member _.Limit (expr : ReqlExpr, toLimit : int) = limit toLimit expr

    /// Count documents for the current query
    [<CustomOperation "count">]
    member _.Count (expr : ReqlExpr) = count expr
    
    /// Count documents for the current query
    [<CustomOperation "count">]
    member _.Count (expr : ReqlExpr, f : ReqlExpr -> bool) = countFunc f expr
    
    /// Count documents for the current query
    [<CustomOperation "count">]
    member _.Count (expr : ReqlExpr, js : string) = countJS js expr
    
    /// Select distinct values from the sequence
    [<CustomOperation "distinct">]
    member _.Distinct (expr : ReqlExpr) = distinct expr
    
    /// Select distinct values from the sequence using an index
    [<CustomOperation "distinct">]
    member _.Distinct (expr : ReqlExpr, index : string) = distinctWithIndex index expr
    
    /// Filter a query by a single field value
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, field : string, value : obj) = filter (fieldsToMap [ field, value ]) expr
    
    /// Filter a query by a single field value, including an optional argument
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, field : string, value : obj, opt : FilterOptArg) =
        filterWithOptArg (fieldsToMap [ field, value ]) opt expr
    
    /// Filter a query by multiple field values
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, filters : (string * obj) list) = filter (fieldsToMap filters) expr
    
    /// Filter a query by multiple field values, including an optional argument
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, filters : (string * obj) list, opt : FilterOptArg) =
        filterWithOptArg (fieldsToMap filters) opt expr
    
    /// Filter a query by a function
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, f : ReqlExpr -> obj) = filterFunc f expr
    
    /// Filter a query by a function, including an optional argument
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, f : ReqlExpr -> obj, opt : FilterOptArg) = filterFuncWithOptArg f opt expr
    
    /// Filter a query by multiple functions (has the effect of ANDing them)
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, fs : (ReqlExpr -> obj) list) = filterFuncAll fs expr
    
    /// Filter a query by multiple functions (has the effect of ANDing them), including an optional argument
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, fs : (ReqlExpr -> obj) list, opt : FilterOptArg) =
        filterFuncAllWithOptArg fs opt expr
    
    /// Filter a query using a JavaScript expression
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, js : string) = filterJS js expr
    
    /// Filter a query using a JavaScript expression, including an optional argument
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, js : string, opt : FilterOptArg) = filterJSWithOptArg js opt expr
    
    /// Filter a query by a range of values
    [<CustomOperation "between">]
    member _.Between (expr : ReqlExpr, lower : obj, upper : obj) = between lower upper expr
    
    /// Filter a query by a range of values, using optional arguments
    [<CustomOperation "between">]
    member _.Between (expr : ReqlExpr, lower : obj, upper : obj, opts : BetweenOptArg list) =
        betweenWithOptArgs lower upper opts expr
        
    /// Map fields for the current query using a function
    [<CustomOperation "map">]
    member _.Map (expr : ReqlExpr, f : ReqlExpr -> obj) = mapFunc f expr

    /// Map fields for the current query using a JavaScript function
    [<CustomOperation "map">]
    member _.Map (expr : ReqlExpr, js : string) = mapJS js expr

    /// Exclude the given fields from the output
    [<CustomOperation "without">]
    member _.Without (expr : ReqlExpr, fields : string list) = without (Seq.ofList fields) expr
    
    /// Combine a left and right selection into a single record
    [<CustomOperation "zip">]
    member _.Zip (expr : ReqlExpr) = zip expr
    
    /// Merge a document into the current query
    [<CustomOperation "merge">]
    member _.Merge (expr : ReqlExpr, doc : obj) = merge doc expr
    
    /// Merge a document into the current query, constructed by a function
    [<CustomOperation "merge">]
    member _.Merge (expr : ReqlExpr, f : ReqlExpr -> obj) = mergeFunc f expr
    
    /// Merge a document into the current query, constructed by a JavaScript function
    [<CustomOperation "merge">]
    member _.Merge (expr : ReqlExpr, js : string) = mergeJS js expr
    
    /// Pluck (select only) specific fields from the query
    [<CustomOperation "pluck">]
    member _.Pluck (expr : ReqlExpr, fields : string list) = pluck (Seq.ofList fields) expr
    
    /// Order the results by the given field name (ascending)
    [<CustomOperation "orderBy">]
    member _.OrderBy (expr : ReqlExpr, field : string) = orderBy field expr

    /// Order the results by the given field name (descending)
    [<CustomOperation "orderByDescending">]
    member _.OrderByDescending (expr : ReqlExpr, field : string) = orderByDescending field expr

    /// Order the results by the given index name (ascending)
    [<CustomOperation "orderByIndex">]
    member _.OrderByIndex (expr : ReqlExpr, index : string) = orderByIndex index expr

    /// Order the results by the given index name (descending)
    [<CustomOperation "orderByIndexDescending">]
    member _.OrderByIndexDescending (expr : ReqlExpr, index : string) = orderByIndexDescending index expr

    /// Order the results by the given function value (ascending)
    [<CustomOperation "orderByFunc">]
    member _.OrderByFunc (expr : ReqlExpr, f : ReqlExpr -> obj) = orderByFunc f expr

    /// Order the results by the given function value (descending)
    [<CustomOperation "orderByFuncDescending">]
    member _.OrderByFuncDescending (expr : ReqlExpr, f : ReqlExpr -> obj) = orderByFuncDescending f expr

    /// Order the results by the given JavaScript function value (ascending)
    [<CustomOperation "orderByJS">]
    member _.OrderByJS (expr : ReqlExpr, js : string) = orderByJS js expr

    /// Order the results by the given JavaScript function value (descending)
    [<CustomOperation "orderByJSDescending">]
    member _.OrderByJSDescending (expr : ReqlExpr, js : string) = orderByJSDescending js expr

    /// Insert a document into the given table
    [<CustomOperation "insert">]
    member _.Insert (tbl : Table, doc : obj) = insert doc tbl
    
    /// Insert multiple documents into the given table
    [<CustomOperation "insert">]
    member _.Insert (tbl : Table, doc : obj list) = insertMany (Seq.ofList doc) tbl
    
    /// Insert a document into the given table, using optional arguments
    [<CustomOperation "insert">]
    member _.Insert (tbl : Table, doc : obj, opts : InsertOptArg list) = insertWithOptArgs doc opts tbl
    
    /// Insert multiple documents into the given table, using optional arguments
    [<CustomOperation "insert">]
    member _.Insert (tbl : Table, docs : obj list, opts : InsertOptArg list) =
        insertManyWithOptArgs (Seq.ofList docs) opts tbl
    
    /// Update fields from the given object
    [<CustomOperation "update">]
    member _.Update (expr : ReqlExpr, object : obj) = update object expr
    
    /// Update fields from the given object, using optional arguments
    [<CustomOperation "update">]
    member _.Update (expr : ReqlExpr, object : obj, args : UpdateOptArg list) = updateWithOptArgs object args expr
    
    /// Update specific fields
    [<CustomOperation "update">]
    member _.Update (expr : ReqlExpr, fields : (string * obj) list) = update (fieldsToMap fields) expr
    
    /// Update specific fields, using optional arguments
    [<CustomOperation "update">]
    member _.Update (expr : ReqlExpr, fields : (string * obj) list, args : UpdateOptArg list) =
        updateWithOptArgs (fieldsToMap fields) args expr
    
    /// Update specific fields using a function
    [<CustomOperation "update">]
    member _.Update (expr : ReqlExpr, f : ReqlExpr -> obj) = updateFunc f expr
    
    /// Update specific fields using a function, with optional arguments
    [<CustomOperation "update">]
    member _.Update (expr : ReqlExpr, f : ReqlExpr -> obj, args : UpdateOptArg list) = updateFuncWithOptArgs f args expr
    
    /// Update specific fields using a JavaScript function
    [<CustomOperation "update">]
    member _.Update (expr : ReqlExpr, js : string) = updateJS js expr
    
    /// Update specific fields using a JavaScript function, with optional arguments
    [<CustomOperation "update">]
    member _.Update (expr : ReqlExpr, js : string, args : UpdateOptArg list) = updateJSWithOptArgs js args expr
    
    /// Replace the current query with the specified document
    [<CustomOperation "replace">]
    member _.Replace (expr : ReqlExpr, doc : obj) = replace doc expr
    
    /// Replace the current query with the specified document, using optional arguments
    [<CustomOperation "replace">]
    member _.Replace (expr : ReqlExpr, doc : obj, args : ReplaceOptArg list) = replaceWithOptArgs doc args expr
    
    /// Replace the current query with document(s) created by a function
    [<CustomOperation "replace">]
    member _.Replace (expr : ReqlExpr, f : ReqlExpr -> obj) = replaceFunc f expr
    
    /// Replace the current query with document(s) created by a function, using optional arguments
    [<CustomOperation "replace">]
    member _.Replace (expr : ReqlExpr, f : ReqlExpr -> obj, args : ReplaceOptArg list) =
        replaceFuncWithOptArgs f args expr
    
    /// Replace the current query with document(s) created by a JavaScript function
    [<CustomOperation "replace">]
    member _.Replace (expr : ReqlExpr, js : string) = replaceJS js expr
    
    /// Replace the current query with document(s) created by a JavaScript function, using optional arguments
    [<CustomOperation "replace">]
    member _.Replace (expr : ReqlExpr, js : string, args : ReplaceOptArg list) = replaceJSWithOptArgs js args expr
    
    /// Delete the document(s) identified by the current query
    [<CustomOperation "delete">]
    member _.Delete (expr : ReqlExpr) = delete expr

    /// Delete the document(s) identified by the current query
    [<CustomOperation "delete">]
    member _.Delete (expr : ReqlExpr, opts : DeleteOptArg list) = deleteWithOptArgs opts expr

    /// Wait for updates to a table to be synchronized to disk
    [<CustomOperation "sync">]
    member _.Sync (tbl : Table) = sync tbl

    // executing queries
    
    /// Execute the query, returning the result of the type specified using the given cancellation token
    [<CustomOperation "result">]
    member _.Result (expr : ReqlExpr, cancelToken : CancellationToken) = runResultWithCancel<'T> cancelToken expr
    
    /// Execute the query, returning the result of the type specified using the given cancellation token
    [<CustomOperation "result">]
    member this.Result (expr : ReqlExpr, cancelToken : CancellationToken, conn : IConnection) =
        this.Result (expr, cancelToken) conn
    
    /// Execute the query, returning the result of the type specified
    [<CustomOperation "result">]
    member _.Result (expr : ReqlExpr) = runResult<'T> expr
    
    /// Execute the query, returning the result of the type specified
    [<CustomOperation "result">]
    member this.Result (expr : ReqlExpr, conn : IConnection) = this.Result expr conn
    
    /// Execute the query, returning the result of the type specified, using optional arguments
    [<CustomOperation "result">]
    member _.Result (expr : ReqlExpr, args : RunOptArg list) = runResultWithOptArgs<'T> args expr 
    
    /// Execute the query, returning the result of the type specified
    [<CustomOperation "result">]
    member this.Result (expr : ReqlExpr, args : RunOptArg list, conn : IConnection) = this.Result (expr, args) conn
    
    /// Execute the query, returning the result of the type specified, using optional arguments and a cancellation token
    [<CustomOperation "result">]
    member _.Result (expr : ReqlExpr, args : RunOptArg list, cancelToken : CancellationToken) =
        runResultWithOptArgsAndCancel<'T> args cancelToken expr 
    
    /// Execute the query, returning the result of the type specified, using optional arguments and a cancellation token
    [<CustomOperation "result">]
    member this.Result (expr : ReqlExpr, args : RunOptArg list, cancelToken : CancellationToken, conn : IConnection) =
        this.Result (expr, args, cancelToken) conn 
    
    /// Execute the query, returning the result of the type specified, or None if no result is found
    [<CustomOperation "resultOption">]
    member this.ResultOption (expr : ReqlExpr) = this.Result expr |> asOption
    
    /// Execute the query, returning the result of the type specified, or None if no result is found
    [<CustomOperation "resultOption">]
    member this.ResultOption (expr : ReqlExpr, conn : IConnection) = this.ResultOption expr conn
    
    /// Execute the query with a cancellation token, returning the result of the type specified, or None if no result is
    /// found
    [<CustomOperation "resultOption">]
    member this.ResultOption (expr : ReqlExpr, cancelToken : CancellationToken) =
        this.Result (expr, cancelToken) |> asOption
    
    /// Execute the query with a cancellation token, returning the result of the type specified, or None if no result is
    /// found
    [<CustomOperation "resultOption">]
    member this.ResultOption (expr : ReqlExpr, cancelToken : CancellationToken, conn : IConnection) =
        this.ResultOption (expr, cancelToken) conn
    
    /// Execute the query with optional arguments, returning the result of the type specified, or None if no result is
    /// found
    [<CustomOperation "resultOption">]
    member this.ResultOption (expr : ReqlExpr, opts : RunOptArg list) = this.Result (expr, opts) |> asOption
    
    /// Execute the query with optional arguments, returning the result of the type specified, or None if no result is
    /// found
    [<CustomOperation "resultOption">]
    member this.ResultOption (expr : ReqlExpr, opts : RunOptArg list, conn : IConnection) =
        this.ResultOption (expr, opts) conn
    
    /// Execute the query with optional arguments and a cancellation token, returning the result of the type specified,
    /// or None if no result is found
    [<CustomOperation "resultOption">]
    member this.ResultOption (expr : ReqlExpr, opts : RunOptArg list, cancelToken : CancellationToken) =
        this.Result (expr, opts, cancelToken) |> asOption
    
    /// Execute the query with optional arguments and a cancellation token, returning the result of the type specified,
    /// or None if no result is found
    [<CustomOperation "resultOption">]
    member this.ResultOption (expr : ReqlExpr, opts : RunOptArg list, cancelToken : CancellationToken,
                              conn : IConnection) =
        this.ResultOption (expr, opts, cancelToken) conn
    
    /// Execute the query, returning the result of the type specified using the given cancellation token
    [<CustomOperation "asyncResult">]
    member _.AsyncResult (expr : ReqlExpr, cancelToken : CancellationToken) = asyncResultWithCancel<'T> cancelToken expr
    
    /// Execute the query, returning the result of the type specified using the given cancellation token
    [<CustomOperation "asyncResult">]
    member this.AsyncResult (expr : ReqlExpr, cancelToken : CancellationToken, conn : IConnection) =
        this.AsyncResult (expr, cancelToken) conn
    
    /// Execute the query, returning the result of the type specified
    [<CustomOperation "asyncResult">]
    member _.AsyncResult (expr : ReqlExpr) = asyncResult<'T> expr
    
    /// Execute the query, returning the result of the type specified
    [<CustomOperation "asyncResult">]
    member this.AsyncResult (expr : ReqlExpr, conn : IConnection) = this.AsyncResult expr conn
    
    /// Execute the query, returning the result of the type specified, using optional arguments
    [<CustomOperation "asyncResult">]
    member _.AsyncResult (expr : ReqlExpr, args : RunOptArg list) = asyncResultWithOptArgs<'T> args expr 
    
    /// Execute the query, returning the result of the type specified
    [<CustomOperation "asyncResult">]
    member this.AsyncResult (expr : ReqlExpr, args : RunOptArg list, conn : IConnection) =
        this.AsyncResult (expr, args) conn
    
    /// Execute the query, returning the result of the type specified, using optional arguments and a cancellation token
    [<CustomOperation "asyncResult">]
    member _.AsyncResult (expr : ReqlExpr, args : RunOptArg list, cancelToken : CancellationToken) =
        asyncResultWithOptArgsAndCancel<'T> args cancelToken expr 
    
    /// Execute the query, returning the result of the type specified, using optional arguments and a cancellation token
    [<CustomOperation "asyncResult">]
    member this.AsyncResult (expr : ReqlExpr, args : RunOptArg list, cancelToken : CancellationToken,
                             conn : IConnection) =
        this.AsyncResult (expr, args, cancelToken) conn 
    
    /// Execute the query, returning the result of the type specified, or None if no result is found
    [<CustomOperation "asyncOption">]
    member this.AsyncOption (expr : ReqlExpr) =  this.AsyncResult expr |> asAsyncOption
    
    /// Execute the query, returning the result of the type specified, or None if no result is found
    [<CustomOperation "asyncOption">]
    member this.AsyncOption (expr : ReqlExpr, conn : IConnection) = this.AsyncOption expr conn
    
    /// Execute the query with a cancellation token, returning the result of the type specified, or None if no result is
    /// found
    [<CustomOperation "asyncOption">]
    member this.AsyncOption (expr : ReqlExpr, cancelToken : CancellationToken) =
        this.AsyncResult (expr, cancelToken) |> asAsyncOption
    
    /// Execute the query with a cancellation token, returning the result of the type specified, or None if no result is
    /// found
    [<CustomOperation "asyncOption">]
    member this.AsyncOption (expr : ReqlExpr, cancelToken : CancellationToken, conn : IConnection) =
        this.AsyncOption (expr, cancelToken) conn
    
    /// Execute the query with optional arguments, returning the result of the type specified, or None if no result is
    /// found
    [<CustomOperation "asyncOption">]
    member this.AsyncOption (expr : ReqlExpr, opts : RunOptArg list) = this.AsyncResult (expr, opts) |> asAsyncOption
    
    /// Execute the query with optional arguments, returning the result of the type specified, or None if no result is
    /// found
    [<CustomOperation "asyncOption">]
    member this.AsyncOption (expr : ReqlExpr, opts : RunOptArg list, conn : IConnection) =
        this.AsyncOption (expr, opts) conn
    
    /// Execute the query with optional arguments and a cancellation token, returning the result of the type specified,
    /// or None if no result is found
    [<CustomOperation "asyncOption">]
    member this.AsyncOption (expr : ReqlExpr, opts : RunOptArg list, cancelToken : CancellationToken) =
        this.AsyncResult (expr, opts, cancelToken) |> asAsyncOption
    
    /// Execute the query with optional arguments and a cancellation token, returning the result of the type specified,
    /// or None if no result is found
    [<CustomOperation "asyncOption">]
    member this.AsyncOption (expr : ReqlExpr, opts : RunOptArg list, cancelToken : CancellationToken,
                             conn : IConnection) =
        this.AsyncOption (expr, opts, cancelToken) conn
    
    /// Execute the query synchronously, returning the result of the type specified
    [<CustomOperation "syncResult">]
    member _.SyncResult (expr : ReqlExpr) = syncResult<'T> expr
    
    /// Execute the query synchronously, returning the result of the type specified
    [<CustomOperation "syncResult">]
    member this.SyncResult (expr : ReqlExpr, conn : IConnection) = this.SyncResult expr conn
    
    /// Execute the query synchronously, returning the result of the type specified, using optional arguments
    [<CustomOperation "syncResult">]
    member _.SyncResult (expr : ReqlExpr, args : RunOptArg list) = syncResultWithOptArgs<'T> args expr 
    
    /// Execute the query synchronously, returning the result of the type specified
    [<CustomOperation "syncResult">]
    member this.SyncResult (expr : ReqlExpr, args : RunOptArg list, conn : IConnection) =
        this.SyncResult (expr, args) conn
    
    /// Execute the query synchronously, returning the result of the type specified, or None if no result is found
    [<CustomOperation "syncOption">]
    member this.SyncOption (expr : ReqlExpr) = this.SyncResult expr |> asSyncOption
    
    /// Execute the query synchronously, returning the result of the type specified, or None if no result is found
    [<CustomOperation "syncOption">]
    member this.SyncOption (expr : ReqlExpr, conn : IConnection) = this.SyncOption expr conn
    
    /// Execute the query synchronously with optional arguments, returning the result of the type specified, or None if
    /// no result is found
    [<CustomOperation "syncOption">]
    member this.SyncOption (expr : ReqlExpr, opts : RunOptArg list) = this.SyncResult (expr, opts) |> asSyncOption
    
    /// Execute the query synchronously with optional arguments, returning the result of the type specified, or None if
    /// no result is found
    [<CustomOperation "syncOption">]
    member this.SyncOption (expr : ReqlExpr, opts : RunOptArg list, conn : IConnection) =
        this.SyncOption (expr, opts) conn
    
    /// Perform a write operation
    [<CustomOperation "write">]
    member _.Write (expr : ReqlExpr) = runWrite expr

    /// Perform a write operation
    [<CustomOperation "write">]
    member this.Write (expr : ReqlExpr, conn : IConnection) = this.Write expr conn
        
    /// Perform a write operation using optional arguments
    [<CustomOperation "write">]
    member _.Write (expr : ReqlExpr, args : RunOptArg list) = runWriteWithOptArgs args expr

    /// Perform a write operation using optional arguments
    [<CustomOperation "write">]
    member this.Write (expr : ReqlExpr, args : RunOptArg list, conn : IConnection) = this.Write (expr, args) conn
        
    /// Perform a write operation using a cancellation token
    [<CustomOperation "write">]
    member _.Write (expr : ReqlExpr, cancelToken : CancellationToken) = runWriteWithCancel cancelToken expr

    /// Perform a write operation using a cancellation token
    [<CustomOperation "write">]
    member this.Write (expr : ReqlExpr, cancelToken : CancellationToken, conn : IConnection) =
        this.Write (expr, cancelToken) conn
        
    /// Perform a write operation using optional arguments and a cancellation token
    [<CustomOperation "write">]
    member _.Write (expr : ReqlExpr, args : RunOptArg list, cancelToken : CancellationToken) =
        runWriteWithOptArgsAndCancel args cancelToken expr

    /// Perform a write operation using optional arguments and a cancellation token
    [<CustomOperation "write">]
    member this.Write (expr : ReqlExpr, args : RunOptArg list, cancelToken : CancellationToken, conn : IConnection) =
        this.Write (expr, args, cancelToken) conn
        
    /// Perform a write operation
    [<CustomOperation "asyncWrite">]
    member _.AsyncWrite (expr : ReqlExpr) = asyncWrite expr

    /// Perform a write operation
    [<CustomOperation "asyncWrite">]
    member this.AsyncWrite (expr : ReqlExpr, conn : IConnection) = this.AsyncWrite expr conn
        
    /// Perform a write operation using optional arguments
    [<CustomOperation "asyncWrite">]
    member _.AsyncWrite (expr : ReqlExpr, args : RunOptArg list) = asyncWriteWithOptArgs args expr

    /// Perform a write operation using optional arguments
    [<CustomOperation "asyncWrite">]
    member this.AsyncWrite (expr : ReqlExpr, args : RunOptArg list, conn : IConnection) =
        this.AsyncWrite (expr, args) conn
        
    /// Perform a write operation using a cancellation token
    [<CustomOperation "asyncWrite">]
    member _.AsyncWrite (expr : ReqlExpr, cancelToken : CancellationToken) = asyncWriteWithCancel cancelToken expr

    /// Perform a write operation using a cancellation token
    [<CustomOperation "asyncWrite">]
    member this.AsyncWrite (expr : ReqlExpr, cancelToken : CancellationToken, conn : IConnection) =
        this.AsyncWrite (expr, cancelToken) conn
        
    /// Perform a write operation using optional arguments and a cancellation token
    [<CustomOperation "asyncWrite">]
    member _.AsyncWrite (expr : ReqlExpr, args : RunOptArg list, cancelToken : CancellationToken) =
        asyncWriteWithOptArgsAndCancel args cancelToken expr

    /// Perform a write operation using optional arguments and a cancellation token
    [<CustomOperation "asyncWrite">]
    member this.AsyncWrite (expr : ReqlExpr, args : RunOptArg list, cancelToken : CancellationToken,
                            conn : IConnection) =
        this.AsyncWrite (expr, args, cancelToken) conn
        
    /// Perform a synchronous write operation
    [<CustomOperation "syncWrite">]
    member _.SyncWrite (expr : ReqlExpr) = syncWrite expr

    /// Perform a synchronous write operation
    [<CustomOperation "syncWrite">]
    member this.SyncWrite (expr : ReqlExpr, conn : IConnection) = this.SyncWrite expr conn
        
    /// Perform a write operation using optional arguments
    [<CustomOperation "syncWrite">]
    member _.SyncWrite (expr : ReqlExpr, args : RunOptArg list) = syncWriteWithOptArgs args expr

    /// Perform a write operation using optional arguments
    [<CustomOperation "syncWrite">]
    member this.SyncWrite (expr : ReqlExpr, args : RunOptArg list, conn : IConnection) =
        this.SyncWrite (expr, args) conn
        
    /// Perform a write operation, returning a result even if there are errors
    [<CustomOperation "writeResult">]
    member _.WriteResult (expr : ReqlExpr) = runWriteResult expr

    /// Perform a write operation, returning a result even if there are errors
    [<CustomOperation "writeResult">]
    member this.WriteResult (expr : ReqlExpr, conn : IConnection) = this.WriteResult expr conn
        
    /// Perform a write operation with optional arguments, returning a result even if there are errors
    [<CustomOperation "writeResult">]
    member _.WriteResult (expr : ReqlExpr, args : RunOptArg list) = runWriteResultWithOptArgs args expr

    /// Perform a write operation with optional arguments, returning a result even if there are errors
    [<CustomOperation "writeResult">]
    member this.WriteResult (expr : ReqlExpr, args : RunOptArg list, conn : IConnection) =
        this.WriteResult (expr, args) conn
        
    /// Perform a write operation with a cancellation token, returning a result even if there are errors
    [<CustomOperation "writeResult">]
    member _.WriteResult (expr : ReqlExpr, cancelToken : CancellationToken) = runWriteResultWithCancel cancelToken expr

    /// Perform a write operation with a cancellation token, returning a result even if there are errors
    [<CustomOperation "writeResult">]
    member this.WriteResult (expr : ReqlExpr, cancelToken : CancellationToken, conn : IConnection) =
        this.WriteResult (expr, cancelToken) conn
        
    /// Perform a write operation with optional arguments and a cancellation token, returning a result even if there are
    /// errors
    [<CustomOperation "writeResult">]
    member _.WriteResult (expr : ReqlExpr, args : RunOptArg list, cancelToken : CancellationToken) =
        runWriteResultWithOptArgsAndCancel args cancelToken expr

    /// Perform a write operation with optional arguments and a cancellation token, returning a result even if there are
    /// errors
    [<CustomOperation "writeResult">]
    member this.WriteResult (expr : ReqlExpr, args : RunOptArg list, cancelToken : CancellationToken,
                             conn : IConnection) =
        this.WriteResult (expr, args, cancelToken) conn
        
    /// Perform a write operation, returning a result even if there are errors
    [<CustomOperation "asyncWriteResult">]
    member _.AsyncWriteResult (expr : ReqlExpr) = asyncWriteResult expr

    /// Perform a write operation, returning a result even if there are errors
    [<CustomOperation "asyncWriteResult">]
    member this.AsyncWriteResult (expr : ReqlExpr, conn : IConnection) = this.AsyncWriteResult expr conn
        
    /// Perform a write operation with optional arguments, returning a result even if there are errors
    [<CustomOperation "asyncWriteResult">]
    member _.AsyncWriteResult (expr : ReqlExpr, args : RunOptArg list) = asyncWriteResultWithOptArgs args expr

    /// Perform a write operation with optional arguments, returning a result even if there are errors
    [<CustomOperation "asyncWriteResult">]
    member this.AsyncWriteResult (expr : ReqlExpr, args : RunOptArg list, conn : IConnection) =
        this.AsyncWriteResult (expr, args) conn
        
    /// Perform a write operation with a cancellation token, returning a result even if there are errors
    [<CustomOperation "asyncWriteResult">]
    member _.AsyncWriteResult (expr : ReqlExpr, cancelToken : CancellationToken) =
        asyncWriteResultWithCancel cancelToken expr

    /// Perform a write operation with a cancellation token, returning a result even if there are errors
    [<CustomOperation "asyncWriteResult">]
    member this.AsyncWriteResult (expr : ReqlExpr, cancelToken : CancellationToken, conn : IConnection) =
        this.AsyncWriteResult (expr, cancelToken) conn
        
    /// Perform a write operation with optional arguments and a cancellation token, returning a result even if there are
    /// errors
    [<CustomOperation "asyncWriteResult">]
    member _.AsyncWriteResult (expr : ReqlExpr, args : RunOptArg list, cancelToken : CancellationToken) =
        asyncWriteResultWithOptArgsAndCancel args cancelToken expr

    /// Perform a write operation with optional arguments and a cancellation token, returning a result even if there are
    /// errors
    [<CustomOperation "asyncWriteResult">]
    member this.AsyncWriteResult (expr : ReqlExpr, args : RunOptArg list, cancelToken : CancellationToken,
                                  conn : IConnection) =
        this.AsyncWriteResult (expr, args, cancelToken) conn
        
    /// Perform a synchronous write operation, returning a result even if there are errors
    [<CustomOperation "syncWriteResult">]
    member _.SyncWriteResult (expr : ReqlExpr) = syncWriteResult expr

    /// Perform a synchronous write operation, returning a result even if there are errors
    [<CustomOperation "syncWriteResult">]
    member this.SyncWriteResult (expr : ReqlExpr, conn : IConnection) = this.SyncWriteResult expr conn
        
    /// Perform a synchronous write operation with optional arguments, returning a result even if there are errors
    [<CustomOperation "syncWriteResult">]
    member _.SyncWriteResult (expr : ReqlExpr, args : RunOptArg list) = syncWriteResultWithOptArgs args expr

    /// Perform a synchronous write operation with optional arguments, returning a result even if there are errors
    [<CustomOperation "syncWriteResult">]
    member this.SyncWriteResult (expr : ReqlExpr, args : RunOptArg list, conn : IConnection) =
        this.SyncWriteResult (expr, args) conn
        
    /// Ignore the result of an operation
    [<CustomOperation "ignoreResult">]
    member _.IgnoreResult (f : IConnection -> Task<'T>) = ignoreResult<'T> f

    /// Ignore the result of an operation
    [<CustomOperation "ignoreResult">]
    member this.IgnoreResult (f : IConnection -> Task<'T>, conn) = this.IgnoreResult f conn

    /// Ignore the result of an operation
    [<CustomOperation "ignoreAsync">]
    member _.IgnoreAsync (f : IConnection -> Async<'T>) = fun conn -> f conn |> Async.Ignore

    /// Ignore the result of an operation
    [<CustomOperation "ignoreAsync">]
    member this.IgnoreAsync (f : IConnection -> Async<'T>, conn) = this.IgnoreAsync f conn

    /// Ignore the result of a synchronous operation
    [<CustomOperation "ignoreSync">]
    member _.IgnoreSync (f : IConnection -> 'T) = fun conn -> f conn |> ignore

    /// Ignore the result of a synchronous operation
    [<CustomOperation "ignoreSync">]
    member this.IgnoreSync (f : IConnection -> 'T, conn) = this.IgnoreSync f conn

    // Reconnection

    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withRetry">]
    member _.WithRetry (f : IConnection -> Task<'T>, retries : float seq) = withRetry<'T> retries f

    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withRetry">]
    member this.WithRetry (f : IConnection -> Task<'T>, retries : float seq, conn : IConnection) =
        this.WithRetry (f, retries) conn
   
    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withRetryOption">]
    member _.WithRetryOption (f : IConnection -> Task<'T option>, retries : float seq) = withRetry<'T option> retries f

    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withRetryOption">]
    member this.WithRetryOption (f : IConnection -> Task<'T option>, retries : float seq, conn : IConnection) =
        this.WithRetryOption (f, retries) conn
   
    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withAsyncRetry">]
    member _.WithAsyncRetry (f : IConnection -> Async<'T>, retries : float seq) = withAsyncRetry<'T> retries f

    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withAsyncRetry">]
    member this.WithAsyncRetry (f : IConnection -> Async<'T>, retries : float seq, conn : IConnection) =
        this.WithAsyncRetry (f, retries) conn
   
    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withAsyncRetryOption">]
    member this.WithAsyncRetryOption (f : IConnection -> Async<'T option>, retries : float seq) =
        withAsyncRetry<'T option> retries f

    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withAsyncRetryOption">]
    member this.WithAsyncRetryOption (f : IConnection -> Async<'T option>, retries : float seq, conn : IConnection) =
        this.WithAsyncRetryOption (f, retries) conn
   
    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withSyncRetry">]
    member _.WithSyncRetry (f : IConnection -> 'T, retries : float seq) = withSyncRetry<'T> retries f

    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withSyncRetry">]
    member this.WithSyncRetry (f : IConnection -> 'T, retries : float seq, conn : IConnection) =
        this.WithSyncRetry (f, retries) conn
   
    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withSyncRetryOption">]
    member _.WithSyncRetryOption (f : IConnection -> 'T option, retries : float seq) =
        withSyncRetry<'T option> retries f

    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withSyncRetryOption">]
    member this.WithSyncRetryOption (f : IConnection -> 'T option, retries : float seq, conn : IConnection) =
        this.WithSyncRetryOption (f, retries) conn
   
    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withRetryDefault">]
    member _.WithRetryDefault (f : IConnection -> Task<'T>) = withRetryDefault<'T> f

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withRetryDefault">]
    member this.WithRetryDefault (f : IConnection -> Task<'T>, conn : IConnection) = this.WithRetryDefault f conn

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withRetryOptionDefault">]
    member _.WithRetryOptionDefault (f : IConnection -> Task<'T option>) = withRetryDefault<'T option> f

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withRetryOptionDefault">]
    member this.WithRetryOptionDefault (f : IConnection -> Task<'T option>, conn : IConnection) =
        this.WithRetryOptionDefault f conn

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withAsyncRetryDefault">]
    member _.WithAsyncRetryDefault (f : IConnection -> Async<'T>) = withAsyncRetryDefault<'T> f

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withAsyncRetryDefault">]
    member this.WithAsyncRetryDefault (f : IConnection -> Async<'T>, conn : IConnection) =
        this.WithAsyncRetryDefault f conn

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withAsyncRetryOptionDefault">]
    member _.WithAsyncRetryOptionDefault (f : IConnection -> Async<'T option>) = withAsyncRetryDefault<'T option> f

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withAsyncRetryOptionDefault">]
    member this.WithAsyncRetryOptionDefault (f : IConnection -> Async<'T option>, conn : IConnection) =
        this.WithAsyncRetryOptionDefault f conn

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withSyncRetryDefault">]
    member _.WithSyncRetryDefault (f : IConnection -> 'T) = withSyncRetryDefault<'T> f

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withSyncRetryDefault">]
    member this.WithSyncRetryDefault (f : IConnection -> 'T, conn : IConnection) = this.WithSyncRetryDefault f conn

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withSyncRetryOptionDefault">]
    member _.WithSyncRetryOptionDefault (f : IConnection -> 'T option) = withSyncRetryDefault<'T option> f

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withSyncRetryOptionDefault">]
    member this.WithSyncRetryOptionDefault (f : IConnection -> 'T option, conn : IConnection) =
        this.WithSyncRetryOptionDefault f conn

    /// Retries once immediately
    [<CustomOperation "withRetryOnce">]
    member _.WithRetryOnce (f : IConnection -> Task<'T>) = withRetryOnce<'T> f

    /// Retries once immediately
    [<CustomOperation "withRetryOnce">]
    member this.WithRetryOnce (f : IConnection -> Task<'T>, conn : IConnection) = this.WithRetryOnce f conn

    /// Retries once immediately
    [<CustomOperation "withRetryOptionOnce">]
    member _.WithRetryOptionOnce (f : IConnection -> Task<'T option>) = withRetryOnce<'T option> f

    /// Retries once immediately
    [<CustomOperation "withRetryOptionOnce">]
    member this.WithRetryOptionOnce (f : IConnection -> Task<'T option>, conn : IConnection) =
        this.WithRetryOptionOnce f conn

    /// Retries once immediately
    [<CustomOperation "withAsyncRetryOnce">]
    member _.WithAsyncRetryOnce (f : IConnection -> Async<'T>) = withAsyncRetryOnce<'T> f

    /// Retries once immediately
    [<CustomOperation "withAsyncRetryOnce">]
    member this.WithAsyncRetryOnce (f : IConnection -> Async<'T>, conn : IConnection) = this.WithAsyncRetryOnce f conn

    /// Retries once immediately
    [<CustomOperation "withAsyncRetryOptionOnce">]
    member _.WithAsyncRetryOptionOnce (f : IConnection -> Async<'T option>) = withAsyncRetryOnce<'T option> f

    /// Retries once immediately
    [<CustomOperation "withAsyncRetryOptionOnce">]
    member this.WithAsyncRetryOptionOnce (f : IConnection -> Async<'T option>, conn : IConnection) =
        this.WithAsyncRetryOptionOnce f conn

    /// Retries once immediately
    [<CustomOperation "withSyncRetryOnce">]
    member _.WithSyncRetryOnce (f : IConnection -> 'T) = withSyncRetryOnce<'T> f

    /// Retries once immediately
    [<CustomOperation "withSyncRetryOnce">]
    member this.WithSyncRetryOnce (f : IConnection -> 'T, conn : IConnection) = this.WithSyncRetryOnce f conn

    /// Retries once immediately
    [<CustomOperation "withSyncRetryOptionOnce">]
    member _.WithSyncRetryOptionOnce (f : IConnection -> 'T option) = withSyncRetryOnce<'T option> f

    /// Retries once immediately
    [<CustomOperation "withSyncRetryOptionOnce">]
    member this.WithSyncRetryOptionOnce (f : IConnection -> 'T option, conn : IConnection) =
        this.WithSyncRetryOptionOnce f conn


/// RethinkDB computation expression
let rethink<'T> = RethinkBuilder<'T> ()
