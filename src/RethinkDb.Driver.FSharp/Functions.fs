[<AutoOpen>]
module RethinkDb.Driver.FSharp.Functions

open RethinkDb.Driver

let private r = RethinkDB.R

/// Get a connection builder that can be used to create one RethinkDB connection
let connection () =
  r.Connection ()

/// Get the results of an expression
let asyncResult<'T> conn (expr : Ast.ReqlExpr) =
  expr.RunResultAsync<'T> conn
  |> Async.AwaitTask

/// Get the result of a non-select ReQL expression
let asyncReqlResult conn (expr : Ast.ReqlExpr) =
  expr.RunResultAsync conn
  |> Async.AwaitTask

/// Get a list of databases
let dbList conn =
  r.DbList ()
  |> asyncResult<string list> conn

/// Create a database
let dbCreate dbName conn =
  r.DbCreate dbName
  |> asyncReqlResult conn

/// Reference a database
let db dbName =
  r.Db dbName

/// Reference the default database
let defaultDb =
  (fun () -> r.Db ()) ()

/// Get a list of tables for the given database
let tableList conn (db : Ast.Db) =
  db.TableList ()
  |> asyncResult<string list> conn

/// Create a table in the given database
let tableCreate tableName conn (db : Ast.Db) =
  db.TableCreate tableName
  |> asyncReqlResult conn

/// Return all documents in a table (may be further refined)
let table tableName (db : Ast.Db) =
  db.Table tableName

/// Return all documents in a table from the default database (may be further refined)
let fromTable tableName =
  table tableName defaultDb

/// Get a list of indexes for the given table
let indexList conn (table : Ast.Table) =
  table.IndexList ()
  |> asyncResult<string list> conn

/// Create an index on the given table
let indexCreate indexName conn (table : Ast.Table) =
  table.IndexCreate indexName
  |> asyncReqlResult conn

/// Create an index on the given table using a function
let indexCreateFunc indexName (f : Ast.ReqlExpr -> obj) conn (table : Ast.Table) =
  table.IndexCreate (indexName, Ast.ReqlFunction1 f)
  |> asyncReqlResult conn

let indexCreateJS indexName jsString conn (table : Ast.Table) =
  table.IndexCreate (indexName, Ast.Javascript (jsString :> obj))
  |> asyncReqlResult conn

/// Get a document by its primary key
let get documentId (table : Ast.Table) =
  table.Get documentId

/// Get all documents matching keys in the given index
let getAll (ids : 'T seq) indexName (table : Ast.Table) =
  table.GetAll(ids |> Array.ofSeq).OptArg("index", indexName)


/// Get a cursor with the results of an expression
let asyncCursor<'T> conn (expr : Ast.ReqlExpr) =
  expr.RunCursorAsync<'T> conn
  |> Async.AwaitTask