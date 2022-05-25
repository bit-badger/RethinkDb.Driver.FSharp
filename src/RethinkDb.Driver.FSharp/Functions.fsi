/// The function-based Domain-Specific Language (DSL) for RethinkDB
module RethinkDb.Driver.FSharp.Functions

open System.Threading
open System.Threading.Tasks
open RethinkDb.Driver
open RethinkDb.Driver.Ast
open RethinkDb.Driver.Net

/// Get a cursor with the results of an expression
val asyncCursor<'T> : ReqlExpr -> IConnection -> Async<Cursor<'T>>

// ~~ WRITE, ALWAYS RETURNING A RESULT ~~

/// Write a ReQL command with a cancellation token, always returning a result
val runWriteResultWithCancel : CancellationToken -> ReqlExpr -> (IConnection -> Task<Model.Result>)

/// Write a ReQL command, always returning a result
val runWriteResult : ReqlExpr -> (IConnection -> Task<Model.Result>)

/// Write a ReQL command with optional arguments and a cancellation token, always returning a result
val runWriteResultWithOptArgsAndCancel :
        RunOptArg list -> CancellationToken -> ReqlExpr -> (IConnection -> Task<Model.Result>)

/// Write a ReQL command with optional arguments, always returning a result
val runWriteResultWithOptArgs : RunOptArg list -> ReqlExpr -> (IConnection -> Task<Model.Result>)

/// Write a ReQL command, always returning a result
val asyncWriteResult : ReqlExpr -> (IConnection -> Async<Model.Result>)

/// Write a ReQL command with optional arguments, always returning a result
val asyncWriteResultWithOptArgs : RunOptArg list -> ReqlExpr -> (IConnection -> Async<Model.Result>)

/// Write a ReQL command with a cancellation token, always returning a result
val asyncWriteResultWithCancel : CancellationToken -> ReqlExpr -> (IConnection -> Async<Model.Result>)

/// Write a ReQL command with optional arguments and a cancellation token, always returning a result
val asyncWriteResultWithOptArgsAndCancel :
        RunOptArg list -> CancellationToken -> ReqlExpr -> (IConnection -> Async<Model.Result>)

/// Write a ReQL command synchronously, always returning a result
val syncWriteResult : ReqlExpr -> (IConnection -> Model.Result)

/// Write a ReQL command synchronously with optional arguments, always returning a result
val syncWriteResultWithOptArgs : RunOptArg list -> ReqlExpr -> (IConnection -> Model.Result)

// ~~ WRITE, RAISING AN EXCEPTION ON ERRORS ~~
    
/// Write a ReQL command, raising an exception if an error occurs
val runWriteWithCancel : CancellationToken -> ReqlExpr -> (IConnection -> Task<Model.Result>)

/// Write a ReQL command, raising an exception if an error occurs
val runWrite : ReqlExpr -> (IConnection -> Task<Model.Result>)

/// Write a ReQL command with optional arguments, raising an exception if an error occurs
val runWriteWithOptArgsAndCancel :
        RunOptArg list -> CancellationToken -> ReqlExpr -> (IConnection -> Task<Model.Result>)

/// Write a ReQL command with optional arguments, raising an exception if an error occurs
val runWriteWithOptArgs : RunOptArg list -> ReqlExpr -> (IConnection -> Task<Model.Result>)

/// Write a ReQL command, raising an exception if an error occurs
val asyncWrite : ReqlExpr -> (IConnection -> Async<Model.Result>)

/// Write a ReQL command with optional arguments, raising an exception if an error occurs
val asyncWriteWithOptArgs : RunOptArg list -> ReqlExpr -> (IConnection -> Async<Model.Result>)

/// Write a ReQL command, raising an exception if an error occurs
val asyncWriteWithCancel : CancellationToken -> ReqlExpr -> (IConnection -> Async<Model.Result>)

/// Write a ReQL command with optional arguments, raising an exception if an error occurs
val asyncWriteWithOptArgsAndCancel :
        RunOptArg list -> CancellationToken -> ReqlExpr -> (IConnection -> Async<Model.Result>)

/// Write a ReQL command synchronously, raising an exception if an error occurs
val syncWrite : ReqlExpr -> (IConnection -> Model.Result)

/// Write a ReQL command synchronously with optional arguments, raising an exception if an error occurs
val syncWriteWithOptArgs : RunOptArg list -> ReqlExpr -> (IConnection -> Model.Result)

// ~~ RUNNING QUERIES AND MANIPULATING RESULTS ~~

/// Run the ReQL command using a cancellation token, returning the result as the type specified
val runResultWithCancel<'T> : CancellationToken -> ReqlExpr -> (IConnection -> Task<'T>)

/// Run the ReQL command using optional arguments and a cancellation token, returning the result as the type specified
val runResultWithOptArgsAndCancel<'T> : RunOptArg list -> CancellationToken -> ReqlExpr -> (IConnection -> Task<'T>)

/// Run the ReQL command, returning the result as the type specified
val runResult<'T> : ReqlExpr -> (IConnection -> Task<'T>)

/// Run the ReQL command using optional arguments, returning the result as the type specified
val runResultWithOptArgs<'T> : RunOptArg list -> ReqlExpr -> (IConnection -> Task<'T>)

/// Run the ReQL command, returning the result as the type specified
val asyncResult<'T> : ReqlExpr -> (IConnection -> Async<'T>)

/// Run the ReQL command using optional arguments, returning the result as the type specified
val asyncResultWithOptArgs<'T> : RunOptArg list -> ReqlExpr -> (IConnection -> Async<'T>)

/// Run the ReQL command using a cancellation token, returning the result as the type specified
val asyncResultWithCancel<'T> : CancellationToken -> ReqlExpr -> (IConnection -> Async<'T>)

/// Run the ReQL command using optional arguments and a cancellation token, returning the result as the type specified
val asyncResultWithOptArgsAndCancel<'T> : RunOptArg list -> CancellationToken -> ReqlExpr -> (IConnection -> Async<'T>)

/// Run the ReQL command, returning the result as the type specified
val syncResult<'T> : ReqlExpr -> (IConnection -> 'T)

/// Run the ReQL command using optional arguments, returning the result as the type specified
val syncResultWithOptArgs<'T> : RunOptArg list -> ReqlExpr -> (IConnection -> 'T)

/// Convert a possibly-null result to an option
val asOption : (IConnection -> Task<'T>) -> IConnection -> Task<'T option>

/// Convert a possibly-null result to an option
val asAsyncOption : (IConnection -> Async<'T>) -> IConnection -> Async<'T option>

/// Convert a possibly-null result to an option
val asSyncOption : (IConnection -> 'T) -> IConnection -> 'T option

/// Ignore the result of a task-based execution
val ignoreResult<'T> : (IConnection -> Task<'T>) -> IConnection -> Task<unit>

/// Apply a connection to the query pipeline (typically the final step)
val withConn<'T> : IConnection -> (IConnection -> 'T) -> 'T

// ~~ REQL QUERY DEFINITION ~~

/// Get documents between a lower bound and an upper bound based on a primary key
val between : obj -> obj -> ReqlExpr -> Between

/// Get document between a lower bound and an upper bound, specifying one or more optional arguments
val betweenWithOptArgs : obj -> obj -> BetweenOptArg list -> ReqlExpr -> Between

/// Get documents between a lower bound and an upper bound based on an index
val betweenIndex : obj -> obj -> string -> ReqlExpr -> Between

/// Get a connection builder that can be used to create one RethinkDB connection
val connection : unit -> Connection.Builder

/// Count the documents in this query
val count : ReqlExpr -> Count
    
/// Count the documents in this query where the function returns true 
val countFunc : (ReqlExpr -> bool) -> ReqlExpr -> Count
    
/// Count the documents in this query where the function returns true 
val countJS : string -> ReqlExpr -> Count
    
/// Reference a database
val db : string -> Db

/// Create a database
val dbCreate : string -> DbCreate

/// Drop a database
val dbDrop : string -> DbDrop

/// Get a list of databases
val dbList : unit -> DbList

/// Delete documents
val delete : ReqlExpr -> Delete

/// Delete documents, providing optional arguments
val deleteWithOptArgs : DeleteOptArg list -> ReqlExpr -> Delete

/// Only retrieve distinct entries from a selection
val distinct : ReqlExpr -> Distinct

/// Only retrieve distinct entries from a selection, based on an index
val distinctWithIndex : string -> ReqlExpr -> Distinct

/// EqJoin the left field on the right-hand table using its primary key
val eqJoin : string -> Table -> ReqlExpr -> EqJoin

/// EqJoin the left function on the right-hand table using its primary key
val eqJoinFunc<'T> : (ReqlExpr -> 'T) -> Table -> ReqlExpr -> EqJoin

/// EqJoin the left function on the right-hand table using the specified index
val eqJoinFuncIndex<'T> : (ReqlExpr -> 'T) -> Table -> string -> ReqlExpr -> EqJoin

/// EqJoin the left field on the right-hand table using the specified index
val eqJoinIndex : string -> Table -> string -> ReqlExpr -> EqJoin

/// EqJoin the left JavaScript on the right-hand table using its primary key
val eqJoinJS : string -> Table -> ReqlExpr -> EqJoin

/// EqJoin the left JavaScript on the right-hand table using the specified index
val eqJoinJSIndex : string -> Table -> string -> ReqlExpr -> EqJoin

/// Filter documents
val filter : obj -> ReqlExpr -> Filter

/// Filter documents, providing an optional argument
val filterWithOptArg : obj -> FilterOptArg -> ReqlExpr -> Filter

/// Filter documents using a function
val filterFunc : (ReqlExpr -> obj) -> ReqlExpr -> Filter

/// Filter documents using a function, providing an optional argument
val filterFuncWithOptArg : (ReqlExpr -> obj) -> FilterOptArg -> ReqlExpr -> Filter

/// Filter documents using multiple functions (has the effect of ANDing them)
val filterFuncAll : (ReqlExpr -> obj) list -> ReqlExpr -> Filter

/// Filter documents using multiple functions (has the effect of ANDing them), providing an optional argument
val filterFuncAllWithOptArg : (ReqlExpr -> obj) list -> FilterOptArg -> ReqlExpr -> Filter

/// Filter documents using JavaScript
val filterJS : string -> ReqlExpr -> Filter

/// Filter documents using JavaScript, providing an optional argument
val filterJSWithOptArg : string -> FilterOptArg -> ReqlExpr -> Filter

/// Get a document by its primary key
val get : obj -> Table -> Get

/// Get all documents matching primary keys
val getAll : obj seq -> Table -> GetAll

/// Get all documents matching keys in the given index
val getAllWithIndex : obj seq -> string -> Table -> GetAll

/// Create an index on the given table
val indexCreate : string -> Table -> IndexCreate

/// Create an index on the given table, including optional arguments
val indexCreateWithOptArgs : string -> IndexCreateOptArg list -> Table -> IndexCreate

/// Create an index on the given table using a function
val indexCreateFunc : string -> (ReqlExpr -> obj) -> Table -> IndexCreate

/// Create an index on the given table using a function, including optional arguments
val indexCreateFuncWithOptArgs : string -> (ReqlExpr -> obj) -> IndexCreateOptArg list -> Table -> IndexCreate

/// Create an index on the given table using JavaScript
val indexCreateJS : string -> string -> Table -> IndexCreate

/// Create an index on the given table using JavaScript, including optional arguments
val indexCreateJSWithOptArgs : string -> string -> IndexCreateOptArg list -> Table -> IndexCreate

/// Drop an index
val indexDrop : string -> Table -> IndexDrop

/// Get a list of indexes for the given table
val indexList : Table -> IndexList

/// Rename an index (will fail if new name already exists)
val indexRename : string -> string -> Table -> IndexRename

/// Rename an index (specifying overwrite action)
val indexRenameWithOptArg : string -> string -> IndexRenameOptArg -> Table -> IndexRename

/// Get the status of specific indexes for the given table
val indexStatus : string list -> Table -> IndexStatus

/// Get the status of all indexes for the given table
val indexStatusAll : Table -> IndexStatus

/// Wait for specific indexes on the given table to become ready
val indexWait : string list -> Table -> IndexWait

/// Wait for all indexes on the given table to become ready
val indexWaitAll : Table -> IndexWait

/// Create an inner join between two sequences, specifying the join condition with a function
val innerJoinFunc<'T> : obj -> (ReqlExpr -> ReqlExpr -> 'T) -> ReqlExpr -> InnerJoin

/// Create an inner join between two sequences, specifying the join condition with JavaScript
val innerJoinJS : obj -> string -> ReqlExpr -> InnerJoin

/// Insert a single document (use insertMany for multiple)
val insert<'T> : 'T -> Table -> Insert

/// Insert multiple documents
val insertMany<'T> : 'T seq -> Table -> Insert

/// Insert a single document, providing optional arguments (use insertManyWithOptArgs for multiple)
val insertWithOptArgs<'T> : 'T -> InsertOptArg list -> Table -> Insert

/// Insert multiple documents, providing optional arguments
val insertManyWithOptArgs<'T> : 'T seq -> InsertOptArg list -> Table -> Insert

/// Test whether a sequence is empty
val isEmpty : ReqlExpr -> IsEmpty

/// End a sequence after a given number of elements
val limit : int -> ReqlExpr -> Limit

/// Map the results using a function
val mapFunc : (ReqlExpr -> obj) -> ReqlExpr -> Map

/// Map the results using a JavaScript function
val mapJS : string -> ReqlExpr -> Map

/// Merge the current query with given document
val merge : obj -> ReqlExpr -> Merge
    
/// Merge the current query with the results of a function
val mergeFunc : (ReqlExpr -> obj) -> ReqlExpr -> Merge
    
/// Merge the current query with the results of a JavaScript function
val mergeJS : string -> ReqlExpr -> Merge
    
/// Retrieve the nth element in a sequence
val nth : int -> ReqlExpr -> Nth

/// Order a sequence by a given field
val orderBy : string -> ReqlExpr -> OrderBy

/// Order a sequence in descending order by a given field
val orderByDescending : string -> ReqlExpr -> OrderBy
    
/// Order a sequence by a given function
val orderByFunc : (ReqlExpr -> obj) -> ReqlExpr -> OrderBy

/// Order a sequence in descending order by a given function
val orderByFuncDescending : (ReqlExpr -> obj) -> ReqlExpr -> OrderBy

/// Order a sequence by a given index
val orderByIndex : string -> ReqlExpr -> OrderBy

/// Order a sequence in descending order by a given index
val orderByIndexDescending : string -> ReqlExpr -> OrderBy

/// Order a sequence by a given JavaScript function
val orderByJS : string -> ReqlExpr -> OrderBy

/// Order a sequence in descending order by a given JavaScript function
val orderByJSDescending : string -> ReqlExpr -> OrderBy

/// Create an outer join between two sequences, specifying the join condition with a function
val outerJoinFunc<'T> : obj -> (ReqlExpr -> ReqlExpr -> 'T) -> ReqlExpr -> OuterJoin

/// Create an outer join between two sequences, specifying the join condition with JavaScript
val outerJoinJS : obj -> string -> ReqlExpr -> OuterJoin

/// Select one or more attributes from an object or sequence
val pluck : string seq -> ReqlExpr -> Pluck

/// Replace documents
val replace : obj -> ReqlExpr -> Replace

/// Replace documents, providing optional arguments
val replaceWithOptArgs : obj -> ReplaceOptArg list -> ReqlExpr -> Replace

/// Replace documents using a function
val replaceFunc : (ReqlExpr -> obj) -> ReqlExpr -> Replace

/// Replace documents using a function, providing optional arguments
val replaceFuncWithOptArgs : (ReqlExpr -> obj) -> ReplaceOptArg list -> ReqlExpr -> Replace

/// Replace documents using JavaScript
val replaceJS : string -> ReqlExpr -> Replace

/// Replace documents using JavaScript, providing optional arguments
val replaceJSWithOptArgs : string -> ReplaceOptArg list -> ReqlExpr -> Replace

/// Skip a number of elements from the head of a sequence
val skip : int -> ReqlExpr -> Skip

/// Ensure changes to a table are written to permanent storage
val sync : Table -> Sync

/// Return all documents in a table (may be further refined)
val table : string -> Db -> Table

/// Return all documents in a table from the default database (may be further refined)
val fromTable : string -> Table

/// Create a table in the given database
val tableCreate : string -> Db -> TableCreate

/// Create a table in the connection-default database
val tableCreateInDefault : string -> TableCreate

/// Drop a table in the given database
val tableDrop : string -> Db -> TableDrop

/// Drop a table from the connection-default database
val tableDropFromDefault : string -> TableDrop

/// Get a list of tables for the given database
val tableList : Db -> TableList

/// Get a list of tables from the connection-default database
val tableListFromDefault : unit -> TableList

/// Update documents
val update : obj -> ReqlExpr -> Update

/// Update documents, providing optional arguments
val updateWithOptArgs : obj -> UpdateOptArg list -> ReqlExpr -> Update

/// Update documents using a function
val updateFunc : (ReqlExpr -> obj) -> ReqlExpr -> Update

/// Update documents using a function, providing optional arguments
val updateFuncWithOptArgs : (ReqlExpr -> obj) -> UpdateOptArg list -> ReqlExpr -> Update

/// Update documents using JavaScript
val updateJS : string -> ReqlExpr -> Update

/// Update documents using JavaScript, providing optional arguments
val updateJSWithOptArgs : string -> UpdateOptArg list -> ReqlExpr -> Update

/// Exclude fields from the result
val without : string seq -> ReqlExpr -> Without

/// Merge the right-hand fields into the left-hand document of a sequence
val zip : ReqlExpr -> Zip

// ~~ RETRY FUNCTIONS ~~

/// Retry, delaying for each the seconds provided (if required)
val withRetry<'T> : float seq -> (IConnection -> Task<'T>) -> (IConnection -> Task<'T>)

/// Retry, delaying for each the seconds provided (if required)
val withAsyncRetry<'T> : float seq -> (IConnection -> Async<'T>) -> (IConnection -> Async<'T>)

/// Retry, delaying for each the seconds provided (if required)
val withSyncRetry<'T> : float seq -> (IConnection -> 'T) -> (IConnection -> 'T)

/// Retry failed commands with 200ms, 500ms, and 1 second delays
val withRetryDefault<'T> : (IConnection -> Task<'T>) -> (IConnection -> Task<'T>)

/// Retry failed commands with 200ms, 500ms, and 1 second delays
val withAsyncRetryDefault<'T> : (IConnection -> Async<'T>) -> (IConnection -> Async<'T>)

/// Retry failed commands with 200ms, 500ms, and 1 second delays
val withSyncRetryDefault<'T> : (IConnection -> 'T) -> (IConnection -> 'T)

/// Retry failed commands one time with no delay
val withRetryOnce<'T> : (IConnection -> Task<'T>) -> (IConnection -> Task<'T>)

/// Retry failed commands one time with no delay
val withAsyncRetryOnce<'T> : (IConnection -> Async<'T>) -> (IConnection -> Async<'T>)

/// Retry failed commands one time with no delay
val withSyncRetryOnce<'T> : (IConnection -> 'T) -> (IConnection -> 'T)
