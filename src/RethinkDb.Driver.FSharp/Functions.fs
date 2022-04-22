/// The function-based Domain-Specific Language (DSL) for RethinkDB
module RethinkDb.Driver.FSharp.Functions

open System.Threading
open RethinkDb.Driver
open RethinkDb.Driver.Ast

[<AutoOpen>]
module private Helpers =
    /// Shorthand for the starting point for ReQL commands
    let r = RethinkDB.R

    /// Create a Javascript object from a string (used mostly for type inference)
    let toJS (js : string) = Javascript js

// ~~ EXECUTION ~~

/// Get a cursor with the results of an expression
let asyncCursor<'T> conn (expr : ReqlExpr) =
    expr.RunCursorAsync<'T> conn
    |> Async.AwaitTask

/// Raise an exception if a write command encountered an error
let private raiseIfWriteError (result : Model.Result) =
    match result.Errors with
    | 0UL -> result
    | _ -> raise <| ReqlRuntimeError result.FirstError
    
/// Write a ReQL command, raising an exception if an error occurs
let runWriteWithCancel cancelToken conn (expr : ReqlExpr) = backgroundTask {
    let! result = expr.RunWriteAsync (conn, cancelToken)
    return raiseIfWriteError result
}

/// Write a ReQL command, raising an exception if an error occurs
let runWrite conn expr = runWriteWithCancel CancellationToken.None conn expr

/// Write a ReQL command with optional arguments, raising an exception if an error occurs
let runWriteWithOptArgsAndCancel args cancelToken conn (expr : ReqlExpr) = backgroundTask {
    let! result = expr.RunWriteAsync (conn, RunOptArg.create args, cancelToken)
    return raiseIfWriteError result
}

/// Write a ReQL command with optional arguments, raising an exception if an error occurs
let runWriteWithOptArgs args conn expr = runWriteWithOptArgsAndCancel args CancellationToken.None conn expr

/// Write a ReQL command, raising an exception if an error occurs
let asyncWrite conn expr = runWrite conn expr |> Async.AwaitTask

/// Write a ReQL command with optional arguments, raising an exception if an error occurs
let asyncWriteWithOptArgs args conn expr = runWriteWithOptArgs args conn expr |> Async.AwaitTask

/// Write a ReQL command, raising an exception if an error occurs
let asyncWriteWithCancel cancelToken conn expr = runWriteWithCancel cancelToken conn expr |> Async.AwaitTask

/// Write a ReQL command with optional arguments, raising an exception if an error occurs
let asyncWriteWithOptArgsAndCancel args cancelToken conn expr =
    runWriteWithOptArgsAndCancel args cancelToken conn expr |> Async.AwaitTask

/// Write a ReQL command synchronously, raising an exception if an error occurs
let syncWrite conn expr = asyncWrite conn expr |> Async.RunSynchronously

/// Write a ReQL command synchronously with optional arguments, raising an exception if an error occurs
let syncWriteWithOptArgs args conn expr = asyncWriteWithOptArgs args conn expr |> Async.RunSynchronously

/// Write a ReQL command with a cancellation token, always returning a result
let runWriteResultWithCancel cancelToken conn (expr : ReqlExpr) =
    expr.RunWriteAsync (conn, cancelToken)

/// Write a ReQL command, always returning a result
let runWriteResult conn expr = runWriteResultWithCancel CancellationToken.None conn expr

/// Write a ReQL command with optional arguments and a cancellation token, always returning a result
let runWriteResultWithOptArgsAndCancel args cancelToken conn (expr : ReqlExpr) =
    expr.RunWriteAsync (conn, RunOptArg.create args, cancelToken)

/// Write a ReQL command with optional arguments, always returning a result
let runWriteResultWithOptArgs args conn expr = runWriteResultWithOptArgsAndCancel args CancellationToken.None conn expr

/// Write a ReQL command, always returning a result
let asyncWriteResult conn expr = runWriteResult conn expr |> Async.AwaitTask

/// Write a ReQL command with optional arguments, always returning a result
let asyncWriteResultWithOptArgs args conn expr = runWriteResultWithOptArgs args conn expr |> Async.AwaitTask

/// Write a ReQL command with a cancellation token, always returning a result
let asyncWriteResultWithCancel cancelToken conn expr = runWriteResultWithCancel cancelToken conn expr |> Async.AwaitTask

/// Write a ReQL command with optional arguments and a cancellation token, always returning a result
let asyncWriteResultWithOptArgsAndCancel args cancelToken conn expr =
    runWriteResultWithOptArgsAndCancel args cancelToken conn expr |> Async.AwaitTask

/// Write a ReQL command synchronously, always returning a result
let syncWriteResult conn expr = asyncWriteResult conn expr |> Async.RunSynchronously

/// Write a ReQL command synchronously with optional arguments, always returning a result
let syncWriteResultWithOptArgs args conn expr = asyncWriteResultWithOptArgs args conn expr |> Async.RunSynchronously

/// Run the ReQL command using a cancellation token, returning the result as the type specified
let runResultWithCancel<'T> cancelToken conn (expr : ReqlExpr) = expr.RunResultAsync<'T> (conn, cancelToken)

/// Run the ReQL command using optional arguments and a cancellation token, returning the result as the type specified
let runResultWithOptArgsAndCancel<'T> args cancelToken conn (expr : ReqlExpr) =
    expr.RunResultAsync<'T> (conn, RunOptArg.create args, cancelToken)

/// Run the ReQL command, returning the result as the type specified
let runResult<'T> = runResultWithCancel<'T> CancellationToken.None

/// Run the ReQL command using optional arguments, returning the result as the type specified
let runResultWithOptArgs<'T> args = runResultWithOptArgsAndCancel<'T> args CancellationToken.None

/// Run the ReQL command, returning the result as the type specified
let asyncResult<'T> conn expr =
    runResult<'T> expr conn |> Async.AwaitTask

/// Run the ReQL command using optional arguments, returning the result as the type specified
let asyncResultWithOptArgs<'T> args conn expr =
    runResultWithOptArgs<'T> args conn expr |> Async.AwaitTask

/// Run the ReQL command using a cancellation token, returning the result as the type specified
let asyncResultWithCancel<'T> cancelToken conn (expr : ReqlExpr) =
    runResultWithCancel<'T> cancelToken conn expr |> Async.AwaitTask

/// Run the ReQL command using optional arguments and a cancellation token, returning the result as the type specified
let asyncResultWithOptArgsAndCancel<'T> args cancelToken conn expr =
    runResultWithOptArgsAndCancel<'T> args cancelToken conn expr |> Async.AwaitTask

/// Run the ReQL command, returning the result as the type specified
let syncResult<'T> conn expr =
    asyncResult<'T> expr conn |> Async.RunSynchronously

/// Run the ReQL command using optional arguments, returning the result as the type specified
let syncResultWithOptArgs<'T> args conn expr =
    asyncResultWithOptArgs<'T> args conn expr |> Async.RunSynchronously

// ~~ QUERY DEFINITION ~~

/// Get documents between a lower bound and an upper bound based on a primary key
let between (lowerKey : obj) (upperKey : obj) (expr : ReqlExpr) =
    expr.Between (lowerKey, upperKey)

/// Get document between a lower bound and an upper bound, specifying one or more optional arguments
let betweenWithOptArgs (lowerKey : obj) (upperKey : obj) args expr =
    between lowerKey upperKey expr |> BetweenOptArg.apply args

/// Get documents between a lower bound and an upper bound based on an index
let betweenIndex (lowerKey : obj) (upperKey : obj) index expr =
    betweenWithOptArgs lowerKey upperKey [ Index index ] expr

/// Get a connection builder that can be used to create one RethinkDB connection
let connection () =
    r.Connection ()

/// Count the documents in this query
let count (expr : ReqlExpr) =
    expr.Count ()
    
/// Count the documents in this query where the function returns true 
let countFunc (f : ReqlExpr -> bool) (expr : ReqlExpr) =
    expr.Count (ReqlFunction1 (fun row -> f row :> obj))
    
/// Count the documents in this query where the function returns true 
let countJS js (expr : ReqlExpr) =
    expr.Count (toJS js)
    
/// Reference a database
let db dbName =
    match dbName with "" -> r.Db () | _ -> r.Db dbName

/// Create a database
let dbCreate (dbName : string) =
    r.DbCreate dbName

/// Drop a database
let dbDrop (dbName : string) =
    r.DbDrop dbName

/// Get a list of databases
let dbList () =
    r.DbList ()

/// Delete documents
let delete (expr : ReqlExpr) =
    expr.Delete ()

/// Delete documents, providing optional arguments
let deleteWithOptArgs args (expr : ReqlExpr) =
    delete expr |> DeleteOptArg.apply args

/// EqJoin the left field on the right-hand table using its primary key
let eqJoin (field : string) (table : Table) (expr : ReqlExpr) =
    expr.EqJoin (field, table)

/// EqJoin the left function on the right-hand table using its primary key
let eqJoinFunc<'T> (f : ReqlExpr -> 'T) (table : Table) (expr : ReqlExpr) =
    expr.EqJoin (ReqlFunction1 (fun row -> f row :> obj), table)

/// EqJoin the left function on the right-hand table using the specified index
let eqJoinFuncIndex<'T> (f : ReqlExpr -> 'T) table (indexName : string) expr =
    (eqJoinFunc f table expr).OptArg ("index", indexName)

/// EqJoin the left field on the right-hand table using the specified index
let eqJoinIndex field table (indexName : string) expr =
    (eqJoin field table expr).OptArg ("index", indexName)

/// EqJoin the left JavaScript on the right-hand table using its primary key
let eqJoinJS js (table : Table) (expr : ReqlExpr) =
    expr.EqJoin (toJS js, table)

/// EqJoin the left JavaScript on the right-hand table using the specified index
let eqJoinJSIndex js table (indexName : string) expr =
    (eqJoinJS js table expr).OptArg ("index", indexName)

/// Filter documents
let filter (filterSpec : obj) (expr : ReqlExpr) =
    expr.Filter filterSpec

/// Filter documents, providing optional arguments
let filterWithOptArgs (filterSpec : obj) arg expr =
    filter filterSpec expr |> FilterOptArg.apply arg

/// Filter documents using a function
let filterFunc f (expr : ReqlExpr) =
    expr.Filter (ReqlFunction1 f)

/// Filter documents using a function, providing optional arguments
let filterFuncWithOptArgs f arg expr =
    filterFunc f expr |> FilterOptArg.apply arg

/// Filter documents using multiple functions (has the effect of ANDing them)
let filterFuncAll fs expr =
    (fs |> List.fold (fun (e : ReqlExpr) f -> filterFunc f e) expr) :?> Filter

/// Filter documents using multiple functions (has the effect of ANDing them), providing optional arguments
let filterFuncAllWithOptArgs fs arg expr =
    filterFuncAll fs expr |> FilterOptArg.apply arg

/// Filter documents using JavaScript
let filterJS js (expr : ReqlExpr) =
    expr.Filter (toJS js)

/// Filter documents using JavaScript, providing optional arguments
let filterJSWithOptArgs js arg expr =
    filterJS js expr |> FilterOptArg.apply arg

/// Get a document by its primary key
let get (documentId : obj) (table : Table) =
    table.Get documentId

/// Get all documents matching primary keys
let getAll (ids : obj seq) (table : Table) =
    table.GetAll (Array.ofSeq ids)

/// Get all documents matching keys in the given index
let getAllWithIndex (ids : obj seq) (indexName : string) table =
    (getAll ids table).OptArg ("index", indexName)

/// Create an index on the given table
let indexCreate (indexName : string) (table : Table) =
    table.IndexCreate indexName

/// Create an index on the given table, including optional arguments
let indexCreateWithOptArgs (indexName : string) args (table : Table) =
    indexCreate indexName table |> IndexCreateOptArg.apply args

/// Create an index on the given table using a function
let indexCreateFunc (indexName : string) f (table : Table) =
    table.IndexCreate (indexName, ReqlFunction1 f)

/// Create an index on the given table using a function, including optional arguments
let indexCreateFuncWithOptArgs indexName f args table =
    indexCreateFunc indexName f table |> IndexCreateOptArg.apply args

/// Create an index on the given table using JavaScript
let indexCreateJS (indexName : string) js (table : Table) =
    table.IndexCreate (indexName, toJS js)

/// Create an index on the given table using JavaScript, including optional arguments
let indexCreateJSWithOptArgs indexName js args table =
    indexCreateJS indexName js table |> IndexCreateOptArg.apply args

/// Drop an index
let indexDrop (indexName : string) (table : Table) =
    table.IndexDrop indexName

/// Get a list of indexes for the given table
let indexList (table : Table) =
    table.IndexList ()

/// Rename an index (will fail if new name already exists)
let indexRename (oldName : string) (newName : string) (table : Table) =
    table.IndexRename (oldName, newName)

/// Rename an index (specifying overwrite action)
let indexRenameWithOptArg oldName newName arg table =
    indexRename oldName newName table |> IndexRenameOptArg.apply arg

/// Get the status of specific indexes for the given table
let indexStatus (indexes : string list) (table : Table) =
    table.IndexStatus (Array.ofList indexes)

/// Get the status of all indexes for the given table
let indexStatusAll (table : Table) =
    table.IndexStatus ()

/// Wait for specific indexes on the given table to become ready
let indexWait (indexes : string list) (table : Table) =
    table.IndexWait (Array.ofList indexes)

/// Wait for all indexes on the given table to become ready
let indexWaitAll (table : Table) =
    table.IndexWait ()

/// Create an inner join between two sequences, specifying the join condition with a function
let innerJoinFunc<'T> (otherSeq : obj) (f : ReqlExpr -> ReqlExpr -> 'T) (expr : ReqlExpr) =
    expr.InnerJoin (otherSeq, ReqlFunction2 (fun f1 f2 -> f f1 f2 :> obj))

/// Create an inner join between two sequences, specifying the join condition with JavaScript
let innerJoinJS (otherSeq : obj) js (expr : ReqlExpr) =
    expr.InnerJoin (otherSeq, toJS js)

/// Insert a single document (use insertMany for multiple)
let insert (doc : obj) (table : Table) =
    table.Insert doc

/// Insert multiple documents
let insertMany (docs : obj seq) (table : Table) =
    table.Insert (Array.ofSeq docs)

/// Insert a single document, providing optional arguments (use insertManyWithOptArgs for multiple)
let insertWithOptArgs (doc : obj) args table =
    insert doc table |> InsertOptArg.apply args

/// Insert multiple documents, providing optional arguments
let insertManyWithOptArgs (docs : obj seq) args table =
    insertMany docs table |> InsertOptArg.apply args

/// Test whether a sequence is empty
let isEmpty (expr : ReqlExpr) =
    expr.IsEmpty ()

/// End a sequence after a given number of elements
let limit (n : int) (expr : ReqlExpr) =
    expr.Limit n

/// Map the results using a function
let mapFunc f (expr : ReqlExpr) =
    expr.Map (ReqlFunction1 f)

/// Map the results using a JavaScript function
let mapJS js (expr : ReqlExpr) =
    expr.Map (toJS js)

/// Merge the current query with given document
let merge (doc : obj) (expr : ReqlExpr) =
    expr.Merge doc
    
/// Merge the current query with the results of a function
let mergeFunc f (expr : ReqlExpr) =
    expr.Merge (ReqlFunction1 f)
    
/// Merge the current query with the results of a JavaScript function
let mergeJS js (expr : ReqlExpr) =
    expr.Merge (toJS js)
    
/// Retrieve the nth element in a sequence
let nth n (expr : ReqlExpr) =
    expr.Nth n

/// Order a sequence by a given field
let orderBy (field : string) (expr : ReqlExpr) =
    expr.OrderBy field

/// Order a sequence in descending order by a given field
let orderByDescending (field : string) (expr : ReqlExpr) =
    expr.OrderBy (r.Desc field)
    
/// Order a sequence by a given function
let orderByFunc f (expr : ReqlExpr) =
    expr.OrderBy (ReqlFunction1 f)

/// Order a sequence in descending order by a given function
let orderByFuncDescending f (expr : ReqlExpr) =
    expr.OrderBy (r.Desc (ReqlFunction1 f))

/// Order a sequence by a given index
let orderByIndex (index : string) (expr : ReqlExpr) =
    expr.OrderBy().OptArg("index", index)

/// Order a sequence in descending order by a given index
let orderByIndexDescending (index : string) (expr : ReqlExpr) =
    expr.OrderBy().OptArg("index", r.Desc index)

/// Order a sequence by a given JavaScript function
let orderByJS js (expr : ReqlExpr) =
    expr.OrderBy (toJS js)

/// Order a sequence in descending order by a given JavaScript function
let orderByJSDescending js (expr : ReqlExpr) =
    expr.OrderBy (r.Desc (toJS js))

/// Create an outer join between two sequences, specifying the join condition with a function
let outerJoinFunc<'T> (otherSeq : obj) (f : ReqlExpr -> ReqlExpr -> 'T) (expr : ReqlExpr) =
    expr.OuterJoin (otherSeq, ReqlFunction2 (fun f1 f2 -> f f1 f2 :> obj))

/// Create an outer join between two sequences, specifying the join condition with JavaScript
let outerJoinJS (otherSeq : obj) js (expr : ReqlExpr) =
    expr.OuterJoin (otherSeq, toJS js)

/// Select one or more attributes from an object or sequence
let pluck (fields : string seq) (expr : ReqlExpr) =
    expr.Pluck (Array.ofSeq fields)

/// Replace documents
let replace (replaceSpec : obj) (expr : ReqlExpr) =
    expr.Replace replaceSpec

/// Replace documents, providing optional arguments
let replaceWithOptArgs (replaceSpec : obj) args expr =
    replace replaceSpec expr |> ReplaceOptArg.apply args

/// Replace documents using a function
let replaceFunc f (expr : ReqlExpr) =
    expr.Replace (ReqlFunction1 f)

/// Replace documents using a function, providing optional arguments
let replaceFuncWithOptArgs f args expr =
    replaceFunc f expr |> ReplaceOptArg.apply args

/// Replace documents using JavaScript
let replaceJS js (expr : ReqlExpr) =
    expr.Replace (toJS js)

/// Replace documents using JavaScript, providing optional arguments
let replaceJSWithOptArgs js args expr =
    replaceJS js expr |> ReplaceOptArg.apply args

/// Skip a number of elements from the head of a sequence
let skip (n : int) (expr : ReqlExpr) =
    expr.Skip n

/// Ensure changes to a table are written to permanent storage
let sync (table : Table) =
    table.Sync ()

/// Return all documents in a table (may be further refined)
let table tableName (db : Db) =
    db.Table tableName

/// Return all documents in a table from the default database (may be further refined)
let fromTable tableName =
    r.Table tableName

/// Create a table in the given database
let tableCreate tableName (db : Db) =
    db.TableCreate tableName

/// Create a table in the connection-default database
let tableCreateInDefault tableName =
    r.TableCreate tableName

/// Drop a table in the given database
let tableDrop tableName (db : Db) =
    db.TableDrop tableName

/// Drop a table from the connection-default database
let tableDropFromDefault tableName =
    r.TableDrop tableName

/// Get a list of tables for the given database
let tableList (db : Db) =
    db.TableList ()

/// Get a list of tables from the connection-default database
let tableListFromDefault () =
    r.TableList ()

/// Update documents
let update (updateSpec : obj) (expr : ReqlExpr) =
    expr.Update updateSpec

/// Update documents, providing optional arguments
let updateWithOptArgs (updateSpec : obj) args expr =
    update updateSpec expr |> UpdateOptArg.apply args

/// Update documents using a function
let updateFunc f (expr : ReqlExpr) =
    expr.Update (ReqlFunction1 f)

/// Update documents using a function, providing optional arguments
let updateFuncWithOptArgs f args expr =
    updateFunc f expr |> UpdateOptArg.apply args

/// Update documents using JavaScript
let updateJS js (expr : ReqlExpr) =
    expr.Update (toJS js)

/// Update documents using JavaScript, providing optional arguments
let updateJSWithOptArgs js args expr =
    updateJS js expr |> UpdateOptArg.apply args

/// Exclude fields from the result
let without (columns : string seq) (expr : ReqlExpr) =
    expr.Without (Array.ofSeq columns)

/// Merge the right-hand fields into the left-hand document of a sequence
let zip (expr : ReqlExpr) =
    expr.Zip ()

// ~~ RETRY ~~

open RethinkDb.Driver.Net

/// Retry, delaying for each the seconds provided (if required)
let withRetry<'T> intervals f =
    Retry.withRetry<'T> f intervals

/// Convert an async function to a task function (Polly does not understand F# Async)
let private asyncFuncToTask<'T> (f : IConnection -> Async<'T>) =
    fun conn -> f conn |> Async.StartAsTask

/// Retry, delaying for each the seconds provided (if required)
let withAsyncRetry<'T> intervals f = fun conn ->
    withRetry<'T> intervals (asyncFuncToTask f) conn |> Async.AwaitTask

/// Retry, delaying for each the seconds provided (if required)
let withSyncRetry<'T> intervals f =
    Retry.withRetrySync<'T> f intervals

/// Retry failed commands with 200ms, 500ms, and 1 second delays
let withRetryDefault<'T> f =
    Retry.withRetryDefault<'T> f

/// Retry, delaying for each the seconds provided (if required)
let withAsyncRetryDefault<'T> f = fun conn ->
    withRetryDefault<'T> (asyncFuncToTask f) conn |> Async.AwaitTask

/// Retry, delaying for each the seconds provided (if required)
let withSyncRetryDefault<'T> f =
    Retry.withRetrySyncDefault<'T> f

/// Retry failed commands one time with no delay
let withRetryOnce<'T> f =
    Retry.withRetryOnce<'T> f

/// Retry, delaying for each the seconds provided (if required)
let withAsyncRetryOnce<'T> f = fun conn ->
    withRetryOnce<'T> (asyncFuncToTask f) conn |> Async.AwaitTask

/// Retry, delaying for each the seconds provided (if required)
let withSyncRetryOnce<'T> f =
    Retry.withRetrySyncOnce<'T> f
