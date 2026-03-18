# Main Systems

- GameManagers [CORE]
- Game state (saving, loading) [CORE]
- Actor system [CORE]
- Movement
- Camera system
- Actor animations
- Controllers & player input
- Combat
- Rpg systems
- Skill system
- Inventory
- World management
- AI systems
- World management
- Quest systems
- UI 
- Networking (!!!)
- Effects (vfx and audio)
- Utilities

## Movement

**Scripts:** `Scripts/Movement/`

### Sınıflar
| Sınıf | Rol |
|---|---|
| `MovementModule` | Beyin — hız, jump, crouch, dash, knockback, lock yönetimi |
| `RigidbodyMovementModule` | Fizik motoru — MovementModule'den okuyup Rigidbody'e yazar |
| `NavmeshMovementModule` | AI motoru — NavMeshAgent'ı yönetir, MovementModule'e geri yazar |
| `AutoMoverModule` | Waypoint/spline/path takibi — MovementModule'den bağımsız çalışır |
| `CrouchHandler` | Crouch davranışını MovementModule'den ayıran handler |
| `DashHandler` | Dash davranışını MovementModule'den ayıran handler |

### Veri Akışı
```
PLAYER:  Input → MovementModule.SetMovementVector()
                 → RigidbodyMovementModule okur → Rigidbody.velocity

AI:      NavmeshMovementModule.GoToPosition() → NavMeshAgent
                 → agent velocity → NavmeshMovementModule → MovementModule.SetMovementVector() (animasyon için)

AUTO:    AutoMoverModule → WaypointFollower / SplineFollower / PathFollower (transform doğrudan)
```

`MovementModule` merkez veri deposu: hız, lock, knockback vector burada tutulur.
`MotionVectorsHandler` (Actor üzerinde) ise ham vektörlerin deposu — her iki taraf da buradan okur/yazar.

### Sorunlar
- **Dodge vs Dash:** Dash `MovementModule`'de (DashHandler ile), Dodge `RigidbodyMovementModule`'de — aynı konsept iki farklı yerde, birbirinden habersiz.
- **Çift lock:** `RigidbodyMovementModule._movementLocked` (bool) + `MovementModule.MovementLock` (LockVariable) — iki ayrı lock sistemi senkronize değil.
- **Update bypass:** `RigidbodyMovementModule` ve `NavmeshMovementModule` Unity'nin kendi `Update/FixedUpdate`'ini kullanıyor, `ModuleUpdate/ModuleFixedUpdate`'i değil — Actor'ün `!Initialized` guard'ı işlemiyor.
- **AutoMoverModule kopuk:** `MovementModule`'ü hiç kullanmıyor, animasyon senkronizasyonu yok.
- **JumpHandler delegate:** `MovementModule.Jump()` → `JumpHandler` null ise warning. Fizik implementasyonu olmadan jump çağrılırsa sessizce başarısız olur.

### Networking Notu
- `HandleMovement()` her frame lokalde çalışıyor — server-authoritative mimaride bu client-side prediction gerektirir.
- `NavmeshMovementModule.GoToPosition()` RPC olacak (server navmesh'i yönetir).
- Knockback iki farklı modülde ayrı handle ediliyor — multiplayer'da tek noktadan senkronize edilmeli.