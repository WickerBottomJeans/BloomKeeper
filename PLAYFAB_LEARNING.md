# PlayFab Learning Notes

This guide teaches the PlayFab systems needed for a game that uses:

- player login and authentication
- PlayFab Entities
- CloudScript backed by Azure Functions
- Entity Objects or Entity Files
- Economy V2 catalogs and inventories
- idempotent server operations
- optimistic concurrency

It intentionally does not cover unrelated PlayFab products such as multiplayer servers, matchmaking, voice chat, leaderboards, experiments, or analytics.

## 1. The Basic Mental Model

PlayFab is a collection of backend services for games. It is not one database and it is not one API.

The relevant parts are:

| PlayFab system | Responsibility |
|---|---|
| Authentication | Identifies a player and issues credentials |
| Entities | Gives players, titles, groups, and other owners a common identity model |
| CloudScript | Lets an authenticated caller execute trusted server logic |
| Data | Stores Objects or Files owned by an entity |
| Economy V2 Catalog | Defines which virtual items exist |
| Economy V2 Inventory | Stores which item stacks an entity owns |

A common request path looks like this:

```text
Game client
    -> authenticates with PlayFab
    -> receives credentials
    -> calls a permitted PlayFab API
    -> optionally asks CloudScript to execute server logic

PlayFab
    -> authenticates the caller
    -> invokes the registered server function
    -> supplies trusted caller information

Server function
    -> validates the requested action
    -> reads or changes PlayFab data
    -> returns a result
```

The client says what it wants to do. Authentication proves who is asking. Trusted server logic decides whether the action is allowed.

## 2. Title, Player, and Entity

These words describe different parts of PlayFab's identity model.

### Title

A **title** is one game/backend environment in PlayFab.

A title owns its:

- players
- configuration
- catalogs
- inventories
- CloudScript registrations
- events and other live-service data

Every title has a **Title ID**. PlayFab uses it to route an API request to the correct game.

A Title ID is an identifier, not a secret.

### Player account

PlayFab creates a player account when a login method is used with account creation enabled and no linked account already exists.

One PlayFab player may have multiple login methods linked to the same account, such as:

- a custom ID
- device identity
- email and password
- Steam
- Google
- Apple
- Xbox

The login method is how the player proves access. It is not necessarily the player's final PlayFab identity.

### Entity

An **entity** is PlayFab's general representation of something that can own data or participate in APIs.

Examples include:

- `title_player_account`
- `master_player_account`
- `character`
- `group`
- `title`

An entity is identified by an **Entity Key**:

```text
Entity Key = Entity ID + Entity Type
```

The ID identifies one entity within its type. The type tells PlayFab what kind of entity the ID represents.

Therefore, an Entity ID by itself is not always enough for an Entity API request.

Official reference: [Entities quickstart](https://learn.microsoft.com/en-us/gaming/playfab/live-service-management/game-configuration/entities/quickstart)

## 3. Login Methods

The first job of login is to exchange some proof of identity for PlayFab credentials.

### Anonymous login

An anonymous login allows a player to begin without creating a recoverable account.

Examples include device-based login and title-generated Custom ID login.

Advantages:

- almost no friction
- useful for letting a new player start immediately

Risk:

- if the identifying value exists only on one device and is lost, the player may lose access to the account

### Recoverable login

A recoverable login uses an identity the player can authenticate again on another device, such as an external platform account or an email-based account.

A common production pattern is:

1. Let the player begin anonymously.
2. Later link a recoverable login method to the same PlayFab account.
3. Use the linked method to restore access on another device.

Official overview: [Player login](https://learn.microsoft.com/en-us/gaming/playfab/identity/player-identity/login/)

## 4. Custom ID Login

`LoginWithCustomID` is a PlayFab Client API login method.

### Custom ID

A **Custom ID** is generated and owned by the game developer, not by PlayFab.

The game sends:

```text
Title ID
Custom ID
CreateAccount
```

PlayFab uses the Custom ID as an external login identity linked to a PlayFab player account.

If the same Custom ID is sent to the same title again, it can access the same linked PlayFab account.

The Custom ID must be difficult to collide with another player's ID. A random GUID is a normal basis for one.

### `CreateAccount`

When `CreateAccount` is true:

- if the Custom ID is already linked, PlayFab logs into that account
- if it is not linked and player creation is allowed, PlayFab creates and links a new account

When `CreateAccount` is false, PlayFab rejects an unknown Custom ID instead of creating an account.

For production titles, anonymous player-creation policy must be considered separately from this request field. New titles may restrict client-side anonymous account creation.

Official API reference: [Login With Custom ID](https://learn.microsoft.com/en-us/rest/api/playfab/client/authentication/login-with-custom-id?view=playfab-rest)

## 5. Values Returned by Login

A successful login can return several values. They are not all interchangeable and they are not all needed by every process.

### PlayFab ID

**Created by:** PlayFab.

**Represents:** The player's classic PlayFab account identifier.

**Used by:** Classic PlayFab APIs and account references that explicitly expect a PlayFab ID.

Do not substitute it for an Entity ID merely because both identify something related to the same player.

### Session ticket

**Created by:** PlayFab after a successful Client API login.

**Represents:** The authenticated player session for classic Client API calls.

**Transport:** PlayFab sends it as authentication for APIs that require a client session ticket.

The session ticket is a credential. It must not be treated like a harmless public identifier.

### Entity token response

A login may also return an entity token response containing:

- Entity ID
- Entity type
- Entity token
- token expiration

If an entity token is not returned by the chosen login path, the client can obtain one through the appropriate entity-token authentication API.

### Entity ID

**Created by:** PlayFab.

**Represents:** One PlayFab entity, commonly the player's `title_player_account` entity for player Entity API operations.

**Used with:** Entity type.

### Entity type

**Created by:** PlayFab.

**Represents:** The kind of entity identified by the Entity ID.

Together, Entity ID and Entity type form an Entity Key.

### Entity token

**Created by:** PlayFab.

**Represents:** Authorization to call Entity APIs as the associated entity.

**Typical HTTP header:** `X-EntityToken`.

An entity token is a credential, not an ID.

### Entity token expiration

**Created by:** PlayFab.

**Represents:** The UTC time when an expiring entity token becomes invalid.

**Needed by:** A process that manages token lifetime, refreshes authentication, or avoids starting calls with an expired credential.

Receiving this field does not automatically refresh anything. Application code must decide how expired authentication is recovered.

### `NewlyCreated`

**Created by:** PlayFab.

**Represents:** Whether the login created the PlayFab player account.

Possible uses include:

- first-time account initialization
- onboarding decisions
- first-login rewards
- analytics

It is informational. It is not required for later authenticated calls.

## 6. Credential Comparison

| Value | ID or credential? | Issued by | Main purpose |
|---|---|---|---|
| Title ID | ID | PlayFab | Select the game title |
| Custom ID | External login ID | Game developer | Log in through `LoginWithCustomID` |
| PlayFab ID | ID | PlayFab | Identify the classic player account |
| Session ticket | Credential | PlayFab login | Authenticate classic Client API calls |
| Entity ID | ID | PlayFab | Identify an entity |
| Entity type | Type discriminator | PlayFab | Complete the Entity Key |
| Entity token | Credential | PlayFab | Authenticate Entity API calls |
| Entity token expiration | Metadata | PlayFab | Manage entity-token lifetime |
| `NewlyCreated` | Metadata | PlayFab | Detect account creation during login |
| Developer secret key | Credential | PlayFab title | Authenticate privileged Server/Admin APIs |

The developer secret key must never be shipped in a game client. It grants privileged access intended for trusted environments.

Official reference: [Secret key management](https://learn.microsoft.com/en-us/gaming/playfab/live-service-management/gamemanager/secret-key-management)

## 7. Client, Entity, Server, and Admin APIs

PlayFab divides APIs partly by who is allowed to call them.

### Client APIs

Called as an authenticated player, usually using a session ticket.

Appropriate for actions the player is allowed to perform directly.

The fact that an API is technically callable by a client does not automatically mean it is safe for a competitive or valuable operation. Titles can restrict client API access through API access policies.

### Entity APIs

Called as a PlayFab entity using an entity token.

Entity APIs provide common operations for different entity types, including Data and Economy V2.

### Server APIs

Called from a trusted server using privileged title credentials.

They must not be authenticated with a secret embedded in the client build.

### Admin APIs

Used for highly privileged title administration and configuration.

These also belong in trusted developer or server environments, not in a player client.

### The security question to ask

For every write operation, ask:

> If a modified client calls this API with arbitrary values, what could it steal, grant, unlock, or corrupt?

Valuable or authoritative operations usually need trusted server validation, even if a client SDK contains a method that can technically perform a related call.

Official reference: [API access policy](https://learn.microsoft.com/en-us/gaming/playfab/api-references/api-access-policy)

## 8. PlayFab SDK Requests and Responses

PlayFab SDK methods are typed wrappers around PlayFab's HTTP APIs.

A typical call contains:

1. A request model.
2. Authentication appropriate to that API.
3. A success result model.
4. A PlayFab error on failure.

Conceptually:

```text
Request object
    -> serialized to JSON
    -> sent to a PlayFab endpoint
    -> authenticated through a header/token

PlayFab response
    -> success model
    OR
    -> structured PlayFab error
```

When reading an unfamiliar SDK call, inspect:

- the API namespace: Client, Data, Economy, CloudScript, Server, or Admin
- the request type
- which fields are required
- which authentication mechanism the API requires
- the response type
- documented error codes
- concurrency and rate limits

Official overview: [PlayFab REST API](https://learn.microsoft.com/en-us/gaming/playfab/api-references/)

## 9. CloudScript

CloudScript is PlayFab's mechanism for executing trusted game logic associated with a PlayFab title.

CloudScript can use Azure Functions as the implementation of a registered function.

Common reasons to use trusted function logic include:

- validating a reward claim
- updating progression
- consuming valuable inventory
- enforcing an idempotent transaction
- combining several backend reads and writes
- hiding privileged credentials and rules from the client

Official overview: [CloudScript](https://learn.microsoft.com/en-us/gaming/playfab/live-service-management/service-gateway/automation/cloudscript/)

## 10. Executing an Azure Function Through CloudScript

The client calls PlayFab's `ExecuteFunction` API. PlayFab then invokes the registered Azure Function.

### `ExecuteFunctionRequest.Entity`

The entity on whose behalf the function is executed.

If omitted, PlayFab can use the currently authenticated entity. Supplying it explicitly makes the target entity visible in the request.

### `FunctionName`

The name used to select the registered function.

This is not necessarily the Azure URL. PlayFab's registration maps the function name to the Azure Function.

### `FunctionParameter`

The caller's input object.

PlayFab passes it into the function execution context as `FunctionArgument`.

This data is still supplied by the client. Passing through PlayFab does not make its contents trustworthy.

### `GeneratePlayStreamEvent`

An optional flag requesting a PlayStream event for the function execution and result context.

### Function result

`ExecuteFunctionResult` can contain:

- the executed function name
- execution time
- the returned function result
- a function execution error
- the size of the result
- a flag indicating that an oversized result was dropped

The transport succeeding does not guarantee that the function itself succeeded. Code must distinguish:

1. A network or PlayFab API failure.
2. A CloudScript/Azure function execution error.
3. A valid function response that rejects the requested game action.

Those are three different outcomes.

## 11. Function Execution Context

When PlayFab invokes the Azure Function, the request body contains context beyond the client's function argument.

Important concepts include:

### Caller entity profile

Identifies the authenticated entity that called the function.

Trusted backend code should derive the acting player/entity from PlayFab's caller context rather than accepting an arbitrary player ID inside the client's function argument.

### Function argument

The client-supplied `FunctionParameter`.

It should be deserialized into a request model and validated as untrusted input.

### Title authentication context

Credentials supplied for the function to call PlayFab services as the title/trusted backend.

This is separate from the player's entity token used by the game client.

The distinction is:

```text
Player credential
    -> proves which player is calling PlayFab

Title/server credential
    -> lets trusted backend logic perform privileged PlayFab operations
```

## 12. Data Storage Choices

PlayFab provides multiple storage systems. Choosing one requires understanding ownership and access patterns.

### Player Data / UserData

Classic per-player key/value storage.

Microsoft currently recommends Entity Objects for new titles that need flexible player data storage.

### Entity Objects

JSON-like objects attached to an entity.

Good for structured state that can be read and replaced as named objects.

### Entity Files

Files attached to an entity.

The file API separates metadata access, content download, upload initiation, byte upload, and upload finalization.

Useful when the desired unit of storage is naturally a file or a larger serialized document.

Official references:

- [Player Data](https://learn.microsoft.com/en-us/gaming/playfab/player-progression/player-data/)
- [Entity Objects](https://learn.microsoft.com/en-us/gaming/playfab/live-service-management/game-configuration/entities/entity-objects)
- [Entity Files](https://learn.microsoft.com/en-us/gaming/playfab/live-service-management/game-configuration/entities/entity-files)

## 13. Entity Files Process

Reading an Entity File generally involves:

1. Call `GetFiles` for an Entity Key.
2. Find the desired filename in returned metadata.
3. Use its download URL to download the content.

Writing Entity Files generally involves:

1. Read the current entity profile version.
2. Call `InitiateFileUploads` with the filenames and expected profile version.
3. Receive upload URLs.
4. Upload each file's bytes to its URL.
5. Call `FinalizeFileUploads`.

The upload URL is part of a temporary upload workflow. It is not the permanent public identity of the file.

## 14. Optimistic Concurrency and Profile Version

Optimistic concurrency assumes conflicting writes are uncommon, but detects them when they happen.

Example:

```text
Request A reads profile version 10.
Request B reads profile version 10.

Request A writes successfully.
The profile advances.

Request B tries to write using its old expected version 10.
PlayFab rejects the conflicting write.
```

Without this check, request B might silently overwrite changes made by request A.

A normal retry process is:

1. Detect the profile-version conflict.
2. Reload the latest state.
3. Re-evaluate the requested operation against that state.
4. Attempt the write again with the new profile version.
5. Stop after a bounded number of retries.

Do not retry by blindly uploading the old computed result. The operation may need a different answer after newer state is loaded.

PlayFab's API guidance also notes that concurrent reads are generally safer than concurrent updates against the same player/entity.

## 15. Economy V2

Economy V2 separates two major ideas:

```text
Catalog
    -> definitions of items that exist

Inventory
    -> item stacks owned by a specific entity
```

Creating a catalog item does not automatically grant it to every player. Granting an inventory item does not redefine the catalog item.

Economy V1 is in maintenance mode. New development should learn Economy V2 unless a project has a specific legacy dependency.

Official overview: [Economy V2](https://learn.microsoft.com/en-us/gaming/playfab/economy-monetization/economy-v2/overview)

## 16. Catalog Vocabulary

### Catalog item

A definition of something that can exist in the game's economy.

Economy V2 catalog item types include items, currencies, bundles, stores, subscriptions, and user-generated content.

### Catalog ID

The canonical identifier assigned to the catalog item.

Inventory entries reference catalog items by ID.

### Friendly ID

An optional human-controlled identifier used for easier reference in code and tools.

Friendly IDs must be unique within the catalog.

The Friendly ID does not replace the catalog item's canonical ID in every API. Code may resolve a Friendly ID to the catalog ID required by an inventory operation.

### Alternate ID

A typed alternative identifier attached to a catalog item.

Conceptually:

```json
{
  "Type": "FriendlyId",
  "Value": "healing_potion"
}
```

`FriendlyId` is the alternate-ID type. `healing_potion` is the developer-chosen value.

The SDK models alternate-ID type as a string, so application code may define its own constant containing the PlayFab-recognized string `FriendlyId`.

### Content type

A title-defined classification used to organize and validate catalog items.

Examples might include:

- `booster`
- `cosmetic`
- `weapon`

Unlike the special alternate-ID type `FriendlyId`, content-type values are game-defined catalog configuration.

### Display properties

Custom JSON metadata associated with a catalog item.

Useful for item-specific data that belongs to the item definition, while authoritative gameplay rules may still need trusted server validation.

Official reference: [Catalog overview](https://learn.microsoft.com/en-us/gaming/playfab/economy-monetization/economy-v2/catalog/catalog-overview)

## 17. Inventory Vocabulary

### Inventory

A collection owned by an entity.

### Inventory item

An owned reference to a catalog item, with inventory-specific state such as amount and stack identity.

### Stack

Economy V2 can store separate instances/stacks of the same catalog item in one inventory.

Each stack can have:

- the same catalog item ID
- a different Stack ID
- its own amount
- its own display properties

### Stack ID

Distinguishes one stack of a catalog item from another.

When no Stack ID is specified, inventory actions typically use the stack named `default`.

The default stack may be deleted and recreated by later add or transfer operations.

### Amount

The quantity contained in that stack.

### `DeleteEmptyStacks`

Controls whether subtraction removes a stack when its amount reaches zero.

- `true`: remove the empty stack
- `false`: retain a zero-amount stack

Which choice is correct depends on whether the application treats stack existence itself as meaningful.

### Continuation token

Inventory results can be paginated.

The first response may include a continuation token. The next request sends that token to receive another page. Continue until PlayFab returns no continuation token.

Official reference: [Inventory stacks](https://learn.microsoft.com/en-us/gaming/playfab/economy-monetization/economy-v2/inventory/stacks)

## 18. Inventory Mutation

Economy V2 provides operations such as:

- add inventory items
- subtract inventory items
- update inventory items
- delete inventory items
- transfer inventory items
- purchase inventory items

A trusted consume operation commonly needs:

```text
Target Entity Key
Catalog Item ID
Stack ID
Amount
Idempotency ID
```

If subtraction exceeds the available amount, PlayFab returns an error such as insufficient funds/quantity rather than producing a negative inventory amount.

The game backend should translate low-level PlayFab errors into game-level outcomes meaningful to its caller.

## 19. Idempotency

Idempotency means retrying the same logical operation does not apply its side effect multiple times.

Example problem:

1. A client asks to consume one item.
2. The server successfully subtracts it.
3. The response is lost.
4. The client cannot tell whether the subtraction happened.
5. The client retries.

Without idempotency, the retry may subtract a second item.

With idempotency:

1. The client creates one operation ID for the logical consume action.
2. Every retry of that action reuses the same ID.
3. The inventory API recognizes the repeated operation.
4. The side effect is not applied as a new independent transaction.

### Important rules

- Generate a new idempotency ID for a new logical operation.
- Reuse the same ID for retries of that operation.
- Do not generate a new ID merely because a network request is retried.
- Do not reuse one ID for two different operations.

An operation ID and a gameplay-session/attempt ID solve different problems:

```text
Operation ID
    -> identifies one requested transaction for idempotency

Session or attempt ID
    -> identifies a longer-lived gameplay process across multiple requests
```

## 20. Error Layers

PlayFab-integrated code should distinguish several kinds of failure.

### Transport failure

The request did not complete normally because of connectivity, timeout, DNS, or another transport problem.

The final server outcome may be unknown, which is why idempotent retries matter.

### PlayFab API error

PlayFab received the request but rejected or failed it.

Examples include:

- invalid authentication
- permission denied
- rate limiting
- insufficient inventory
- concurrent edit
- invalid request fields

### CloudScript function error

PlayFab invoked the server function, but the function threw or returned a function execution error.

### Valid domain rejection

The complete request pipeline worked, but trusted game rules rejected the action.

Examples:

- item not owned
- level locked
- operation conflicts with current state

A domain rejection is not necessarily an infrastructure failure. It should usually be represented as an explicit outcome rather than as a network exception.

## 21. Security Rules Worth Memorizing

1. Never put a PlayFab developer secret key in a shipped client.
2. Treat session tickets and entity tokens as credentials.
3. Treat all function arguments from the client as untrusted input.
4. Derive the caller identity from authenticated PlayFab context.
5. Keep valuable inventory and progression changes behind trusted validation.
6. Do not trust a player ID sent as an ordinary request field to establish ownership.
7. Use idempotency for valuable operations that may be retried.
8. Handle concurrent writes explicitly instead of assuming requests never overlap.
9. Link anonymous accounts to recoverable identities before account loss becomes expensive.
10. Restrict client-callable APIs according to the title's security model.

## 22. Recommended Learning Order

### Stage 1: Identity and authentication

Learn:

- title
- login method
- Custom ID
- PlayFab ID
- session ticket
- entity
- Entity Key
- entity token
- token expiration
- anonymous versus recoverable login

You are ready to move on when you can explain why Custom ID, PlayFab ID, and Entity ID are three different values.

### Stage 2: API authorization

Learn:

- Client API
- Entity API
- Server API
- Admin API
- secret keys
- API access policy

You are ready to move on when you can decide whether a new API call belongs in a player client or trusted server.

### Stage 3: CloudScript and Azure Functions

Learn:

- function registration
- `ExecuteFunction`
- caller entity
- function argument
- title authentication context
- function result versus function error

You are ready to move on when you can explain why a function argument is still untrusted even though PlayFab delivered it.

### Stage 4: Entity Data

Learn:

- Entity Objects
- Entity Files
- metadata and download URLs
- upload initiation and finalization
- profile version
- optimistic concurrency

You are ready to move on when you can explain how two requests can both read version 10 and then conflict while writing.

### Stage 5: Economy V2

Learn:

- catalog versus inventory
- catalog ID
- Friendly ID
- alternate ID
- content type
- Entity Key ownership
- stacks, Stack ID, and amount
- continuation tokens
- inventory mutation
- idempotency ID

You are ready when you can explain why an inventory operation may begin with a Friendly ID but ultimately mutate a stack using a catalog ID and Stack ID.

## 23. Self-Test

Answer these without looking back:

1. Who generates a Custom ID?
2. What does PlayFab return after a successful Client login?
3. What is the difference between a PlayFab ID and an Entity ID?
4. Why does an Entity Key contain both ID and type?
5. Which credential authenticates classic Client API calls?
6. Which credential authenticates Entity API calls?
7. Why must a developer secret key never be placed in Unity?
8. What is the difference between `FunctionParameter` and the caller entity?
9. Why is a CloudScript function argument untrusted?
10. What is the difference between an Entity Object and an Entity File?
11. What problem does profile version solve?
12. What is the difference between a catalog and an inventory?
13. What is the relationship between Catalog ID and Friendly ID?
14. What distinguishes two stacks of the same catalog item?
15. Why must an idempotency ID be reused during a retry?
16. How is an operation ID different from a gameplay attempt/session ID?
17. What is the difference between a PlayFab API error and a valid domain rejection?

## 24. Official Documentation Index

- [Player login](https://learn.microsoft.com/en-us/gaming/playfab/identity/player-identity/login/)
- [Login With Custom ID API](https://learn.microsoft.com/en-us/rest/api/playfab/client/authentication/login-with-custom-id?view=playfab-rest)
- [PlayFab REST API overview](https://learn.microsoft.com/en-us/gaming/playfab/api-references/)
- [API access policy](https://learn.microsoft.com/en-us/gaming/playfab/api-references/api-access-policy)
- [Secret key management](https://learn.microsoft.com/en-us/gaming/playfab/live-service-management/gamemanager/secret-key-management)
- [CloudScript](https://learn.microsoft.com/en-us/gaming/playfab/live-service-management/service-gateway/automation/cloudscript/)
- [Player Data](https://learn.microsoft.com/en-us/gaming/playfab/player-progression/player-data/)
- [Entity Objects](https://learn.microsoft.com/en-us/gaming/playfab/live-service-management/game-configuration/entities/entity-objects)
- [Entity Files](https://learn.microsoft.com/en-us/gaming/playfab/live-service-management/game-configuration/entities/entity-files)
- [Economy V2 overview](https://learn.microsoft.com/en-us/gaming/playfab/economy-monetization/economy-v2/overview)
- [Catalog overview](https://learn.microsoft.com/en-us/gaming/playfab/economy-monetization/economy-v2/catalog/catalog-overview)
- [Inventory stacks](https://learn.microsoft.com/en-us/gaming/playfab/economy-monetization/economy-v2/inventory/stacks)
