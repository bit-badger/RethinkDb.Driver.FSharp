[<AutoOpen>]
module RethinkDb.Driver.FSharp.Functions

open RethinkDb.Driver
open RethinkDb.Driver.Ast

let private r = RethinkDB.R


/// Get a cursor with the results of an expression
let asyncCursor<'T> conn (expr : ReqlExpr) =
  expr.RunCursorAsync<'T> conn
  |> Async.AwaitTask

/// Get the result of a non-select ReQL expression
let asyncReqlResult conn (expr : ReqlExpr) =
  expr.RunResultAsync conn
  |> Async.AwaitTask

/// Get the results of an expression
let asyncResult<'T> conn (expr : ReqlExpr) =
  expr.RunResultAsync<'T> conn
  |> Async.AwaitTask

/// Get documents between a lower bound and an upper bound based on a primary key
let between lowerKey upperKey (expr : ReqlExpr) =
  expr.Between (lowerKey, upperKey)

/// Get document between a lower bound and an upper bound, specifying one or more optional arguments
let betweenWithOptArgs lowerKey upperKey (args : (string * _) seq) (expr : ReqlExpr) =
  args
  |> List.ofSeq
  |> List.fold (fun (btw : Between) arg -> btw.OptArg (fst arg, snd arg)) (between lowerKey upperKey expr)

/// Get documents between a lower bound and an upper bound based on an index
let betweenIndex lowerKey upperKey (index : string) (expr : ReqlExpr) =
  betweenWithOptArgs lowerKey upperKey [ "index", index ] expr

/// Get a connection builder that can be used to create one RethinkDB connection
let connection () =
  r.Connection ()

/// Reference a database
let db dbName =
  r.Db dbName

/// Create a database
let dbCreate dbName conn =
  r.DbCreate dbName
  |> asyncReqlResult conn

/// Drop a database
let dbDrop dbName conn =
  r.DbDrop dbName
  |> asyncReqlResult conn

/// Get a list of databases
let dbList conn =
  r.DbList ()
  |> asyncResult<string list> conn

/// Delete documents
let delete (expr : ReqlExpr) =
  expr.Delete ()

/// Delete documents, providing optional arguments
let deleteWithOptArgs args expr =
  args
  |> List.ofSeq
  |> List.fold (fun (del : Delete) arg -> del.OptArg (fst arg, snd arg)) (delete expr)

/// EqJoin the left field on the right-hand table using its primary key
let eqJoin field (table : Table) (expr : ReqlExpr) =
  expr.EqJoin (field :> obj, table)

/// EqJoin the left function on the right-hand table using its primary key
let eqJoinFunc (f : ReqlExpr -> 'T) (table : Table) (expr : ReqlExpr) =
  expr.EqJoin (ReqlFunction1 (fun row -> upcast f row), table)

/// EqJoin the left function on the right-hand table using the specified index
let eqJoinFuncIndex f table (indexName : string) expr =
  (eqJoinFunc f table expr).OptArg ("index", indexName)

/// EqJoin the left field on the right-hand table using the specified index
let eqJoinIndex field table (indexName : string) expr =
  (eqJoin field table expr).OptArg ("index", indexName)

/// EqJoin the left JavaScript on the right-hand table using its primary key
let eqJoinJS (jsString : string) (table : Table) (expr : ReqlExpr) =
  expr.EqJoin (Javascript (jsString :> obj), table)

/// EqJoin the left JavaScript on the right-hand table using the specified index
let eqJoinJSIndex jsString table (indexName : string) expr =
  (eqJoinJS jsString table expr).OptArg ("index", indexName)

/// Filter documents
let filter filterSpec (expr : ReqlExpr) =
  expr.Filter (filterSpec :> obj)
 
/// Filter documents, providing optional arguments
let filterWithOptArgs filterSpec args expr =
  args
  |> List.ofSeq
  |> List.fold (fun (fil : Filter) arg -> fil.OptArg (fst arg, snd arg)) (filter filterSpec expr)

/// Filter documents using a function
let filterFunc (f : ReqlExpr -> 'T) (expr : ReqlExpr) =
  expr.Filter (ReqlFunction1 (fun row -> upcast f row))

/// Filter documents using a function, providing optional arguments
let filterFuncWithOptArgs f args expr =
  args
  |> List.ofSeq
  |> List.fold (fun (fil : Filter) arg -> fil.OptArg (fst arg, snd arg)) (filterFunc f expr)

/// Filter documents using JavaScript
let filterJS jsString (expr : ReqlExpr) =
  expr.Filter (Javascript (jsString :> obj))

/// Filter documents using JavaScript, providing optional arguments
let filterJSWithOptArgs jsString args expr =
  args
  |> List.ofSeq
  |> List.fold (fun (fil : Filter) arg -> fil.OptArg (fst arg, snd arg)) (filterJS jsString expr)

/// Get a document by its primary key
let get documentId (table : Table) =
  table.Get documentId

/// Get all documents matching keys in the given index
let getAll (ids : 'T seq) indexName (table : Table) =
  table.GetAll(Array.ofSeq ids).OptArg ("index", indexName)

/// Create an index on the given table
let indexCreate indexName conn (table : Table) =
  table.IndexCreate indexName
  |> asyncReqlResult conn

/// Create an index on the given table using a function
let indexCreateFunc indexName (f : ReqlExpr -> 'T) conn (table : Table) =
  table.IndexCreate (indexName, ReqlFunction1 (fun row -> upcast f row))
  |> asyncReqlResult conn

/// Create an index on the given table using JavaScript
let indexCreateJS indexName jsString conn (table : Table) =
  table.IndexCreate (indexName, Javascript (jsString :> obj))
  |> asyncReqlResult conn

/// Drop an index
let indexDrop indexName conn (table : Table) =
  table.IndexDrop indexName
  |> asyncReqlResult conn

/// Get a list of indexes for the given table
let indexList conn (table : Table) =
  table.IndexList ()
  |> asyncResult<string list> conn

/// Rename an index (overwrite will fail)
let indexRename oldName newName conn (table : Table) =
  table.IndexRename (oldName, newName)
  |> asyncReqlResult conn

/// Rename an index (overwrite will succeed)
let indexRenameWithOverwrite oldName newName conn (table : Table) =
  table.IndexRename(oldName, newName).OptArg ("overwrite", true)
  |> asyncReqlResult conn

/// Create an inner join between two sequences, specifying the join condition with a function
let innerJoinFunc otherSeq (f : ReqlExpr -> ReqlExpr -> 'T) (expr : ReqlExpr) =
  expr.InnerJoin (otherSeq, ReqlFunction2 (fun leftRow rightRow -> upcast f leftRow rightRow))

/// Create an inner join between two sequences, specifying the join condition with JavaScript
let innerJoinJS otherSeq jsString (expr : ReqlExpr) =
  expr.InnerJoin (otherSeq, Javascript (jsString :> obj))

/// Insert a single document (use insertMany for multiple)
let insert doc (table : Table) =
  table.Insert doc

/// Insert multiple documents
let insertMany docs (table : Table) =
  table.Insert <| Array.ofSeq docs

/// Insert a single document, providing optional arguments (use insertManyWithOptArgs for multiple)
let insertWithOptArgs doc (args : (string * _) seq) (table : Table) =
  args
  |> List.ofSeq
  |> List.fold (fun (ins : Insert) arg -> ins.OptArg (fst arg, snd arg)) (insert doc table)

/// Insert multiple documents, providing optional arguments
let insertManyWithOptArgs docs (args : (string * _) seq) (table : Table) =
  args
  |> List.ofSeq
  |> List.fold (fun (ins : Insert) arg -> ins.OptArg (fst arg, snd arg)) (insertMany docs table)

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
let outerJoinFunc otherSeq (f : ReqlExpr -> ReqlExpr -> 'T) (expr : ReqlExpr) =
  expr.OuterJoin (otherSeq, ReqlFunction2 (fun leftRow rightRow -> upcast f leftRow rightRow))

/// Create an outer join between two sequences, specifying the join condition with JavaScript
let outerJoinJS otherSeq jsString (expr : ReqlExpr) =
  expr.OuterJoin (otherSeq, Javascript (jsString :> obj))

/// Select one or more attributes from an object or sequence
let pluck (fields : string seq) (expr : ReqlExpr) =
  expr.Pluck (Array.ofSeq fields)

/// Replace documents
let replace replaceSpec (expr : ReqlExpr) =
  expr.Replace (replaceSpec :> obj)

/// Replace documents, providing optional arguments
let replaceWithOptArgs replaceSpec args expr =
  args
  |> List.ofSeq
  |> List.fold (fun (rep : Replace) arg -> rep.OptArg (fst arg, snd arg)) (replace replaceSpec expr)

/// Replace documents using a function
let replaceFunc (f : ReqlExpr -> 'T) (expr : ReqlExpr) =
  expr.Replace (ReqlFunction1 (fun row -> upcast f row))

/// Replace documents using a function, providing optional arguments
let replaceFuncWithOptArgs f args expr =
  args
  |> List.ofSeq
  |> List.fold (fun (rep : Replace) arg -> rep.OptArg (fst arg, snd arg)) (replaceFunc f expr)

/// Replace documents using JavaScript
let replaceJS jsString (expr : ReqlExpr) =
  expr.Replace (Ast.Javascript (jsString :> obj))

/// Replace documents using JavaScript, providing optional arguments
let replaceJSWithOptArgs jsString args expr =
  args
  |> List.ofSeq
  |> List.fold (fun (rep : Replace) arg -> rep.OptArg (fst arg, snd arg)) (replaceJS jsString expr)

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
let tableCreate tableName conn (db : Db) =
  db.TableCreate tableName
  |> asyncReqlResult conn

/// Create a table in the connection-default database
let tableCreateInDefault tableName conn =
  r.TableCreate tableName
  |> asyncReqlResult conn

/// Drop a table in the given database
let tableDrop tableName conn (db : Db) =
  db.TableDrop tableName
  |> asyncReqlResult conn

/// Drop a table from the connection-default database
let tableDropFromDefault tableName conn =
  r.TableDrop tableName
  |> asyncReqlResult conn

/// Get a list of tables for the given database
let tableList conn (db : Db) =
  db.TableList ()
  |> asyncResult<string list> conn

/// Get a list of tables from the connection-default database
let tableListFromDefault conn =
  r.TableList ()
  |> asyncResult<string list> conn

/// Update documents
let update updateSpec (expr : ReqlExpr) =
  expr.Update (updateSpec :> obj)

/// Update documents, providing optional arguments
let updateWithOptArgs updateSpec args expr =
  args
  |> List.ofSeq
  |> List.fold (fun (upd : Update) arg -> upd.OptArg (fst arg, snd arg)) (update updateSpec expr)

/// Update documents using a function
let updateFunc (f : ReqlExpr -> 'T) (expr : ReqlExpr) =
  expr.Update (ReqlFunction1 (fun row -> upcast f row))

/// Update documents using a function, providing optional arguments
let updateFuncWithOptArgs f args expr =
  args
  |> List.ofSeq
  |> List.fold (fun (upd : Update) arg -> upd.OptArg (fst arg, snd arg)) (updateFunc f expr)

/// Update documents using JavaScript
let updateJS jsString (expr : ReqlExpr) =
  expr.Update (Ast.Javascript (jsString :> obj))

/// Update documents using JavaScript, providing optional arguments
let updateJSWithOptArgs jsString args expr =
  args
  |> List.ofSeq
  |> List.fold (fun (upd : Update) arg -> upd.OptArg (fst arg, snd arg)) (updateJS jsString expr)

/// Exclude fields from the result
let without (columns : string seq) (expr : ReqlExpr) =
  expr.Without (Array.ofSeq columns)

/// Merge the right-hand fields into the left-hand document of a sequence
let zip (expr : ReqlExpr) =
  expr.Zip ()
