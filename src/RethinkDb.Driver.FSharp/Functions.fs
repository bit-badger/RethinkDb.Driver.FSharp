[<AutoOpen>]
module RethinkDb.Driver.FSharp.Functions

open RethinkDb.Driver
open RethinkDb.Driver.Ast

[<AutoOpen>]
module private Helpers =
    /// Shorthand for the starting point for ReQL commands
    let r = RethinkDB.R

    /// Create a Javascript object from a string (used mostly for type inference)
    let toJS (js : string) = Javascript js


/// Get a cursor with the results of an expression
let asyncCursor<'T> conn (expr : ReqlExpr) =
    expr.RunCursorAsync<'T> conn
    |> Async.AwaitTask

/// Get the result of a non-select ReQL expression
let asyncReqlResult conn (expr : ReqlExpr) =
    expr.RunWriteAsync conn
    |> Async.AwaitTask

/// Write a ReQL command, always returning a result
let runWriteResult (expr : ReqlExpr) =
    expr.RunWriteAsync

/// Write a ReQL command, raising an exception if an error occurs
let runWrite (expr : ReqlExpr) = fun conn -> backgroundTask {
    let! result = expr.RunWriteAsync conn
    if result.Errors > 0UL then raise <| ReqlRuntimeError result.FirstError
    return result
}
  
/// Get the results of an expression
let asyncResult<'T> conn (expr : ReqlExpr) =
    expr.RunResultAsync<'T> conn
    |> Async.AwaitTask

/// Run the ReQL command, returning the result as the type specified
let runResult<'T> (expr : ReqlExpr) = expr.RunResultAsync<'T>

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

/// Apply optional argument to a filter
let private optArgFilter arg (filter : Filter) =
    filter.OptArg (match arg with Default d -> d.reql)
  
/// Filter documents, providing optional arguments
let filterWithOptArgs (filterSpec : obj) arg expr =
    filter filterSpec expr |> optArgFilter arg

/// Filter documents using a function
let filterFunc<'T> (f : ReqlExpr -> 'T) (expr : ReqlExpr) =
    expr.Filter (ReqlFunction1 (fun row -> f row :> obj))

/// Filter documents using a function, providing optional arguments
let filterFuncWithOptArgs<'T> (f : ReqlExpr -> 'T) arg expr =
    filterFunc f expr |> optArgFilter arg

/// Filter documents using JavaScript
let filterJS js (expr : ReqlExpr) =
    expr.Filter (toJS js)

/// Filter documents using JavaScript, providing optional arguments
let filterJSWithOptArgs js arg expr =
    filterJS js expr |> optArgFilter arg

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
let indexCreateFunc<'T> (indexName : string) (f : ReqlExpr -> 'T) (table : Table) =
    table.IndexCreate (indexName, ReqlFunction1 (fun row -> f row :> obj))

/// Create an index on the given table using a function, including optional arguments
let indexCreateFuncWithOptArgs<'T> indexName (f : ReqlExpr -> 'T) args table =
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

/// Rename an index (overwrite will succeed)
let indexRenameWithOverwrite (oldName : string) (newName : string) (table : Table) =
    indexRename oldName newName table |> IndexRenameOptArg.apply Overwrite

/// Get the status of specific indexes for the given table
let indexStatus (table : Table) (indexes : string list) =
    table.IndexStatus (Array.ofList indexes)

/// Get the status of all indexes for the given table
let indexStatusAll (table : Table) =
    table.IndexStatus ()

/// Wait for specific indexes on the given table to become ready
let indexWait (table : Table) (indexes : string list) =
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
let insert<'T> (doc : 'T) (table : Table) =
    table.Insert doc

/// Insert multiple documents
let insertMany<'T> (docs : 'T seq) (table : Table) =
    table.Insert (Array.ofSeq docs)

/// Insert a single document, providing optional arguments (use insertManyWithOptArgs for multiple)
let insertWithOptArgs<'T> (doc : 'T) args table =
    insert doc table |> InsertOptArg.apply args

/// Insert multiple documents, providing optional arguments
let insertManyWithOptArgs<'T> (docs : 'T seq) args table =
    insertMany docs table |> InsertOptArg.apply args

/// Test whether a sequence is empty
let isEmpty (expr : ReqlExpr) =
    expr.IsEmpty ()

/// End a sequence after a given number of elements
let limit n (expr : ReqlExpr) =
    expr.Limit n

/// Retrieve the nth element in a sequence
let nth n (expr : ReqlExpr) =
    expr.Nth n

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
let replace<'T> (replaceSpec : 'T) (expr : ReqlExpr) =
    expr.Replace replaceSpec

/// Replace documents, providing optional arguments
let replaceWithOptArgs<'T> (replaceSpec : 'T) args expr =
    replace replaceSpec expr |> ReplaceOptArg.apply args

/// Replace documents using a function
let replaceFunc<'T> (f : ReqlExpr -> 'T) (expr : ReqlExpr) =
    expr.Replace (ReqlFunction1 (fun row -> f row :> obj))

/// Replace documents using a function, providing optional arguments
let replaceFuncWithOptArgs<'T> (f : ReqlExpr -> 'T) args expr =
    replaceFunc f expr |> ReplaceOptArg.apply args

/// Replace documents using JavaScript
let replaceJS js (expr : ReqlExpr) =
    expr.Replace (toJS js)

/// Replace documents using JavaScript, providing optional arguments
let replaceJSWithOptArgs js args expr =
    replaceJS js expr |> ReplaceOptArg.apply args

/// Skip a number of elements from the head of a sequence
let skip n (expr : ReqlExpr) =
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
let update<'T> (updateSpec : 'T) (expr : ReqlExpr) =
    expr.Update updateSpec

/// Update documents, providing optional arguments
let updateWithOptArgs<'T> (updateSpec : 'T) args expr =
    update updateSpec expr |> UpdateOptArg.apply args

/// Update documents using a function
let updateFunc<'T> (f : ReqlExpr -> 'T) (expr : ReqlExpr) =
    expr.Update (ReqlFunction1 (fun row -> f row :> obj))

/// Update documents using a function, providing optional arguments
let updateFuncWithOptArgs<'T> (f : ReqlExpr -> 'T) args expr =
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
open System.Threading.Tasks

/// Retry, delaying for each the seconds provided (if required)
let withRetry intervals (f : IConnection -> Task<'T>) =
    Retry.withRetry f intervals

/// Retry failed commands with 200ms, 500ms, and 1 second delays
let withRetryDefault (f : IConnection -> Task<'T>) =
    Retry.withRetryDefault f

/// Retry failed commands one time with no delay
let withRetryOnce (f : IConnection -> Task<'T>) =
    Retry.withRetryOnce f
