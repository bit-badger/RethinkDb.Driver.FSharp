## RethinkDb.Driver.FSharp

This package provides idiomatic F# extensions on the [official C# driver][csharp-pkg]. Within this package:

### Connection Configuration / Creation

```fsharp
open RethinkDb.Driver.FSharp

let dataCfg = DataConfig.fromJson "rethink-config.json"
// - or -
let dataCfg = DataConfig.fromConfiguration [config-section]
// - or -
let dataCfg = DataConfig.fromUri [connection-string]

let conn = dataCfg.CreateConnection ()  // IConnection
```

### Domain-Specific Language (DSL) / Computation Expression (CE) Style

```fsharp
open RethinkDb.Driver.FSharp

// Remove the conn parameter and usage for point-free style

let getPost postId conn =
    rethink<Post> {
        withTable "Post"
        get postId
        resultOption
        withRetryOptionDefault conn
    }

let updatePost post conn =
    rethink {
        withTable "Post"
        get post.id
        update post
        write
        ignoreResult
        withRetryDefault conn
    }
```

### Function Style

```fsharp
open RethinkDb.Driver.FSharp.Functions

// Remove the conn parameter and usage for point-free style

let getPost postId conn =
    fromTable "Post"
    |> get postId
    |> runResult<Post>
    |> asOption
    |> withRetryDefault
    |> withConn conn

let updatePost post conn =
    fromTable "Post"
    |> get post.id
    |> update post
    |> runWrite
    |> ignoreResult
    |> withRetryDefault
    |> withConn conn
```

### Retry Logic

The driver does not reconnect automatically when the underlying connection has been interrupted. When specified, the retry logic attempts to reconnect; default retries wait 200ms, 500ms, and 1 second. There are also functions to retry once, and those that allow the intervals to be specified.

### Strongly-Typed Optional Arguments

Many RethinkDB commands support optional arguments to tweak the behavior of that command. A quick example using the `between` command (clause):

```fsharp
// ...
    between 1 100 [ LowerBound Open; UpperBound Closed ]
// ...
```
---

More information is available on the [project site][].


[csharp-pkg]: https://www.nuget.org/packages/RethinkDb.Driver/
[project site]: https://bitbadger.solutions/open-source/rethinkdb-driver-fsharp/
