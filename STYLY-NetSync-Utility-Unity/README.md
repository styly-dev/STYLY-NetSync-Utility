**[日本語版はこちら (Japanese)](README_ja.md)**

# STYLY NetSync Utility

A utility package that wraps the core functionality of STYLY NetSync (`com.styly.styly-netsync`), providing a **type-safe** and **reactive** interface.

## Benefits

- **Type-safe Enum-based definitions** — Define RPCs and synced variables as Enums to eliminate bugs caused by string typos.
- **R3 Observable support** — Subscribe to variables and RPCs as reactive streams via `AsObservable()`, enabling filtering, transformation, and composition with LINQ operators.
- **UnityEvent compatibility** — Also supports traditional callback-style subscriptions via `AddListener()`, making integration with existing code easy.
- **Reduced boilerplate** — Handles common patterns internally: waiting for NetSyncManager readiness, string-to-type conversion, and client number filtering.
- **Self API** — Provides `SetSelf` / `GetSelf` / `AsObservableSelf` for user variables, automatically resolving your own client number to keep code concise.

## Package Info

| Field | Value |
|-------|-------|
| Package name | `com.styly.styly-netsync.utility` |
| Version | 0.4.2 |
| Unity | 6000.0 or later |
| Namespace | `Styly.NetSync.Utility` |

### Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `com.styly.styly-netsync` | 0.10.2 | Core network synchronization |
| `com.cysharp.r3` | 1.3.0 | Reactive Extensions (Observable) |
| `com.unity.xr.management` | 4.5.2 | XR loader management (used by `XRLoaderAutoConfigurator`) |
| `com.cysharp.unitask` | 2.5.10 | async/await support |

## Basic Usage

### EventManager — Waiting for Ready State

`EventManager` is a singleton that waits for NetSyncManager to become ready.

```csharp
// async/await pattern
await EventManager.Instance.WaitForReadyAsync(destroyCancellationToken);

// Observable pattern
EventManager.Instance.OnReadyAsObservable()
    .Subscribe(_ => Debug.Log("Ready!"))
    .AddTo(this);
```

### RpcManagerBase — Sending and Receiving RPCs

Define RPCs as an Enum and create a subclass for type-safe RPC management.

```csharp
// 1. Define RPCs as an Enum
public enum GameRpc { Ready, StartRound, Attack }

// 2. Create a subclass and attach it to a GameObject
public class GameRpcManager : RpcManagerBase<GameRpc> { }
```

**Sending:**

```csharp
// Send to all clients
rpcManager.Send(GameRpc.Attack, new[] { "fireball", "50" });

// Send to a specific client
rpcManager.Send(GameRpc.Attack, new[] { "heal", "30" }, targetClientNo: 2);

// Send to multiple specific clients
rpcManager.Send(GameRpc.Attack, new[] { "aoe" }, new[] { 1, 3, 5 });
```

**Receiving (Observable):**

```csharp
rpcManager.AsObservable(GameRpc.Attack)
    .Subscribe(data =>
    {
        int sender = data.ClientNo;
        string skill = data.Parameters[0];
        string damage = data.Parameters[1];
        Debug.Log($"Client {sender}: {skill} ({damage} dmg)");
    })
    .AddTo(this);
```

**Receiving (UnityEvent):**

```csharp
rpcManager.AddListener(GameRpc.Attack, (clientNo, parameters) =>
{
    Debug.Log($"Client {clientNo} attacked!");
});
```

### VariableManagerBase — Synced Variable Management

Define global variables (shared across all clients) and user variables (per-client) with two Enums.

```csharp
// 1. Define variables as Enums
public enum GlobalVar { GamePhase, RoundNumber }
public enum UserVar   { Health, Score, IsAlive }

// 2. Create a subclass and attach it to a GameObject
public class GameVarManager : VariableManagerBase<GlobalVar, UserVar> { }
```

**Reading and writing global variables:**

```csharp
// Set (type conversion is automatic)
vars.Set(GlobalVar.RoundNumber, 3);

// Get (with default value)
int round = vars.Get<int>(GlobalVar.RoundNumber, 1);

// Subscribe (initial value + change notifications)
vars.AsObservable<int>(GlobalVar.RoundNumber)
    .Subscribe(n => roundText.text = $"Round {n}")
    .AddTo(this);

// Subscribe to changes only (no initial value)
vars.AsObservableOnChanged(GlobalVar.GamePhase)
    .Subscribe(phase => Debug.Log($"Phase changed: {phase}"))
    .AddTo(this);
```

**Reading and writing user variables (Self — your own client):**

```csharp
// Set and get your own values
vars.SetSelf(UserVar.Health, 100);
int myHp = vars.GetSelf<int>(UserVar.Health);

// Subscribe to your own values (initial value + change notifications)
vars.AsObservableSelf<int>(UserVar.Health)
    .Subscribe(hp => healthBar.value = hp)
    .AddTo(this);
```

**Observing user variables (all clients / specific client):**

```csharp
// Observe changes from all clients
vars.AsObservableOnChanged<int>(UserVar.Score)
    .Subscribe(data => UpdateScoreboard(data.ClientNo, data.Value))
    .AddTo(this);

// Subscribe to a specific client (initial value + change notifications)
vars.AsObservable<int>(UserVar.Health, clientNo: 2)
    .Subscribe(hp => player2HealthBar.value = hp)
    .AddTo(this);
```

## Included Shaders / Materials

`Runtime/Materials` contains utility shaders for LBE (Location-Based Experience) use cases.

| Shader | Purpose |
|--------|---------|
| `NetSyncUtility/Occlusion` | A depth-only occlusion shader. Place it on real-world walls and floors to occlude virtual objects behind them. |
| `NetSyncUtility/WarningWall` | A red lattice pattern shader that fades with distance. Used to visually warn players of play area boundaries. Uses triplanar projection so the pattern is independent of mesh orientation. |

## Editor Tools

### Suppressing the Project Validation Window

XR Hands / XR Interaction Toolkit sample scripts contain code that automatically opens the Project Settings window on Unity editor startup. This is especially annoying in environments that launch multiple editor instances, such as Multiplayer Playmode. Use the menu to suppress or restore this behavior.

| Menu | Description |
|------|-------------|
| **Tools > Suppress Project Validation Window** | Suppress the automatic Project Settings window |
| **Tools > Restore Project Validation Window** | Restore the original behavior |

### Automatic OpenXR Loader Switching

`XRLoaderAutoConfigurator` automatically enables or disables the OpenXR loader based on the presence of the `USE_OPENXR` Scripting Define Symbol. It runs automatically on editor load and as a build preprocessor — no manual action required.

- `USE_OPENXR` defined → OpenXR loader enabled
- `USE_OPENXR` not defined → OpenXR loader disabled
