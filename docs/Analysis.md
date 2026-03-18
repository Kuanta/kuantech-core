# KuantechCore Analysis

## Architecture Overview

The core is built around a **GameManager + SubManager** pattern.
- `GameManager` is a persistent singleton that orchestrates the game lifecycle.
- `SubManager` is the base class for all subsystems (inventory, audio, AI, etc.).
- Two types of SubManagers: **persistent** (DontDestroyOnLoad) and **scene-specific** (cleaned up on scene change).

---

## GameManager.cs

**Responsibilities:**
- Singleton lifecycle (Awake, DontDestroyOnLoad)
- Initializes persistent SubManagers on start (async, parallel via UniTask.WhenAll)
- Scene management: ChangeScene, OnSceneLoaded, OnNewScene
- Manages scene-specific SubManagers via `SceneSubManagerContainer`
- Loading screen toggling
- Game pause/resume (Time.timeScale)

**Notable Design Decisions:**
- SubManagers are initialized in **parallel** (UniTask.WhenAll) — good for performance, but SubManagers must not depend on each other during Initialize()
- Scene-specific SubManagers are discovered via `FindObjectOfType<SceneSubManagerContainer>()` — works but tight coupling to scene structure
- `LevelTransitionData` is an abstract class, meant to be subclassed per transition type — clean pattern

**Issues / Smells:**
- `GetSubManagerByType<T>()` returns `SubManager` instead of `T` — callers must cast manually (GetContext<T> handles this but the base method is weakly typed)
- `ToggleSubManager<T>` has no generic constraint (`where T : SubManager`) — can compile with wrong types
- `OnNewScene()` calls `ResumeGame()` unconditionally — could be problematic if a scene intentionally loads paused
- `ChangeScene()` is a static method but accesses instance — fine but inconsistent style with the rest

---

## SubManager.cs

**Responsibilities:**
- Base class for all subsystems
- Lifecycle hooks: Initialize, OnSubmanagersInitialized, OnSceneEntry, OnSceneLeave, Cleanup
- State persistence: LoadState, SaveState, ClearState, Serialize, Deserialize
- `LoadAfterInitialize` flag — auto-loads saved state after all managers are initialized

**Notable Design Decisions:**
- `GetContext<T>()` static helper is convenient for cross-manager communication
- `DataStorageProviderId` suggests pluggable storage backends — good for flexibility
- Implements `ISaveable` — consistent save/load contract

**Issues / Smells:**
- `Deserialize(byte[] data)` is empty — not implemented, silent failure risk
- `LoadState()` and `SaveState()` both have `if (this is GameStateManager) return;` — base class should not know about a specific subclass. This is a violation of the Dependency Inversion Principle. GameStateManager should override these methods instead.
- `Serialize()` returns null by default — subclasses that forget to override will silently save nothing

---

## Summary

Solid foundational architecture. The GameManager/SubManager split is clean and extensible.
The main issues are:
1. `GameStateManager` referenced in base class (SubManager) — should be inverted
2. `Deserialize` not implemented
3. Minor type safety issues in GameManager
4. Scene loading always resumes game — may need a flag

---

## Save System — Full Analysis

### How the Current (Old) System Works

**The chain (bottom-up push):**

```
ActorModule.CreateModuleState()
    → Actor.GetActorState()           (collects all module states by ModuleId)
        → WorldManager serializes actors
            → GameStateManager.UpdateSaveData(worldManager)
                → SaveUtility.Serialize(ISaveable)
                    → SaveableData.ToBytes()
                        → GameState.UpdateData(id, bytes)
                            → GameState.SaveData() → disk/PlayerPrefs
```

Each layer **pushes** its data upward. Upper layers don't know what's inside lower layers — they just call `Serialize()` and get back a `byte[]`. This is what makes it context-blind.

**ISaveable + SaveUtility:**
- `ISaveable` defines `Serialize()` (manual) and `Deserialize()` (manual)
- `SaveUtility` uses reflection to auto-serialize fields marked with `[SaveableField]`
- Both field-based and manual data live in `SaveableData` (FieldData + ManualData)
- Nested `ISaveable` fields are handled recursively

**GameStateManager:**
- Holds a `GameState` (dictionary: `string id → byte[]`)
- Key = `Type.FullName` for SubManagers
- Dirty flag + frequency check for auto-save in LateUpdate
- `SubManager.SaveState()` → `GameStateManager.UpdateSaveData(this)` — the trigger is always bottom-up

**What works well:**
- Completely generic — new SubManagers just add `[SaveableField]` and it works
- No boilerplate per-system
- Works for mobile, single-player, offline games

---

### The Multiplayer Problem

For a server-authoritative multiplayer game, this model breaks down:

| Old model | Multiplayer need |
|---|---|
| SubManager owns its data, pushes binary blob | Server owns the data, client must query |
| `SaveState()` = write to local disk | `SaveState()` = POST to server endpoint |
| `LoadState()` = read from local file | `LoadState()` = GET from server endpoint |
| Binary bytes are the transport | Structured queries (REST, WebSocket, etc.) |

The push direction must invert: instead of **"I have data → let me save it"**, it becomes **"I need data → give it to me from wherever it lives"**.

---

### The New System (In Progress — Partially Split)

The idea: decouple **what** is saved from **where/how** it's saved via pluggable providers.

There are actually **two parallel starts** that haven't been connected yet:

**1. `IStorageProvider` / `BaseStorageProvider` (in State/):**
```
IStorageProvider
  ProviderId: string
  Initialize()
  SaveChanges()
  Load()
  Clear()

BaseStorageProvider : MonoBehaviour, IStorageProvider  ← abstract, _providerId in Inspector
```
Generic contract. No knowledge of SubManagers. Think of it as a key-value store backend.

**2. `DataStorageProvider` (in Storage/):**
```
DataStorageProvider : MonoBehaviour
  Id: string
  LoadSubManager(SubManager) → bool  ← empty
  SaveSubManager(SubManager) → bool  ← empty
```
SubManager-aware. But doesn't implement `IStorageProvider`. Made at a different time, not connected.

**`GameState.cs`:**
- Simple `Dictionary<string, byte[]>` in memory
- Reads/writes a single `gameState.bin` file
- Has `Dirtied` flag + periodic flush in `GameStateManager.LateUpdate`
- This is the local file provider logic in disguise — will likely be absorbed into a `LocalFileStorageProvider`

**GameStateManager — NEW SAVE SYSTEM region:**
- `SaveSubmanager(SubManager sub)` → finds provider by `sub.DataStorageProviderId` → calls `provider.SaveSubManager(sub)`
- `GetDataStorageProvider(string id)` → always returns null (TODO comment in code)
- The routing logic is there, the registry isn't

---

### Current State of the Migration

```
Old path (works):  SubManager.SaveState() → GameStateManager.UpdateSaveData() → GameState dict → .bin file
New path (broken): SubManager.DataStorageProviderId → GameStateManager → DataStorageProvider → ???
```

The two paths coexist but the new one is not functional yet.

---

### What Needs to Be Reconciled

1. **Two provider concepts, one needed:** `DataStorageProvider` (Storage/) and `BaseStorageProvider` (State/) need to be merged into a single abstract base. `BaseStorageProvider` has the better foundation (abstract class + interface).

2. **Provider ↔ SubManager contract:** `BaseStorageProvider` currently has no SubManager-specific methods. The bridge is missing: a provider needs a way to save/load a specific SubManager's data by key. Options:
   - Provider is pure key-value (`Save(key, bytes)` / `Load(key) → bytes`) and SubManager handles its own serialization with `SaveUtility`
   - Provider is SubManager-aware (`SaveSubManager(SubManager)`) and decides how to serialize

3. **GameState absorption:** Once a `LocalFileStorageProvider` exists, `GameState.cs` becomes redundant — its `_loadedData` dict and file I/O move into the provider.

4. **GameStateManager role shifts:** Instead of owning a `GameState`, it becomes a **provider registry** — SubManagers ask it for the right provider, then interact with it directly.

5. **Multiplayer implication:** A `ServerStorageProvider` would need SubManagers to expose a typed DTO (not binary) because you can't POST binary blobs to a REST API. SubManagers may need a second contract alongside `ISaveable`.

---

### Proposed Direction (to discuss)

```
SubManager.DataStorageProviderId → GameStateManager (provider registry)
    → IStorageProvider.Save(key, bytes)   ← SubManager still owns serialization via SaveUtility
    → IStorageProvider.Load(key) → bytes

Concrete providers:
  LocalFileStorageProvider  ← replaces GameState, same binary format
  PlayerPrefsStorageProvider
  ServerStorageProvider     ← needs DTO contract, different serialization path
```

SubManagers stay unchanged for local storage. Server storage requires additional work per SubManager.

Next areas to explore: `SceneSubManagerContainer`, `ActorBlueprint`, `ActorSerializableData`
