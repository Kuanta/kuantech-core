# Description

## Design Philosophy

KuantechCore is a **game-agnostic foundation**. It provides reusable building blocks; game-specific rules, values, and logic live in the consuming project — not here.

**Key principles:**

- **No hardcoded gameplay assumptions.** A module must never embed game-specific constants (e.g. reward values, damage numbers, faction names, ability IDs). These are always injected via ScriptableObjects, configuration assets, or virtual method overrides.
- **Composition over inheritance.** Game-specific behaviour is added by writing new `ActorModule` components and attaching them to actors — not by subclassing existing modules. Features are combined at the prefab level, not through deep class hierarchies.
- **Override points over conditionals.** When a system needs to vary per game, expose a `virtual` method or an event/callback — not an `if (gameType == ...)` branch.
- **Separation of definition and execution.** What counts as a "reward", a "goal", or a "threat" is defined by the game project. The core framework only provides the execution machinery (scheduling, ticking, state storage).

Example: an AI scoring system in core would expose `protected virtual float EvaluateAction(BTLeafAction action)` returning a normalized score — the game project overrides this method to define what "good" means in that specific game.

---


## GameManager & SubManager System

`GameManager` is the root singleton that survives scene loads (`DontDestroyOnLoad`). It owns all **SubManagers** and orchestrates their lifecycle.

### Two Manager Categories

| Category | Lifetime | Discovery |
| --- | --- | --- |
| **Persistent** | Entire game session | Children of `GameManager` in the first scene |
| **Scene-specific** | Current scene only | Children of `SceneSubManagerContainer` in the scene |

`GetSubManagerByType<T>()` checks persistent first, then scene managers. Cleaned up on scene change.

### Initialization Flow

```
GameManager.Start()
  → GetComponentsInChildren<SubManager>()          // discover persistent managers
  → InitializeSubManagers()                         // parallel: UniTask.WhenAll
      → each subManager.Initialize(gameManager)     // async, run in parallel
  → OnSubmanagersInitialized()                      // all ready, called sequentially
  → OnNewScene()                                    // discover scene managers + repeat
```

On scene load, `SceneSubManagerContainer` (a component placed in the scene) provides the scene-specific managers. They go through the same `Initialize → OnSubmanagersInitialized` cycle, then all managers receive `OnSceneEntry` and `OnPostSceneLoaded`.

### SubManager Lifecycle Hooks

```csharp
public class MyManager : SubManager
{
    // Called once, async — fetch remote data, warm caches, etc.
    public override async UniTask Initialize(GameManager gm) { ... }

    // All managers initialized — safe to call GetContext<OtherManager>()
    public override void OnSubmanagersInitialized() { ... }

    // Player just entered a new scene
    public override void OnSceneEntry() { ... }

    // Player is leaving the current scene (before load)
    public override void OnSceneLeave() { ... }

    // Scene fully loaded + all managers initialized, transition data available
    public override void OnPostSceneLoaded(LevelTransitionData data, string prevScene) { ... }

    // Scene-specific manager being torn down
    public override void Cleanup() { ... }
}
```

### Scene Transitions

```
GameManager.ChangeScene("DungeonScene", transitionData)
  → foreach sceneSubManager: Cleanup()     // tear down scene managers
  → foreach allManagers: OnSceneLeave()    // persistent managers notified
  → LoadingScreen.SetActive(true)
  → SceneManager.LoadScene()
  → OnSceneLoaded fires
    → discover SceneSubManagerContainer
    → InitializeSubManagers(sceneManagers)
    → OnSubmanagersInitialized()
    → OnSceneEntry() + OnPostSceneLoaded()  // all managers
    → LoadingScreen.SetActive(false)
```

**`LevelTransitionData`** is an abstract class — subclass it to pass typed data between scenes (e.g. chosen character, dungeon seed).

### Access Pattern

Every `SubManager` has a static helper that routes through `GameManager.Instance`:

```csharp
MyManager.GetContext<MyManager>()   // from anywhere, including other SubManagers
// equivalent to:
GameManager.Instance.GetSubManagerByType<MyManager>() as MyManager
```

---

## Architecture: Actor + Module Pattern

Everything interactive in the world is an **Actor**. Behaviour is composed via **ActorModules**.

```
Actor (NetworkBehaviour or MonoBehaviour)
  ├── StatsModule        (ActorModule)
  ├── HealthcareModule   (ActorModule)
  ├── CombatModule       (ActorModule)
  ├── MovementModule     (ActorModule)
  ├── AnimationModule    (ActorModule)
  ├── BTAgent            (ActorModule) ← AI
  └── CustomModule       (ActorModule) ← game-specific
```

### Actor

```csharp
// State machine
enum ActorState { Inactive, Spawned, Dead, Despawned }

// Module access
T GetModule<T>() where T : ActorModule
List<T> GetModules<T>() where T : ActorModule

// Lifecycle
void Initialize(ActorSerializableData data = null)
void Spawn()
void Despawn(float delay = 0)
void KillActor(GameObject killer = null)

// Save/Load
ActorSerializableData GetActorState()
void LoadActorState(ActorSerializableData data)

// Events
UnityAction<ActorState> OnActorStateChanged
UnityAction<Actor> OnSpawnedEvent
UnityAction<Actor> OnDeathEvent
UnityAction<Actor> OnDespawnedEvent
UnityAction<HitInfo> OnHitEvent
UnityAction<Actor> OnStateLoaded       // fires after LoadActorState
```

**Module discovery:** On `Initialize()`, all `ActorModule` children are discovered via `GetComponentsInChildren`. Stored in:

- `ActorModulesList` — all modules (ordered)
- `Modules` — `Dictionary<Type, List<ActorModule>>`
- `ModulesById` — `Dictionary<string, ActorModule>` — **only modules with `ModuleId` set in inspector**

### ActorModule

```csharp
public Actor Actor { get; set; }      // set by Actor before Initialize()
public string ModuleId;               // inspector field — required for save/load

// Lifecycle callbacks (all virtual, no-op defaults)
void Initialize()
void OnModulesInitialized()           // all modules ready, safe to cross-reference
void SetDefaultValues()               // reset to design-time defaults
void ResetModule()                    // actor respawned
void ModuleUpdate/FixedUpdate/LateUpdate()
void Cleanup()                        // actor despawning
void OnActorStateChanged(ActorState old, ActorState new)

// Save/Load
ActorModuleSerializableData CreateModuleState()    // calls InstantiateState() internally
void LoadState(ActorModuleSerializableData data)

// Networking (Unity Netcode for GameObjects — NGO)
void OnLocalPlayerStart()             // this actor is now the local player
void OnLocalPlayerStop()
void OnNetworkSynced()                // late-join state sync received from server

// Networking helpers (from NetworkBehaviour)
bool IsServer
bool IsClient
bool IsOwner
bool IsSpawned
bool IsDedicatedServer   // IsServer && !IsClient
```

---

## Networking (Unity Netcode for GameObjects — NGO)

Migrated from FishNet after Unity 6.5 broke FishNet's compile (see git history for the migration). `StatsModule`, `SpellBook`, `CombatModule`, `InventoryModule`, `StatusEffectHandler` and the two custom serializers (`RpgSerializer`, `ActionCastDataSerializer`) are still FishNet-shaped internally but sit behind a guard that's never defined, so they compile as inert offline stubs — not yet ported to NGO.

### Authority Model

- **Server-authoritative:** All state changes execute on server first.
- **`[Rpc(SendTo.NotServer)]`** — replicate public data (health bars, actor state, animations) to all clients except the host.
- **`[Rpc(SendTo.Owner)]`** — replicate private data (attributes, modifiers) to owner only.
- **`[Rpc(SendTo.SpecifiedInParams)]`** — full state sync to one specific client (e.g. a new connection).

### Standard RPC Pattern

```csharp
// On a module method that changes state:
public void DamageResource(DamageInfo info)
{
    if (!IsServer) return;                   // server only
    ExecuteDamageResource(info);             // local execution
    if (IsSpawned)
        ObserverSyncResource_Rpc(asset, newValue);
}

[Rpc(SendTo.NotServer)]
private void ObserverSyncResource_Rpc(ResourceAsset asset, float value)
{
    // No self-skip guard needed — SendTo.NotServer never invokes this on the host in the first place.
    ExecuteSetResourceValue(asset, value);
}
```

### NGO Callback Order (Scene Objects)

| Callback | Fires on | When |
| --- | --- | --- |
| `OnNetworkSpawn()` | All peers | Network initializes — fires for late-joining clients too |
| `OnGainedOwnership()` | The new owner | Ownership assigned (including initial spawn, if owned) |
| `OnLostOwnership()` | The previous owner | Ownership taken away |
| `OnNetworkDespawn()` | All peers that had it spawned | Object removed from the network |

`Actor.OnNetworkSpawn` → `Initialize()` + `Spawn()` (+ `StartLocalPlayer()` if owner)

### Late-Join State Sync

NGO has no per-object "new observer" callback like FishNet's `OnSpawnServer` — a newly-connected client doesn't automatically get pushed anything beyond `NetworkVariable` values. `KtNetworkManager` fills the gap:

```
New client connects
  → NetworkManager.OnClientConnectedCallback fires (server only)
  → KtNetworkManager iterates ActorManager.GetAllActors()
  → each Actor.PushStateTo(clientId)
      → GetActorState() snapshots all modules in ModulesById
      → SaveUtility.SerializePoco() → JSON bytes (TypeNameHandling.Auto handles polymorphism)
      → TargetSyncActorState_Rpc(bytes, RpcTarget.Single(clientId, RpcTargetUse.Temp))
      → Client: DeserializePoco → LoadActorState → OnStateLoaded fires
      → foreach module: OnNetworkSynced()
```

**Requirement:** Module must have `ModuleId` set in inspector to be included in state sync.

### Despawn Flow

```
Server: Despawn(delay)
  → NetworkObject.Despawn()
  → OnNetworkDespawn() on every peer → ExecuteNetworkDespawn() [Cleanup + state change, no pooling]
  → NGO manages object lifecycle (destroy/deactivate)

Standalone (no NetworkObject):
  → _DespawnRoutine → ExecuteLocalDespawn() → PoolManager.PoolObject()
```

---

## Save / Load System

### Serialization

```csharp
// SaveUtility (static)
byte[] SerializePoco<T>(T value)    // Newtonsoft JSON, TypeNameHandling.Auto
T DeserializePoco<T>(byte[] bytes)
```

`TypeNameHandling.Auto` writes `$type` discriminator for derived types, enabling polymorphic deserialization of `ActorModuleSerializableData` subclasses.

### Actor State Structure

```csharp
class ActorSerializableData
{
    string ActorId;
    Dictionary<string, ActorModuleSerializableData> ModuleStates; // key = ModuleId
}
```

### Adding Save Support to a Module

```csharp
public class MyModule : ActorModule
{
    // 1. Set ModuleId in inspector, e.g. "mymodule"

    // 2. Define data class
    [Serializable]
    public class MyModuleData : ActorModuleSerializableData
    {
        public int SomeValue;
    }

    // 3. Snapshot current state
    protected override ActorModuleSerializableData InstantiateState()
        => new MyModuleData { SomeValue = _someValue };

    // 4. Restore from snapshot
    public override void LoadState(ActorModuleSerializableData data)
    {
        base.LoadState(data);
        var d = data as MyModuleData;
        _someValue = d.SomeValue;
    }
}
```

---

## RPG Systems

### StatsModule

```csharp
// Attributes
void SetAttributeRank(string id, int rank)
void SetAttributeValue(string id, float value)
float GetAttributeValue(AttributeAsset asset)

// Resources (health, mana, stamina…)
float GetResourceValue(ResourceAsset asset, float defaultValue = 0)
float GetResourceMaxValue(ResourceAsset asset, float defaultValue = 1)
void SetResourceValue(ResourceAsset asset, float value)
void RefreshResourceValue(ResourceAsset asset)

// Level
void AddExperience(float amount)
void SetLevel(int level)     // server-authoritative + ObserversRpc
int CurrentLevel             // via ActorLevel.CurrentLevel

// Modifiers
void AddModifier(StatModifier modifier)
void RemoveModifiers(List<StatModifier> modifiers)
void ClearModifiers()
```

**Save data (`StatsSerializableData`):**

```csharp
int Level;
Dictionary<string, int> AttributeRanks;   // key = AttributeAsset.Id
Dictionary<string, float> ResourceValues; // key = ResourceAsset.Id
```

**Inspector requirement:** `ModuleId` must be set (e.g. `"stats"`) for state to persist and sync.

### HealthcareModule

- Links resources (health, mana, etc.) to `Fillbar` UI components
- `DamageResource(DamageInfo)` — server-authoritative, syncs value via `[Rpc(SendTo.NotServer)]`
- `HealResource(ResourceAsset, float)` — server-authoritative
- Hit animations — **not yet networked**: `ObserversHitAnim_Rpc` runs local-only until `RpgSerializer` is ported (see below), since `HitInfo` isn't network-serializable yet
- `OnNetworkSynced()` — refreshes all bars after late-join sync
- Subscribes to `Actor.OnStateLoaded` — refreshes bars after any `LoadActorState` call

### RpgSerializer

**Not yet migrated to NGO.** Still a FishNet custom serializer for RPG types, compiled out (inert) since the project no longer defines the FishNet guard symbol. Needs porting to `INetworkSerializable` on each type before `StatsModule`, `SpellBook`, `CombatModule`, and `HealthcareModule`'s hit-animation RPC can go back over the network. Currently handles:

- `ResourceAsset` ↔ `string Id` (via `RpgManager.GetResourceAssetById`)
- `DamageType` ↔ `string Id`
- `AttributeDefinition`, `StatModifier`, `DamageInfo`, `HitInfo`
- `HitInfo.Hitter` ↔ `NetworkObject`

---

## AI System

Currently, only major AI system is Behaviour Trees. 

### Behaviour Tree

```
BehaviourTree
  └── BTNode (composite/decorator/leaf)
        └── Returns: Running | Success | Failure
```

```csharp
// Attach to an actor
BTAgent agent = GetModule<BTAgent>();
agent.SetTree(myBehaviourTree);

// Custom leaf node
public class MoveToPlayerAction : BTLeafAction
{
    protected override BTNodeStatus Execute() { ... }
}
```

`BTAgent` ticks the tree at configurable interval with random jitter. Pauses automatically on actor death. Resumes on spawn.

## Pool Manager

```csharp
// Pool an object (return to pool)
PoolManager.GetContext<PoolManager>().PoolObject(gameObject);

// Spawn from pool (or instantiate if pool empty)
// Typically done via Level/Spawner systems
```

**Rule:** Never call `PoolManager.PoolObject` from NGO's `OnNetworkDespawn`. NGO owns networked object lifecycle. Only use PoolManager in standalone (non-networked) despawn path.

---

## Camera System

```csharp
// Get active camera
KtCamera cam = CameraManager.GetKtCamera();

// Get aim point (for attack direction)
Vector3 aimPoint = cam.GetAimPoint();

// Set orbit camera anchor (e.g. player spawn)
OrbitCameraFollower follower = cam.GetComponent<OrbitCameraFollower>();
follower.Anchor = actor.transform;

// Temporary camera switch
CameraManager.GetContext<CameraManager>().SwitchCamera(camera, duration);
```

---

## Static Context Access

All managers use the same pattern:

```csharp
PoolManager.GetContext<PoolManager>()
UIManager.GetContext<UIManager>()
CameraManager.GetContext<CameraManager>()
ControllerManager.GetContext<ControllerManager>()
RpgManager.GetContext<RpgManager>()
GameStateManager.GetContext<GameStateManager>()
```


## Third-Party Licenses

| Package | License | Commercial Use |
| --- | --- | --- |
| Netcode for GameObjects | Unity Package | ✅ (Unity license) |
| Newtonsoft.Json | MIT | ✅ |
| UniTask | MIT | ✅ |
| Cinemachine | Unity Package | ✅ (Unity license) |
| Unity InputSystem | Unity Package | ✅ (Unity license) |