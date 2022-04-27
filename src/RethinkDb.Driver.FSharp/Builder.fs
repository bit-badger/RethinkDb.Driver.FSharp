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
    
    /// Return None if the item is null, Some if it is not
    let noneIfNull (it : 'T) = match (box >> isNull) it with true -> None | false -> Some it
    
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
    member this.Table (r, tableName) =
        match dbAndTable tableName with
        | Some dbName, tblName -> this.Db (r, dbName) |> table tblName
        | None, _ -> fromTable tableName
    
    /// Create an equality join with another table
    [<CustomOperation "eqJoin">]
    member _.EqJoin (expr : ReqlExpr, field : string, otherTable : string) =
        expr.EqJoin (field, RethinkDB.R.Table otherTable)
    
    // meta queries (tables, indexes, etc.)
    
    /// List all databases
    [<CustomOperation "dbList">]
    member _.DbList (_ : RethinkDB) = dbList ()
    
    /// Create a database
    [<CustomOperation "dbCreate">]
    member _.DbCreate (_ : RethinkDB, dbName) = dbCreate dbName
    
    /// Drop a database
    [<CustomOperation "dbDrop">]
    member _.DbDrop (_ : RethinkDB, dbName) = dbDrop dbName
    
    /// List all tables for the default database
    [<CustomOperation "tableList">]
    member _.TableList (_ : RethinkDB) = tableListFromDefault ()
    
    /// List all tables for the specified database
    [<CustomOperation "tableList">]
    member this.TableList (r, dbName) =
        match dbName with "" -> tableListFromDefault () | _ -> this.Db (r, dbName) |> tableList
    
    /// Create a table (of form "dbName.tableName"; if no db name, uses default database)
    [<CustomOperation "tableCreate">]
    member this.TableCreate (r, tableName) =
        match dbAndTable tableName with
        | Some dbName, tblName -> this.Db (r, dbName) |> tableCreate tblName
        | None, _ -> tableCreateInDefault tableName
    
    /// Drop a table (of form "dbName.tableName"; if no db name, uses default database)
    [<CustomOperation "tableDrop">]
    member this.TableDrop (r, tableName) =
        match dbAndTable tableName with
        | Some dbName, tblName -> this.Db (r, dbName) |> tableDrop tblName
        | None, _ -> tableDropFromDefault tableName
    
    /// List all indexes for a table
    [<CustomOperation "indexList">]
    member _.IndexList tbl = indexList tbl
    
    /// Create an index for a table
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl, index) = indexCreate index tbl
    
    /// Create an index for a table, specifying an optional argument
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl, index, opts) = indexCreateWithOptArgs index opts tbl
    
    /// Create an index for a table, using a function to calculate the index
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl, index, f) = indexCreateFunc index f tbl
    
    /// Create an index for a table, using a function to calculate the index
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl, index, f, opts) = indexCreateFuncWithOptArgs index f opts tbl

    /// Create an index for a table, using a JavaScript function to calculate the index
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl, index, js) = indexCreateJS index js tbl
    
    /// Create an index for a table, using a JavaScript function to calculate the index
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl, index, js, opts) = indexCreateJSWithOptArgs index js opts tbl

    /// Create an index for a table, using an object expression
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl, index, expr : obj) = indexCreateObj index expr tbl
    
    /// Create an index for a table, using an object expression and optional arguments
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl, index, expr : obj, opts) = indexCreateObjWithOptArgs index expr opts tbl
    
    /// Drop an index for a table
    [<CustomOperation "indexDrop">]
    member _.IndexDrop (tbl, index) = indexDrop index tbl
    
    /// Rename an index on a table
    [<CustomOperation "indexRename">]
    member _.IndexRename (tbl, oldName, newName) = indexRename oldName newName tbl
    
    /// Rename an index on a table, specifying an overwrite option
    [<CustomOperation "indexRename">]
    member _.IndexRename (tbl, oldName, newName, opt) = indexRenameWithOptArg oldName newName opt tbl
    
    /// Get the status of all indexes on a table
    [<CustomOperation "indexStatus">]
    member _.IndexStatus tbl = indexStatusAll tbl
    
    /// Get the status of specific indexes on a table
    [<CustomOperation "indexStatus">]
    member _.IndexStatus (tbl, indexes) = indexStatus indexes tbl
    
    /// Wait for all indexes on a table to become ready
    [<CustomOperation "indexWait">]
    member _.IndexWait tbl = indexWaitAll tbl
    
    /// Wait for specific indexes on a table to become ready
    [<CustomOperation "indexWait">]
    member _.IndexWait (tbl, indexes) = indexWait indexes tbl
    
    // data retrieval / manipulation
    
    /// Get a document from a table by its ID
    [<CustomOperation "get">]
    member _.Get (tbl, key : obj) = get key tbl
    
    /// Get all documents matching the given primary key value
    [<CustomOperation "getAll">]
    member _.GetAll (tbl, keys) = getAll (Seq.ofList keys) tbl
    
    /// Get all documents matching the given index value
    [<CustomOperation "getAll">]
    member _.GetAll (tbl, keys, index) = getAllWithIndex (Seq.ofList keys) index tbl
    
    /// Skip a certain number of results
    [<CustomOperation "skip">]
    member _.Skip (expr, toSkip) = skip toSkip expr

    /// Limit the results of this query
    [<CustomOperation "limit">]
    member _.Limit (expr, toLimit) = limit toLimit expr

    /// Count documents for the current query
    [<CustomOperation "count">]
    member _.Count expr = count expr
    
    /// Count documents for the current query
    [<CustomOperation "count">]
    member _.Count (expr, f) = countFunc f expr
    
    /// Count documents for the current query
    [<CustomOperation "count">]
    member _.Count (expr, js) = countJS js expr
    
    /// Filter a query by a single field value
    [<CustomOperation "filter">]
    member _.Filter (expr, field, value) = filter (fieldsToMap [ field, value ]) expr
    
    /// Filter a query by a single field value, including an optional argument
    [<CustomOperation "filter">]
    member _.Filter (expr, field, value, opt) = filterWithOptArgs (fieldsToMap [ field, value ]) opt expr
    
    /// Filter a query by multiple field values
    [<CustomOperation "filter">]
    member _.Filter (expr, filters) = filter (fieldsToMap filters) expr
    
    /// Filter a query by multiple field values, including an optional argument
    [<CustomOperation "filter">]
    member _.Filter (expr, filters, opt) = filterWithOptArgs (fieldsToMap filters) opt expr
    
    /// Filter a query by a function
    [<CustomOperation "filter">]
    member _.Filter (expr, f) = filterFunc f expr
    
    /// Filter a query by a function, including an optional argument
    [<CustomOperation "filter">]
    member _.Filter (expr, f, opt) = filterFuncWithOptArgs f opt expr
    
    /// Filter a query by multiple functions (has the effect of ANDing them)
    [<CustomOperation "filter">]
    member _.Filter (expr, fs) = filterFuncAll fs expr
    
    /// Filter a query by multiple functions (has the effect of ANDing them), including an optional argument
    [<CustomOperation "filter">]
    member _.Filter (expr, fs, opt) = filterFuncAllWithOptArgs fs opt expr
    
    /// Filter a query using a JavaScript expression
    [<CustomOperation "filter">]
    member _.Filter (expr, js) = filterJS js expr
    
    /// Filter a query using a JavaScript expression, including an optional argument
    [<CustomOperation "filter">]
    member _.Filter (expr, js, opt) = filterJSWithOptArgs js opt expr
    
    /// Filter a query by a range of values
    [<CustomOperation "between">]
    member _.Between (expr, lower : obj, upper : obj) = between lower upper expr
    
    /// Filter a query by a range of values, using optional arguments
    [<CustomOperation "between">]
    member _.Between (expr, lower : obj, upper : obj, opts) = betweenWithOptArgs lower upper opts expr
        
    /// Map fields for the current query using a function
    [<CustomOperation "map">]
    member _.Map (expr, f) = mapFunc f expr

    /// Map fields for the current query using a JavaScript function
    [<CustomOperation "map">]
    member _.Map (expr, js) = mapJS js expr

    /// Exclude the given fields from the output
    [<CustomOperation "without">]
    member _.Without (expr, fields) = without (Seq.ofList fields) expr
    
    /// Combine a left and right selection into a single record
    [<CustomOperation "zip">]
    member _.Zip expr = zip expr
    
    /// Merge a document into the current query
    [<CustomOperation "merge">]
    member _.Merge (expr, doc : obj) = merge doc expr
    
    /// Merge a document into the current query, constructed by a function
    [<CustomOperation "merge">]
    member _.Merge (expr, f) = mergeFunc f expr
    
    /// Merge a document into the current query, constructed by a JavaScript function
    [<CustomOperation "merge">]
    member _.Merge (expr, js) = mergeJS js expr
    
    /// Pluck (select only) specific fields from the query
    [<CustomOperation "pluck">]
    member _.Pluck (expr, fields) = pluck (Seq.ofList fields) expr
    
    /// Order the results by the given field name (ascending)
    [<CustomOperation "orderBy">]
    member _.OrderBy (expr, field) = orderBy field expr

    /// Order the results by the given field name (descending)
    [<CustomOperation "orderByDescending">]
    member _.OrderByDescending (expr, field) = orderByDescending field expr

    /// Order the results by the given index name (ascending)
    [<CustomOperation "orderByIndex">]
    member _.OrderByIndex (expr, index) = orderByIndex index expr

    /// Order the results by the given index name (descending)
    [<CustomOperation "orderByIndexDescending">]
    member _.OrderByIndexDescending (expr, index) = orderByIndexDescending index expr

    /// Order the results by the given function value (ascending)
    [<CustomOperation "orderByFunc">]
    member _.OrderByFunc (expr, f) = orderByFunc f expr

    /// Order the results by the given function value (descending)
    [<CustomOperation "orderByFuncDescending">]
    member _.OrderByFuncDescending (expr, f) = orderByFuncDescending f expr

    /// Order the results by the given JavaScript function value (ascending)
    [<CustomOperation "orderByJS">]
    member _.OrderByJS (expr, js) = orderByJS js expr

    /// Order the results by the given JavaScript function value (descending)
    [<CustomOperation "orderByJSDescending">]
    member _.OrderByJSDescending (expr, js) = orderByJSDescending js expr

    /// Insert a document into the given table
    [<CustomOperation "insert">]
    member _.Insert (tbl, doc : obj) = insert doc tbl
    
    /// Insert multiple documents into the given table
    [<CustomOperation "insert">]
    member _.Insert (tbl, doc) = insertMany (Seq.ofList doc) tbl
    
    /// Insert a document into the given table, using optional arguments
    [<CustomOperation "insert">]
    member _.Insert (tbl, docs : obj, opts) = insertWithOptArgs docs opts tbl
    
    /// Insert multiple documents into the given table, using optional arguments
    [<CustomOperation "insert">]
    member _.Insert (tbl, docs, opts) = insertManyWithOptArgs (Seq.ofList docs) opts tbl
    
    /// Update specific fields in a document
    [<CustomOperation "update">]
    member _.Update (expr, fields) = update (fieldsToMap fields) expr
    
    /// Update specific fields in a document, using optional arguments
    [<CustomOperation "update">]
    member _.Update (expr, fields, args) = updateWithOptArgs (fieldsToMap fields) args expr
    
    /// Update specific fields in a document using a function
    [<CustomOperation "update">]
    member _.Update (expr, f) = updateFunc f expr
    
    /// Update specific fields in a document using a function, with optional arguments
    [<CustomOperation "update">]
    member _.Update (expr, f, args) = updateFuncWithOptArgs f args expr
    
    /// Update specific fields in a document using a JavaScript function
    [<CustomOperation "update">]
    member _.Update (expr, js) = updateJS js expr
    
    /// Update specific fields in a document using a JavaScript function, with optional arguments
    [<CustomOperation "update">]
    member _.Update (expr, js, args) = updateJSWithOptArgs js args expr
    
    /// Replace the current query with the specified document
    [<CustomOperation "replace">]
    member _.Replace (expr, doc : obj) = replace doc expr
    
    /// Replace the current query with the specified document, using optional arguments
    [<CustomOperation "replace">]
    member _.Replace (expr, doc : obj, args) = replaceWithOptArgs doc args expr
    
    /// Replace the current query with document(s) created by a function
    [<CustomOperation "replace">]
    member _.Replace (expr, f) = replaceFunc f expr
    
    /// Replace the current query with document(s) created by a function, using optional arguments
    [<CustomOperation "replace">]
    member _.Replace (expr, f, args) = replaceFuncWithOptArgs f args expr
    
    /// Replace the current query with document(s) created by a JavaScript function
    [<CustomOperation "replace">]
    member _.Replace (expr, js) = replaceJS js expr
    
    /// Replace the current query with document(s) created by a JavaScript function, using optional arguments
    [<CustomOperation "replace">]
    member _.Replace (expr, js, args) = replaceJSWithOptArgs js args expr
    
    /// Delete the document(s) identified by the current query
    [<CustomOperation "delete">]
    member _.Delete expr = delete expr

    /// Delete the document(s) identified by the current query
    [<CustomOperation "delete">]
    member _.Delete (expr, opts) = deleteWithOptArgs opts expr

    /// Wait for updates to a table to be synchronized to disk
    [<CustomOperation "sync">]
    member _.Sync tbl = sync tbl

    // executing queries
    
    /// Execute the query, returning the result of the type specified using the given cancellation token
    [<CustomOperation "result">]
    member _.Result (expr, cancelToken) = fun conn -> backgroundTask {
        return! runResultWithCancel<'T> cancelToken conn expr
    }
    
    /// Execute the query, returning the result of the type specified using the given cancellation token
    [<CustomOperation "result">]
    member _.Result (expr, cancelToken, conn) = runResultWithCancel<'T> cancelToken conn expr
    
    /// Execute the query, returning the result of the type specified
    [<CustomOperation "result">]
    member _.Result expr = fun conn -> backgroundTask {
        return! runResult<'T> conn expr
    }
    
    /// Execute the query, returning the result of the type specified
    [<CustomOperation "result">]
    member _.Result (expr, conn) = runResult<'T> conn expr
    
    /// Execute the query, returning the result of the type specified, using optional arguments
    [<CustomOperation "result">]
    member _.Result (expr, args) = fun conn -> backgroundTask {
        return! runResultWithOptArgs<'T> args conn expr 
    }
    
    /// Execute the query, returning the result of the type specified
    [<CustomOperation "result">]
    member _.Result (expr, args, conn) = runResultWithOptArgs<'T> args conn expr
    
    /// Execute the query, returning the result of the type specified, using optional arguments and a cancellation token
    [<CustomOperation "result">]
    member _.Result (expr, args, cancelToken) = fun conn -> backgroundTask {
        return! runResultWithOptArgsAndCancel<'T> args cancelToken conn expr 
    }
    
    /// Execute the query, returning the result of the type specified, using optional arguments and a cancellation token
    [<CustomOperation "result">]
    member _.Result (expr, args, cancelToken, conn) =
        runResultWithOptArgsAndCancel<'T> args cancelToken conn expr 
    
    /// Execute the query, returning the result of the type specified, or None if no result is found
    [<CustomOperation "resultOption">]
    member _.ResultOption expr = fun conn -> backgroundTask {
        let! result = runResult<'T> conn expr
        return noneIfNull result
    }
    
    /// Execute the query, returning the result of the type specified, or None if no result is found
    [<CustomOperation "resultOption">]
    member this.ResultOption (expr, conn) =
        this.ResultOption expr conn
    
    /// Execute the query with a cancellation token, returning the result of the type specified, or None if no result is
    /// found
    [<CustomOperation "resultOption">]
    member _.ResultOption (expr, cancelToken) = fun conn -> backgroundTask {
        let! result = runResultWithCancel<'T> cancelToken conn expr
        return noneIfNull result
    }
    
    /// Execute the query with a cancellation token, returning the result of the type specified, or None if no result is
    /// found
    [<CustomOperation "resultOption">]
    member this.ResultOption (expr : ReqlExpr, cancelToken : CancellationToken, conn) =
        this.ResultOption (expr, cancelToken) conn
    
    /// Execute the query with optional arguments, returning the result of the type specified, or None if no result is
    /// found
    [<CustomOperation "resultOption">]
    member _.ResultOption (expr, opts) = fun conn -> backgroundTask {
        let! result = runResultWithOptArgs<'T> opts conn expr
        return noneIfNull result
    }
    
    /// Execute the query with optional arguments, returning the result of the type specified, or None if no result is
    /// found
    [<CustomOperation "resultOption">]
    member this.ResultOption (expr, opts : RunOptArg list, conn) =
        this.ResultOption (expr, opts) conn
    
    /// Execute the query with optional arguments and a cancellation token, returning the result of the type specified,
    /// or None if no result is found
    [<CustomOperation "resultOption">]
    member _.ResultOption (expr, opts, cancelToken) = fun conn -> backgroundTask {
        let! result = runResultWithOptArgsAndCancel<'T> opts cancelToken conn expr
        return noneIfNull result
    }
    
    /// Execute the query with optional arguments and a cancellation token, returning the result of the type specified,
    /// or None if no result is found
    [<CustomOperation "resultOption">]
    member this.ResultOption (expr, opts : RunOptArg list, cancelToken : CancellationToken, conn) =
        this.ResultOption (expr, opts, cancelToken) conn
    
    /// Execute the query, returning the result of the type specified using the given cancellation token
    [<CustomOperation "asyncResult">]
    member _.AsyncResult (expr, cancelToken) = fun conn -> async {
        return! asyncResultWithCancel<'T> cancelToken conn expr
    }
    
    /// Execute the query, returning the result of the type specified using the given cancellation token
    [<CustomOperation "asyncResult">]
    member _.AsyncResult (expr, cancelToken, conn) = asyncResultWithCancel<'T> cancelToken conn expr
    
    /// Execute the query, returning the result of the type specified
    [<CustomOperation "asyncResult">]
    member _.AsyncResult expr = fun conn -> async {
        return! asyncResult<'T> conn expr
    }
    
    /// Execute the query, returning the result of the type specified
    [<CustomOperation "asyncResult">]
    member _.AsyncResult (expr, conn) = asyncResult<'T> conn expr
    
    /// Execute the query, returning the result of the type specified, using optional arguments
    [<CustomOperation "asyncResult">]
    member _.AsyncResult (expr, args) = fun conn -> async {
        return! asyncResultWithOptArgs<'T> args conn expr 
    }
    
    /// Execute the query, returning the result of the type specified
    [<CustomOperation "asyncResult">]
    member _.AsyncResult (expr, args, conn) = asyncResultWithOptArgs<'T> args conn expr
    
    /// Execute the query, returning the result of the type specified, using optional arguments and a cancellation token
    [<CustomOperation "asyncResult">]
    member _.AsyncResult (expr, args, cancelToken) = fun conn -> async {
        return! asyncResultWithOptArgsAndCancel<'T> args cancelToken conn expr 
    }
    
    /// Execute the query, returning the result of the type specified, using optional arguments and a cancellation token
    [<CustomOperation "asyncResult">]
    member _.AsyncResult (expr, args, cancelToken, conn) =
        asyncResultWithOptArgsAndCancel<'T> args cancelToken conn expr 
    
    /// Execute the query, returning the result of the type specified, or None if no result is found
    [<CustomOperation "asyncOption">]
    member _.AsyncOption expr = fun conn -> async {
        let! result = asyncResult<'T> conn expr
        return noneIfNull result
    }
    
    /// Execute the query, returning the result of the type specified, or None if no result is found
    [<CustomOperation "asyncOption">]
    member this.AsyncOption (expr, conn) =
        this.AsyncOption expr conn
    
    /// Execute the query with a cancellation token, returning the result of the type specified, or None if no result is
    /// found
    [<CustomOperation "asyncOption">]
    member _.AsyncOption (expr, cancelToken) = fun conn -> async {
        let! result = asyncResultWithCancel<'T> cancelToken conn expr
        return noneIfNull result
    }
    
    /// Execute the query with a cancellation token, returning the result of the type specified, or None if no result is
    /// found
    [<CustomOperation "asyncOption">]
    member this.AsyncOption (expr : ReqlExpr, cancelToken : CancellationToken, conn) =
        this.AsyncOption (expr, cancelToken) conn
    
    /// Execute the query with optional arguments, returning the result of the type specified, or None if no result is
    /// found
    [<CustomOperation "asyncOption">]
    member _.AsyncOption (expr, opts) = fun conn -> async {
        let! result = asyncResultWithOptArgs<'T> opts conn expr
        return noneIfNull result
    }
    
    /// Execute the query with optional arguments, returning the result of the type specified, or None if no result is
    /// found
    [<CustomOperation "asyncOption">]
    member this.AsyncOption (expr, opts : RunOptArg list, conn) =
        this.AsyncOption (expr, opts) conn
    
    /// Execute the query with optional arguments and a cancellation token, returning the result of the type specified,
    /// or None if no result is found
    [<CustomOperation "asyncOption">]
    member _.AsyncOption (expr, opts, cancelToken) = fun conn -> async {
        let! result = asyncResultWithOptArgsAndCancel<'T> opts cancelToken conn expr
        return noneIfNull result
    }
    
    /// Execute the query with optional arguments and a cancellation token, returning the result of the type specified,
    /// or None if no result is found
    [<CustomOperation "asyncOption">]
    member this.AsyncOption (expr, opts : RunOptArg list, cancelToken : CancellationToken, conn) =
        this.AsyncOption (expr, opts, cancelToken) conn
    
    /// Execute the query synchronously, returning the result of the type specified
    [<CustomOperation "syncResult">]
    member _.SyncResult expr = fun conn -> syncResult<'T> conn expr
    
    /// Execute the query synchronously, returning the result of the type specified
    [<CustomOperation "syncResult">]
    member _.SyncResult (expr, conn) = syncResult<'T> conn expr
    
    /// Execute the query synchronously, returning the result of the type specified, using optional arguments
    [<CustomOperation "syncResult">]
    member _.SyncResult (expr, args) = fun conn -> syncResultWithOptArgs<'T> args conn expr 
    
    /// Execute the query synchronously, returning the result of the type specified
    [<CustomOperation "syncResult">]
    member _.SyncResult (expr, args, conn) = syncResultWithOptArgs<'T> args conn expr
    
    /// Execute the query synchronously, returning the result of the type specified, or None if no result is found
    [<CustomOperation "syncOption">]
    member _.SyncOption expr = fun conn -> noneIfNull (syncResult<'T> conn expr)
    
    /// Execute the query synchronously, returning the result of the type specified, or None if no result is found
    [<CustomOperation "syncOption">]
    member _.SyncOption (expr, conn) = noneIfNull (syncResult<'T> conn expr)
    
    /// Execute the query synchronously with optional arguments, returning the result of the type specified, or None if
    /// no result is found
    [<CustomOperation "syncOption">]
    member _.SyncOption (expr, opts) = fun conn -> noneIfNull (syncResultWithOptArgs<'T> opts conn expr)
    
    /// Execute the query synchronously with optional arguments, returning the result of the type specified, or None if
    /// no result is found
    [<CustomOperation "syncOption">]
    member _.SyncOption (expr, opts, conn) = noneIfNull (syncResultWithOptArgs<'T> opts conn expr)
    
    /// Perform a write operation
    [<CustomOperation "write">]
    member _.Write expr = fun conn -> runWrite conn expr

    /// Perform a write operation
    [<CustomOperation "write">]
    member _.Write (expr, conn) = runWrite conn expr
        
    /// Perform a write operation using optional arguments
    [<CustomOperation "write">]
    member _.Write (expr, args) = fun conn -> runWriteWithOptArgs args conn expr

    /// Perform a write operation using optional arguments
    [<CustomOperation "write">]
    member _.Write (expr, args, conn) = runWriteWithOptArgs args conn expr
        
    /// Perform a write operation using a cancellation token
    [<CustomOperation "write">]
    member _.Write (expr, cancelToken) = fun conn -> runWriteWithCancel cancelToken conn expr

    /// Perform a write operation using a cancellation token
    [<CustomOperation "write">]
    member _.Write (expr, cancelToken, conn) = runWriteWithCancel cancelToken conn expr
        
    /// Perform a write operation using optional arguments and a cancellation token
    [<CustomOperation "write">]
    member _.Write (expr, args, cancelToken) = fun conn -> runWriteWithOptArgsAndCancel args cancelToken conn expr

    /// Perform a write operation using optional arguments and a cancellation token
    [<CustomOperation "write">]
    member _.Write (expr, args, cancelToken, conn) = runWriteWithOptArgsAndCancel args cancelToken conn expr
        
    /// Perform a write operation
    [<CustomOperation "asyncWrite">]
    member _.AsyncWrite expr = fun conn -> asyncWrite conn expr

    /// Perform a write operation
    [<CustomOperation "asyncWrite">]
    member _.AsyncWrite (expr, conn) = asyncWrite conn expr
        
    /// Perform a write operation using optional arguments
    [<CustomOperation "asyncWrite">]
    member _.AsyncWrite (expr, args) = fun conn -> asyncWriteWithOptArgs args conn expr

    /// Perform a write operation using optional arguments
    [<CustomOperation "asyncWrite">]
    member _.AsyncWrite (expr, args, conn) = asyncWriteWithOptArgs args conn expr
        
    /// Perform a write operation using a cancellation token
    [<CustomOperation "asyncWrite">]
    member _.AsyncWrite (expr, cancelToken) = fun conn -> asyncWriteWithCancel cancelToken conn expr

    /// Perform a write operation using a cancellation token
    [<CustomOperation "asyncWrite">]
    member _.AsyncWrite (expr, cancelToken, conn) = asyncWriteWithCancel cancelToken conn expr
        
    /// Perform a write operation using optional arguments and a cancellation token
    [<CustomOperation "asyncWrite">]
    member _.AsyncWrite (expr, args, cancelToken) = fun conn ->
        asyncWriteWithOptArgsAndCancel args cancelToken conn expr

    /// Perform a write operation using optional arguments and a cancellation token
    [<CustomOperation "asyncWrite">]
    member _.AsyncWrite (expr, args, cancelToken, conn) = asyncWriteWithOptArgsAndCancel args cancelToken conn expr
        
    /// Perform a synchronous write operation
    [<CustomOperation "syncWrite">]
    member _.SyncWrite expr = fun conn -> syncWrite conn expr

    /// Perform a synchronous write operation
    [<CustomOperation "syncWrite">]
    member _.SyncWrite (expr, conn) = syncWrite conn expr
        
    /// Perform a write operation using optional arguments
    [<CustomOperation "syncWrite">]
    member _.SyncWrite (expr, args) = fun conn -> syncWriteWithOptArgs args conn expr

    /// Perform a write operation using optional arguments
    [<CustomOperation "syncWrite">]
    member _.SyncWrite (expr, args, conn) = syncWriteWithOptArgs args conn expr
        
    /// Perform a write operation, returning a result even if there are errors
    [<CustomOperation "writeResult">]
    member _.WriteResult expr = fun conn -> runWriteResult conn expr

    /// Perform a write operation, returning a result even if there are errors
    [<CustomOperation "writeResult">]
    member _.WriteResult (expr, conn) = runWriteResult conn expr
        
    /// Perform a write operation with optional arguments, returning a result even if there are errors
    [<CustomOperation "writeResult">]
    member _.WriteResult (expr, args) = fun conn -> runWriteResultWithOptArgs args conn expr

    /// Perform a write operation with optional arguments, returning a result even if there are errors
    [<CustomOperation "writeResult">]
    member _.WriteResult (expr, args, conn) = runWriteResultWithOptArgs args conn expr
        
    /// Perform a write operation with a cancellation token, returning a result even if there are errors
    [<CustomOperation "writeResult">]
    member _.WriteResult (expr, cancelToken) = fun conn -> runWriteResultWithCancel cancelToken conn expr

    /// Perform a write operation with a cancellation token, returning a result even if there are errors
    [<CustomOperation "writeResult">]
    member _.WriteResult (expr, cancelToken, conn) = runWriteResultWithCancel cancelToken conn expr
        
    /// Perform a write operation with optional arguments and a cancellation token, returning a result even if there are
    /// errors
    [<CustomOperation "writeResult">]
    member _.WriteResult (expr, args, cancelToken) = fun conn ->
        runWriteResultWithOptArgsAndCancel args cancelToken conn expr

    /// Perform a write operation with optional arguments and a cancellation token, returning a result even if there are
    /// errors
    [<CustomOperation "writeResult">]
    member _.WriteResult (expr, args, cancelToken, conn) = runWriteResultWithOptArgsAndCancel args cancelToken conn expr
        
    /// Perform a write operation, returning a result even if there are errors
    [<CustomOperation "asyncWriteResult">]
    member _.AsyncWriteResult expr = fun conn -> asyncWriteResult conn expr

    /// Perform a write operation, returning a result even if there are errors
    [<CustomOperation "asyncWriteResult">]
    member _.AsyncWriteResult (expr, conn) = asyncWriteResult conn expr
        
    /// Perform a write operation with optional arguments, returning a result even if there are errors
    [<CustomOperation "asyncWriteResult">]
    member _.AsyncWriteResult (expr, args) = fun conn -> asyncWriteResultWithOptArgs args conn expr

    /// Perform a write operation with optional arguments, returning a result even if there are errors
    [<CustomOperation "asyncWriteResult">]
    member _.AsyncWriteResult (expr, args, conn) = asyncWriteResultWithOptArgs args conn expr
        
    /// Perform a write operation with a cancellation token, returning a result even if there are errors
    [<CustomOperation "asyncWriteResult">]
    member _.AsyncWriteResult (expr, cancelToken) = fun conn -> asyncWriteResultWithCancel cancelToken conn expr

    /// Perform a write operation with a cancellation token, returning a result even if there are errors
    [<CustomOperation "asyncWriteResult">]
    member _.AsyncWriteResult (expr, cancelToken, conn) = asyncWriteResultWithCancel cancelToken conn expr
        
    /// Perform a write operation with optional arguments and a cancellation token, returning a result even if there are
    /// errors
    [<CustomOperation "asyncWriteResult">]
    member _.AsyncWriteResult (expr, args, cancelToken) = fun conn ->
        asyncWriteResultWithOptArgsAndCancel args cancelToken conn expr

    /// Perform a write operation with optional arguments and a cancellation token, returning a result even if there are
    /// errors
    [<CustomOperation "asyncWriteResult">]
    member _.AsyncWriteResult (expr, args, cancelToken, conn) =
        asyncWriteResultWithOptArgsAndCancel args cancelToken conn expr
        
    /// Perform a synchronous write operation, returning a result even if there are errors
    [<CustomOperation "syncWriteResult">]
    member _.SyncWriteResult expr = fun conn -> syncWriteResult conn expr

    /// Perform a synchronous write operation, returning a result even if there are errors
    [<CustomOperation "syncWriteResult">]
    member _.SyncWriteResult (expr, conn) = syncWriteResult conn expr
        
    /// Perform a synchronous write operation with optional arguments, returning a result even if there are errors
    [<CustomOperation "syncWriteResult">]
    member _.SyncWriteResult (expr, args) = fun conn -> syncWriteResultWithOptArgs args conn expr

    /// Perform a synchronous write operation with optional arguments, returning a result even if there are errors
    [<CustomOperation "syncWriteResult">]
    member _.SyncWriteResult (expr, args, conn) = syncWriteResultWithOptArgs args conn expr
        
    /// Ignore the result of an operation
    [<CustomOperation "ignoreResult">]
    member _.IgnoreResult (f : IConnection -> Task<'T>) = fun conn -> task {
        let! _ = (f conn).ConfigureAwait false
        ()
    }

    /// Ignore the result of an operation
    [<CustomOperation "ignoreResult">]
    member this.IgnoreResult (f : IConnection -> Task<'T>, conn) =
        this.IgnoreResult f conn

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
    member _.WithRetry (f, retries) = withRetry<'T> retries f

    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withRetry">]
    member _.WithRetry (f, retries, conn) = withRetry<'T> retries f conn
   
    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withRetryOption">]
    member _.WithRetryOption (f, retries) = withRetry<'T option> retries f

    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withRetryOption">]
    member _.WithRetryOption (f, retries, conn) = withRetry<'T option> retries f conn
   
    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withAsyncRetry">]
    member _.WithAsyncRetry (f, retries) = withAsyncRetry<'T> retries f

    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withAsyncRetry">]
    member _.WithAsyncRetry (f, retries, conn) = withAsyncRetry<'T> retries f conn
   
    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withAsyncRetryOption">]
    member _.WithAsyncRetryOption (f, retries) = withAsyncRetry<'T option> retries f

    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withAsyncRetryOption">]
    member _.WithAsyncRetryOption (f, retries, conn) = withAsyncRetry<'T option> retries f conn
   
    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withSyncRetry">]
    member _.WithSyncRetry (f, retries) = withSyncRetry<'T> retries f

    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withSyncRetry">]
    member _.WithSyncRetry (f, retries, conn) = withSyncRetry<'T> retries f conn
   
    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withSyncRetryOption">]
    member _.WithSyncRetryOption (f, retries) = withSyncRetry<'T option> retries f

    /// Retries a variable number of times, waiting each time for the seconds specified
    [<CustomOperation "withSyncRetryOption">]
    member _.WithSyncRetryOption (f, retries, conn) = withSyncRetry<'T option> retries f conn
   
    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withRetryDefault">]
    member _.WithRetryDefault f = withRetryDefault<'T> f

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withRetryDefault">]
    member _.WithRetryDefault (f, conn) = withRetryDefault<'T> f conn

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withRetryOptionDefault">]
    member _.WithRetryOptionDefault f = withRetryDefault<'T option> f

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withRetryOptionDefault">]
    member _.WithRetryOptionDefault (f, conn) = withRetryDefault<'T option> f conn

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withAsyncRetryDefault">]
    member _.WithAsyncRetryDefault f = withAsyncRetryDefault<'T> f

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withAsyncRetryDefault">]
    member _.WithAsyncRetryDefault (f, conn) = withAsyncRetryDefault<'T> f conn

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withAsyncRetryOptionDefault">]
    member _.WithAsyncRetryOptionDefault f = withAsyncRetryDefault<'T option> f

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withAsyncRetryOptionDefault">]
    member _.WithAsyncRetryOptionDefault (f, conn) = withAsyncRetryDefault<'T option> f conn

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withSyncRetryDefault">]
    member _.WithSyncRetryDefault f = withSyncRetryDefault<'T> f

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withSyncRetryDefault">]
    member _.WithSyncRetryDefault (f, conn) = withSyncRetryDefault<'T> f conn

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withSyncRetryOptionDefault">]
    member _.WithSyncRetryOptionDefault f = withSyncRetryDefault<'T option> f

    /// Retries at 200ms, 500ms, and 1s
    [<CustomOperation "withSyncRetryOptionDefault">]
    member _.WithSyncRetryOptionDefault (f, conn) = withSyncRetryDefault<'T option> f conn

    /// Retries once immediately
    [<CustomOperation "withRetryOnce">]
    member _.WithRetryOnce f = withRetryOnce<'T> f

    /// Retries once immediately
    [<CustomOperation "withRetryOnce">]
    member _.WithRetryOnce (f, conn) = withRetryOnce<'T> f conn

    /// Retries once immediately
    [<CustomOperation "withRetryOptionOnce">]
    member _.WithRetryOptionOnce f = withRetryOnce<'T option> f

    /// Retries once immediately
    [<CustomOperation "withRetryOptionOnce">]
    member _.WithRetryOptionOnce (f, conn) = withRetryOnce<'T option> f conn

    /// Retries once immediately
    [<CustomOperation "withAsyncRetryOnce">]
    member _.WithAsyncRetryOnce f = withAsyncRetryOnce<'T> f

    /// Retries once immediately
    [<CustomOperation "withAsyncRetryOnce">]
    member _.WithAsyncRetryOnce (f, conn) = withAsyncRetryOnce<'T> f conn

    /// Retries once immediately
    [<CustomOperation "withAsyncRetryOptionOnce">]
    member _.WithAsyncRetryOptionOnce f = withAsyncRetryOnce<'T option> f

    /// Retries once immediately
    [<CustomOperation "withAsyncRetryOptionOnce">]
    member _.WithAsyncRetryOptionOnce (f, conn) = withAsyncRetryOnce<'T option> f conn

    /// Retries once immediately
    [<CustomOperation "withSyncRetryOnce">]
    member _.WithSyncRetryOnce f = withSyncRetryOnce<'T> f

    /// Retries once immediately
    [<CustomOperation "withSyncRetryOnce">]
    member _.WithSyncRetryOnce (f, conn) = withSyncRetryOnce<'T> f conn

    /// Retries once immediately
    [<CustomOperation "withSyncRetryOptionOnce">]
    member _.WithSyncRetryOptionOnce f = withSyncRetryOnce<'T option> f

    /// Retries once immediately
    [<CustomOperation "withSyncRetryOptionOnce">]
    member _.WithSyncRetryOptionOnce (f, conn) = withSyncRetryOnce<'T option> f conn


/// RethinkDB computation expression
let rethink<'T> = RethinkBuilder<'T> ()
