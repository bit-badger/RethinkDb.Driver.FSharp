[<AutoOpen>]
module RethinkDb.Driver.FSharp.RethinkBuilder

open RethinkDb.Driver
open RethinkDb.Driver.Ast
open RethinkDb.Driver.Net
open System.Threading.Tasks

/// Computation Expression builder for RethinkDB queries
type RethinkBuilder<'T> () =
    
    /// Create a RethinkDB hash map of the given field/value pairs
    let fieldsToMap (fields : (string * obj) list) =
        fields
        |> List.fold (fun (m : Model.MapObject) item -> m.With (fst item, snd item)) (RethinkDB.R.HashMap ())
    
    member _.Bind (expr : ReqlExpr, f : ReqlExpr -> ReqlExpr) = f expr
  
    member this.For (expr, f) = this.Bind (expr, f)
    
    member _.Yield _ = RethinkDB.R
    
    // meta queries (tables, indexes, etc.)
    
    /// List all databases
    [<CustomOperation "dbList">]
    member _.DbList (r : RethinkDB) = r.DbList ()
    
    /// Create a database
    [<CustomOperation "dbCreate">]
    member _.DbCreate (r : RethinkDB, db : string) = r.DbCreate db
    
    /// List all tables for the default database
    [<CustomOperation "tableList">]
    member _.TableList (r : RethinkDB) = r.TableList ()
    
    /// List all tables for the specified database
    [<CustomOperation "tableListWithDb">]
    member _.TableListWithDb (r : RethinkDB, db : string) = r.Db(db).TableList ()
    
    /// Create a table in the default database
    [<CustomOperation "tableCreate">]
    member _.TableCreate (r : RethinkDB, table : string) = r.TableCreate table
    
    /// Create a table in the default database
    [<CustomOperation "tableCreateWithDb">]
    member _.TableCreateWithDb (r : RethinkDB, table : string, db : string) = r.Db(db).TableCreate table
    
    /// List all indexes for a table
    [<CustomOperation "indexList">]
    member _.IndexList (tbl : Table) = tbl.IndexList ()
    
    /// Create an index for a table
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl : Table, index : string) = tbl.IndexCreate index
    
    /// Create an index for a table, using a function to calculate the index
    [<CustomOperation "indexCreate">]
    member _.IndexCreate (tbl : Table, index : string, f : ReqlExpr -> obj) = tbl.IndexCreate (index, ReqlFunction1 f)
    
    // database/table identification
    
    /// Specify a database for further commands
    [<CustomOperation "withDb">]
    member _.Db (expr : RethinkDB, db : string) = expr.Db db
    
    /// Specify a table in the default database
    [<CustomOperation "withTable">]
    member _.TableInDefaultDb (expr : RethinkDB, table : string) = expr.Table table
    
    /// Specify a table in a specific database
    [<CustomOperation "withTableInDb">]
    member _.Table (expr : RethinkDB, table : string, db : string) = expr.Db(db).Table table
    
    /// Create an equality join with another table
    [<CustomOperation "eqJoin">]
    member _.EqJoin (expr : ReqlExpr, field : string, otherTable : string) =
        expr.EqJoin (field, RethinkDB.R.Table otherTable)
    
    // data retrieval / manipulation
    
    /// Get a document from a table by its ID
    [<CustomOperation "get">]
    member _.Get (tbl : Table, key : obj) = tbl.Get key
    
    /// Get all documents matching the given index value
    [<CustomOperation "getAll">]
    member _.GetAll (tbl : Table, keys : obj list, index : string) =
        tbl.GetAll(Array.ofList keys).OptArg ("index", index)
    
    /// Limit the results of this query
    [<CustomOperation "limit">]
    member _.Limit (expr : ReqlExpr, limit : int) = expr.Limit limit

    /// Count documents for the current query
    [<CustomOperation "count">]
    member _.Count (expr : ReqlExpr) = expr.Count ()
    
    /// Filter a query by a single field value
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, field : string, value : obj) = expr.Filter (fieldsToMap [ field, value ])
    
    /// Filter a query by multiple field values
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, filter : (string * obj) list) = expr.Filter (fieldsToMap filter)
    
    /// Filter a query by a function
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, f : ReqlExpr -> obj) = expr.Filter (ReqlFunction1 f)
    
    /// Filter a query by multiple functions (has the effect of ANDing them)
    [<CustomOperation "filter">]
    member _.Filter (expr : ReqlExpr, fs : (ReqlExpr -> obj) list) =
        fs |> List.fold (fun (e : ReqlExpr) f -> e.Filter (ReqlFunction1 f)) expr
    
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
    
    /// Update specific fields in a document
    [<CustomOperation "update">]
    member _.Update (expr : ReqlExpr, fields : (string * obj) list) = expr.Update (fieldsToMap fields)
    
    /// Replace the current query with the specified document
    [<CustomOperation "replace">]
    member _.Replace (expr : ReqlExpr, doc : obj) = expr.Replace doc
    
    /// Delete the document(s) identified by the current query
    [<CustomOperation "delete">]
    member _.Delete (expr : ReqlExpr) = expr.Delete ()

    // executing queries
    
    /// Execute the query, returning the result of the type specified
    [<CustomOperation "result">]
    member _.Result (expr : ReqlExpr) : IConnection -> Task<'T> = 
        fun conn -> task {
            return! expr.RunResultAsync<'T> conn
        }
    
    /// Execute the query, returning the result of the type specified, or None if no result is found
    [<CustomOperation "resultOption">]
    member _.ResultOption (expr : ReqlExpr) : IConnection -> Task<'T option> = 
        fun conn -> task {
            let! result = expr.RunResultAsync<'T> conn
            return match (box >> isNull) result with true -> None | false -> Some result
        }
    
    /// Perform a write operation
    [<CustomOperation "write">]
    member _.Write (expr : ReqlExpr) : IConnection -> Task<Model.Result> =
        fun conn -> task {
            return! expr.RunWriteAsync conn
        }

/// RethinkDB computation expression
let rethink<'T> = RethinkBuilder<'T> ()
