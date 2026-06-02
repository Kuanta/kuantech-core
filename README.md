# Description

# KuantechCore — Technical Reference

KuantechCore is a reusable Unity game framework. It is engine-agnostic (no hard gameplay dependencies) and networking-optional (`#if NETWORKING_FISHNET` gates all FishNet code).

**Namespaces:** `Kuantech.Core`, `Kuantech.Rpg`, `Kuantech.AI`**Networking:** FishNet (optional, compile flag)
**Serialization:** Newtonsoft.Json (`TypeNameHandling.Auto`)
**Async:** UniTask

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

// Networking (FishNet)
void OnLocalPlayerStart()             // this actor is now the local player
void OnLocalPlayerStop()
void OnNetworkSynced()                // late-join state sync received from server

// Networking helpers (from NetworkBehaviour)
bool IsServerInitialized
bool IsClientInitialized
bool IsOwner
bool IsSpawned
bool IsDedicatedServer   // IsServerInitialized && !IsClientInitialized
```

---

## Networking (FishNet)

### Authority Model

- **Server-authoritative:** All state changes execute on server first.
- **ObserversRpc** — replicate public data (health bars, actor state, animations) to all clients.
- **TargetRpc** — replicate private data (attributes, modifiers) to owner only.
- **TargetSyncActorState_Rpc** — full state sync to new observer on connect.

### Standard RPC Pattern

```csharp
// On a module method that changes state:
public void DamageResource(DamageInfo info)
{
    if (!IsServerInitialized) return;        // server only
    ExecuteDamageResource(info);             // local execution
    if (IsSpawned)
        ObserverSyncResource_Rpc(asset, newValue);
}

[ObserversRpc]
private void ObserverSyncResource_Rpc(ResourceAsset asset, float value)
{
    if (IsServerInitialized) return;         // skip on listen server (already ran above)
    ExecuteSetResourceValue(asset, value);
}
```

### FishNet Callback Order (Scene Objects)

| Callback | Fires on | When |
| --- | --- | --- |
| `OnStartNetwork()` | All peers | Network initializes — fires for late-joining clients too |
| `OnStartServer()` | Server | Server-side init |
| `OnStartClient()` | Client | Client-side init |
| `OnSpawnServer(conn)` | Server | Every time a NEW observer is added |
| `OnStopServer()` | Server | Object removed from server |
| `OnStopClient()` | Client | Object removed from client |

`Actor.OnStartNetwork` → `Initialize()` + `Spawn()`

### Late-Join State Sync

```
New client connects
  → OnSpawnServer(connection) fires on server for each visible actor
  → GetActorState() snapshots all modules in ModulesById
  → SaveUtility.SerializePoco() → JSON bytes (TypeNameHandling.Auto handles polymorphism)
  → TargetSyncActorState_Rpc(connection, bytes)
  → Client: DeserializePoco → LoadActorState → OnStateLoaded fires
  → foreach module: OnNetworkSynced()
```

**Requirement:** Module must have `ModuleId` set in inspector to be included in state sync.

### Despawn Flow

```
Server: Despawn(delay)
  → NetworkObject.Despawn()
  → OnStopServer → ExecuteNetworkDespawn() [Cleanup + state change, no pooling]
  → OnStopClient on clients → ExecuteNetworkDespawn()
  → FishNet manages object lifecycle (destroy/deactivate)

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
- `DamageResource(DamageInfo)` — server-authoritative, syncs value via `ObserversRpc`
- `HealResource(ResourceAsset, float)` — server-authoritative
- Hit animations synced via `ObserversRpc` (server decides threshold)
- `OnNetworkSynced()` — refreshes all bars after late-join sync
- Subscribes to `Actor.OnStateLoaded` — refreshes bars after any `LoadActorState` call

### RpgSerializer

FishNet custom serializer for RPG types. Handles:

- `ResourceAsset` ↔ `string Id` (via `RpgManager.GetResourceAssetById`)
- `DamageType` ↔ `string Id`
- `AttributeDefinition`, `StatModifier`, `DamageInfo`, `HitInfo`
- `HitInfo.Hitter` ↔ `NetworkObject`

---

## AI System

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

### Pathfinding

```csharp
PathfindingAgent agent = GetModule<PathfindingAgent>();
agent.SetDestination(targetPosition);

PathFollower follower = GetModule<PathFollower>();
follower.Follow(path);
```

A* implementation. `SurroundSystem` handles multi-enemy positioning around a single target.

---

## Pool Manager

```csharp
// Pool an object (return to pool)
PoolManager.GetContext<PoolManager>().PoolObject(gameObject);

// Spawn from pool (or instantiate if pool empty)
// Typically done via Level/Spawner systems
```

**Rule:** Never call `PoolManager.PoolObject` from FishNet `OnStopServer`/`OnStopClient`. FishNet owns networked object lifecycle. Only use PoolManager in standalone (non-networked) despawn path.

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

---

## New Actor Prefab Checklist (Networked)

1. **Root GameObject:**
    - `Actor` (or subclass) component
    - `NetworkObject` component (FishNet)
    - `NetworkTransform` (position/rotation sync)
    - `NetworkAnimator` (animation sync)
2. **Child components (ActorModules):**
    - `StatsModule` — **`ModuleId = "stats"`**
    - `HealthcareModule` — resource bars wired
    - `CombatModule` — attack patterns configured
    - `MovementModule` — speed attribute linked
    - `AnimationModule` — animator parameter names set
3. **Player actor only:**
    - `DepthsOfVolanActorNetworkBehaviour` — camera + controller binding
4. **AI actor only:**
    - `BTAgent` — behaviour tree assigned
    - `PathfindingAgent` + `PathFollower`
    - `TargetDetectionModule` — detection radius + faction filter

---

## Third-Party Licenses

| Package | License | Commercial Use |
| --- | --- | --- |
| FishNet | Asset Store (free) | ✅ |
| Newtonsoft.Json | MIT | ✅ |
| UniTask | MIT | ✅ |
| Cinemachine | Unity Package | ✅ (Unity license) |
| Unity InputSystem | Unity Package | ✅ (Unity license) |