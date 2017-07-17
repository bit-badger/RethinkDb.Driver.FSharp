# RethinkDb.Driver.FSharp
Idiomatic F# extensions for the C# RethinkDB driver

## Licensing

While no specific additional license restrictions exist for this project, there are modifications to the Apache v2
license on this project's dependencies. Please see [the heading on the C# driver page][license] for details.

## Using

It is still early days on this project; however, AppVeyor CI provides a [NuGet feed][nuget] that builds packages for
each commit.

## Goals

The goal is to provide:
- A composable pipeline for creating ReQL statements:

```fsharp
/// string -> (IConnection -> Async<Post>)
let fetchPost (postId : string) =
  fromDb "Blog"
  |> table "Post"
  |> get postId
  |> asyncResult<Post>
```

- An F# domain-specific language (DSL) using a `rethink` computation expression:

```fsharp
/// string -> (IConnection -> Async<Post>)
let fetchPost (postId : string) =
  rethink {
    fromDb "Blog"
    table "Post"
    get postId
    asyncResult<Post>
    }
```

- A standard way to translate JSON into a strongly-typed configuration:

```fsharp
/// type: DataConfig
let config = DataConfig.fromJsonFile "data-config.json"

/// type: IConnection
let conn = config.Connect ()

/// type: Post (utilizing either example above)
let post = fetchPost "the-post-id" conn |> Async.RunSynchronously
```

- Only rename functions/methods where required

```fsharp
// Function names cannot be polymorphic the way object-oriented methods can, so filter's three overloads become
filter r.HashMap("age", 30)
// and
filterFunc (fun row -> row.["age"].Eq(30))
// and
filterJS "function (row) { return 30 == row['age'] }"
```

The composable pipeline and the JSON configuration are the early goals, as the computation expression will utilize the
same composition as those functions.

[license]: https://github.com/bchavez/RethinkDb.Driver#open-source-and-commercial-licensing
[nuget]: https://ci.appveyor.com/nuget/danieljsummers-rethinkdb-driver-fsharp