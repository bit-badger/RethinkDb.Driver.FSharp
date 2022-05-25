# RethinkDb.Driver.FSharp
Idiomatic F# extensions for the C# RethinkDB driver

[![Nuget (with prereleases)][pkg-img]][pkg-url]

## Using

Install the [NuGet][pkg-url] package `RethinkDb.Driver.FSharp`. You will need to specify pre-release, as the package currently has a beta designation.

## What It Provides

The full documentation is on [the project site][project]; TL;DR below.

### A composable pipeline for creating ReQL statements

```fsharp
open RethinkDb.Driver.FSharp.Functions

/// string -> (IConnection -> Task<Post option>)
let fetchPost (postId : string) =
    db "Blog"
    |> table "Post"
    |> get postId
    |> runResult<Post>
    |> asOption
    |> withRetryDefault
```

### An F# domain-specific language (DSL) using a `rethink` computation expression (CE)

```fsharp
open RethinkDb.Driver.FSharp

/// string -> (IConnection -> Task<Post>)
let fetchPost (postId : string) =
    rethink<Post> {
        withTable "Blog.Post"
        get postId
        result
        withRetryDefault
    }
```

### A standard way to translate JSON into a strongly-typed configuration

```fsharp
/// type: DataConfig
let config = DataConfig.fromJsonFile "data-config.json"
// OR
let config = DataConfig.fromConfiguration (config.GetSection "RethinkDB")

/// type: IConnection
let conn = config.Connect ()

/// type: Post (utilizing either example above)
// (within a task CE)
let! post = fetchPost "the-post-id" conn
```

### Robust queries

The RethinkDB connection is generally stored as a singleton. Over time, this connection can lose its connection to the server. Both the CE and functions have `withRetryDefault`, which will retry a failed command up to 3 times (4 counting the initial try), waiting 200ms, 500ms, and 1 second between the respective attempts. There are other options as well; `withRetryOnce` will retry one time immediately. `withRetry` takes a list of `float`s, which will be interpreted as seconds to delay between each retry; it will retry until it has exhausted the delays.

The examples above both use the default retry logic.

### Only rename functions/methods where required

Within the CE, there are a few differing names, mostly notably at the start (selecting databases and tables); this is to allow for a more natural language flow. Also, while CEs now support overloading (thank you F# 6 developers!), they do not detect if the first value in the tupled arguments is different. This is most noticeable once `result*` or `write*` commands have been issued; these support `Task<'T>`, `Async<'T>`, and synchronous `'T` operations, but the follow-on commands will be different (e.x. `withRetryDefault` (tasks) vs. `withAsyncRetryDefault` vs. `withSyncRetryDefault`). There are also versions of these that support optional arguments (for all) and cancellation tokens (for task/async).

The functions show this pattern throughout, as functions in a module do not support overloading; an example for `filter` is below.

```fsharp
// Function names cannot be polymorphic the way object-oriented methods can, so filter's three overloads become
filter (r.HashMap ("age", 30))
// and
filterFunc (fun row -> row.G("age").Eq 30)
// and
filterJS "function (row) { return 30 == row['age'] }"
```

Functions that support optional arguments end with `WithOptArgs`; those that support cancellation tokens end with `WithCancel`; and, those that support both end with `WithOptArgsAndCancel`.

## Licensing

While no specific additional license restrictions exist for this project, there are modifications to the Apache v2
license on this project's dependencies. Please see [the heading on the C# driver page][license] for details.

---

If you are using the project, feel free to file issues about your pain points; there is no substitute for real-world feedback!

[pkg-img]: https://img.shields.io/nuget/vpre/RethinkDb.Driver.FSharp
[pkg-url]: https://www.nuget.org/packages/RethinkDb.Driver.FSharp/
[project]: https://bitbadger.solutions/open-source/rethinkdb-driver-fsharp/
[license]: https://github.com/bchavez/RethinkDb.Driver#open-source-and-commercial-licensing
