using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core.Combat;
using Kuantech.Inventory;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.FX
{
    /// <summary>
    /// This module handles the effects that are attached to the character
    /// </summary>
    public class EffectsModule : ActorModule
    {
        #region Shader Defaults
        [Serializable]
        public struct Param
        {
            public enum ParamType { Float, Int, Color }

            public string Property;          // Shader property name (e.g., "_Cutoff", "_TintColor")
            public ParamType Type;
            public float FloatValue;         // Used when Type == Float
            public int IntValue;             // Used when Type == Int
            public Color ColorValue;         // Used when Type == Color
        }

        [Header("Defaults")]
        [Tooltip("Property → default value pairs to apply when resetting.")]
        public List<Param> Defaults = new List<Param>();

        #endregion

        [Header("Pre-defined Effects")]
        [Tooltip("Looked up via PlayExistingEffectById -- swap the Effect on the actor's own asset or a " +
                 "prefab/PlayerHitFx sitting on the equipped ActorVisual without this field caring which.")]
        public string DamageReceiveEffectId;
        public Effect HealEffect;
        public Effect JumpEffect;
        public Effect DodgeEffect;
        public Effect DeathEffect;
        public EffectPlayer AttackEffect;
        private Effect _impact;

        // Static registry: effects/players authored directly on the actor's own prefab (not the swappable
        // ActorVisual) -- registered once at Initialize and never re-scanned.
        [Header("Static Effects (on the actor itself, not the visual)")]
        public List<Effect> ExistingEffects;
        public List<EffectPlayerComponent> ExistingEffectPlayerComponents;

        // Unified registries: populated by RegisterEffect/RegisterEffectPlayer, regardless of whether the
        // source is a static actor-level effect or one that came in with the currently-equipped ActorVisual
        // (see OnActorVisualSet/OnActorVisualRemoved below). Callers (PlayExistingEffectById/ByTag) don't
        // need to know or care which.
        private Dictionary<string, Effect> _effectsById;
        private Dictionary<string, EffectPlayerComponent> _effectPlayersById;

        [Header("Shader Effects")]
        public List<ShaderEffect> ExistingShaderEffects;
        public HashSet<ShaderEffect> ShaderEffects = new HashSet<ShaderEffect>();
        private Dictionary<string, ShaderEffect> _shaderEffectsById = new Dictionary<string, ShaderEffect>();
        public List<Effect> ActiveEffects = new List<Effect>();

        private HealthcareModule _healthcareModule;
        private CombatModule _combatModule;
        private InventoryModule _inventoryModule;

        public override void Initialize()
        {
            base.Initialize();
            Actor.OnHitEvent += OnReceiveDamage;

            _effectsById = new Dictionary<string, Effect>();
            _effectPlayersById = new Dictionary<string, EffectPlayerComponent>();

            foreach (var effect in ExistingEffects)
                RegisterEffect(effect);
            foreach (var player in ExistingEffectPlayerComponents)
                RegisterEffectPlayer(player);

            //Set shader effects
            foreach (var shaderEffect in ExistingShaderEffects)
            {
                if(shaderEffect == null) continue;
                AddShaderEffect(shaderEffect);
            }
        }

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            if(Actor.VisualHandler == null) return;
            ActorVisual actorVisual = Actor.VisualHandler.GetActorVisual();
            if (actorVisual != null)
            {
                // ActorVisualHandler.Initialize() already fired OnActorVisualSet once for a visual baked
                // directly into the prefab (CurrentActorVisual set before this module ever subscribed) --
                // that firing was missed, so run the same registration/detection logic on it now rather than
                // only doing the narrower "detect renderers" half this used to do.
                OnActorVisualSet(actorVisual);
            }
            else
            {
                UpdateShaderEffectRenderers(gameObject);
            }

            Actor.VisualHandler.OnActorVisualSet += OnActorVisualSet;
            Actor.VisualHandler.OnActorVisualRemoved += OnActorVisualRemoved;

            _combatModule = Actor.GetModule<CombatModule>();
            _healthcareModule = Actor.GetModule<HealthcareModule>();
            _inventoryModule = Actor.GetModule<InventoryModule>();

            if (_combatModule != null)
            {
                _combatModule.AttackStartedEvent += AttackStartedEvent;
                _combatModule.AttackCompletedEvent += AttackEndedEvent;
            }

            if (_healthcareModule != null)
            {
                _healthcareModule.OnHealReceived += OnHealReceived;
            }

            if(_inventoryModule != null)
            {
                _inventoryModule.OnItemEquipped += OnItemEquipped;
                _inventoryModule.OnItemUnequipped += OnItemUnequipped;
                _inventoryModule.OnInventoryAttached += OnInventoryAttached;
                _inventoryModule.OnInventoryDetached += OnInventoryDetached;

                // SetInventory can run before this module subscribes (same ordering gotcha as
                // ActorVisualHandler.OnActorVisualSet) -- if an inventory is already bound, catch up now.
                if (_inventoryModule.Inventory != null)
                    RegisterEquippedItemEffects(_inventoryModule.Inventory);
            }
        }

        #region Event Handlers
        private Effect _attackEffect;
        private void AttackStartedEvent(CombatModule cm)
        {
            EffectPlaySettings playSettings = EffectPlaySettings.GetPlayAtPositionSettings(cm.GetAttackPosition(), Quaternion.identity);
            playSettings.Caster = Actor;
            _attackEffect = AttackEffect.PlayEffect(playSettings);
        }

        private void AttackEndedEvent(CombatModule cm)
        {
            if(_attackEffect != null && _attackEffect.Duration < 0) //If attack vfx is looping, we stop it
            {
                _attackEffect.Stop();
            }
            _attackEffect = null;
        }

        private void OnItemEquipped(Item item, EquipmentSlotType slot)
        {
            ItemVisual visual = item.ItemVisual;
            if (visual == null || visual.Effects == null) return;
            foreach (var effect in visual.Effects)
                RegisterEffect(effect);
        }

        private void OnItemUnequipped(Item item)
        {
            ItemVisual visual = item.ItemVisual;
            if (visual == null || visual.Effects == null) return;
            foreach (var effect in visual.Effects)
                UnregisterEffect(effect);
        }

        // OnItemEquipped/OnItemUnequipped only fire for items that go through Inventory.EquipItem/UnequipItem.
        // An item already sitting in Equipment.slotTable when the inventory attaches (e.g. restored from saved
        // state, which calls Equipment.EquipItem directly) never raises those -- so a fresh inventory bind is
        // handled separately here by sweeping every currently-equipped item.
        private void OnInventoryAttached(Kuantech.Inventory.Inventory inventory) => RegisterEquippedItemEffects(inventory);
        private void OnInventoryDetached(Kuantech.Inventory.Inventory inventory) => UnregisterEquippedItemEffects(inventory);

        private void RegisterEquippedItemEffects(Kuantech.Inventory.Inventory inventory)
        {
            if (inventory == null) return;
            foreach (var item in inventory.GetEquippedItems())
            {
                if (item.ItemVisual == null || item.ItemVisual.Effects == null) continue;
                foreach (var effect in item.ItemVisual.Effects)
                    RegisterEffect(effect);
            }
        }

        private void UnregisterEquippedItemEffects(Kuantech.Inventory.Inventory inventory)
        {
            if (inventory == null) return;
            foreach (var item in inventory.GetEquippedItems())
            {
                if (item.ItemVisual == null || item.ItemVisual.Effects == null) continue;
                foreach (var effect in item.ItemVisual.Effects)
                    UnregisterEffect(effect);
            }
        }
        #endregion

        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            base.OnActorStateChanged(oldState, newState);
            if (newState == ActorState.Dead)
            {
                OnDeath();
            }
        }

        /// <summary>
        /// The visual is fully parented under the actor by the time this fires (ActorVisualHandler.
        /// SetActorVisual attaches first, then invokes) -- unlike relying on the visual's own children's
        /// OnEnable, which fires too early (while still parented under the pool, pre-attach) to find this
        /// module via GetComponentInParent. Registers every Effect/EffectPlayerComponent the visual brought
        /// with it, so a swapped-in weapon/character model's FX (e.g. a muzzle flash, PlayerHitFx with its
        /// own Animator wiring) become findable the same way as anything statically on the actor.
        /// </summary>
        public void OnActorVisualSet(ActorVisual actorVisual)
        {
            ApplyDefaults(actorVisual);

            foreach (var effect in actorVisual.GetComponentsInChildren<Effect>(true))
                RegisterEffect(effect);
            foreach (var player in actorVisual.GetComponentsInChildren<EffectPlayerComponent>(true))
                RegisterEffectPlayer(player);
            // A ShaderEffect sitting on the visual (e.g. a hit-flash effect) has to be in ShaderEffects
            // BEFORE UpdateShaderEffectRenderers runs below, or its own MaterialInstances never gets
            // populated -- DetectAllRenderers is only ever called from that sweep, never on registration.
            foreach (var shaderEffect in actorVisual.GetComponentsInChildren<ShaderEffect>(true))
                AddShaderEffect(shaderEffect);

            UpdateShaderEffectRenderers(actorVisual.gameObject);
        }

        /// <summary>Mirrors OnActorVisualSet -- unregisters everything the outgoing visual registered, before
        /// it's pooled. Fired by ActorVisualHandler.ClearCurrentVisual before the visual is actually pooled,
        /// so its hierarchy is still intact to scan.</summary>
        public void OnActorVisualRemoved(ActorVisual actorVisual)
        {
            foreach (var effect in actorVisual.GetComponentsInChildren<Effect>(true))
                UnregisterEffect(effect);
            foreach (var player in actorVisual.GetComponentsInChildren<EffectPlayerComponent>(true))
                UnregisterEffectPlayer(player);
            foreach (var shaderEffect in actorVisual.GetComponentsInChildren<ShaderEffect>(true))
                RemoveShaderEffect(shaderEffect);
        }

        private EffectPlaySettings GetEffectPlaySettings()
        {
            EffectPlaySettings settings = EffectPlaySettings.GetDefaultSettings();
            if (Actor.VisualHandler != null && Actor.VisualHandler.GetActorVisual() != null)
            {
                settings.EffectParent = Actor.VisualHandler.GetActorVisual().transform;
            }

            return settings;
        }
        private void OnReceiveDamage(HitInfo hitInfo)
        {
            PlayExistingEffectById(DamageReceiveEffectId, GetEffectPlaySettings());
        }

        private void OnHealReceived(float heal)
        {
            if (HealEffect != null)
            {
                HealEffect.Play(GetEffectPlaySettings());
            }
        }
        private void OnDodge(object sender, EventArgs args)
        {
            if (DodgeEffect != null)
            {
                DodgeEffect.Play();
            }
        }
        private void OnJump(object sender, EventArgs args)
        {
            if (JumpEffect != null)
            {
                JumpEffect.Play();
            }
        }

        private void OnDeath()
        {
            if (DeathEffect != null)
            {
                DeathEffect.Play();
            }
        }

        public override void ResetModule()
        {
            base.ResetModule();
            if(DeathEffect != null) DeathEffect.Stop();
            GetExistingEffect(DamageReceiveEffectId)?.Stop();

            //Clear active effects
            ClearActiveEffects();

            // Rebuild every registry from scratch rather than trusting incremental Register/Unregister calls to
            // have stayed perfectly in sync -- a pooled actor going through Despawn/Spawn can miss an unequip
            // event (see the despawn-cleanup gotcha with deferred Cleanup coroutines), which would otherwise
            // leave a stale EffectPlayerComponent/Effect reference (pointing at a since-pooled item visual)
            // sitting in the dictionaries for the actor's next life.
            RebuildRegistries();
        }

        /// <summary>
        /// Clears and re-populates _effectsById/_effectPlayersById/_shaderEffectsById from ground truth: the
        /// actor's own static effects, whatever ActorVisual is currently attached, and whatever items are
        /// currently equipped. Safe to call any time -- Register* is idempotent (indexer assignment).
        /// </summary>
        private void RebuildRegistries()
        {
            _effectsById.Clear();
            _effectPlayersById.Clear();
            _shaderEffectsById.Clear();
            ShaderEffects.Clear();

            foreach (var effect in ExistingEffects)
                RegisterEffect(effect);
            foreach (var player in ExistingEffectPlayerComponents)
                RegisterEffectPlayer(player);
            foreach (var shaderEffect in ExistingShaderEffects)
            {
                if (shaderEffect == null) continue;
                AddShaderEffect(shaderEffect);
            }

            ActorVisual actorVisual = Actor.VisualHandler != null ? Actor.VisualHandler.GetActorVisual() : null;
            if (actorVisual != null)
                OnActorVisualSet(actorVisual);

            RegisterEquippedItemEffects(_inventoryModule?.Inventory);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            ClearActiveEffects();
        }

        #region Registration
        // Keyed by whatever id/tag the component carries. Later registrations for the same key win (e.g. an
        // equipped visual's PlayerHitFx overriding a static fallback with the same id) -- the same
        // last-write-wins behaviour the old ActorVisual rebuild already had.
        public void RegisterEffect(Effect effect)
        {
            if (effect == null || string.IsNullOrEmpty(effect.EffectId)) return;
            _effectsById[effect.EffectId] = effect;
        }

        public void UnregisterEffect(Effect effect)
        {
            if (effect == null || string.IsNullOrEmpty(effect.EffectId)) return;
            if (_effectsById.TryGetValue(effect.EffectId, out var current) && current == effect)
                _effectsById.Remove(effect.EffectId);
        }

        public void RegisterEffectPlayer(EffectPlayerComponent player)
        {
            if (player == null || player.EffectPlayer == null) return;

            // GetEffectId(), not the raw EffectId field -- it falls back to EffectPrefab.EffectId / Effect.EffectId
            // when EffectId itself is left blank (e.g. a player that just references an Effect/EffectPrefab
            // directly instead of duplicating its id as a string).
            string id = player.EffectPlayer.GetEffectId();
            if (string.IsNullOrEmpty(id)) return;

            if (_effectPlayersById.TryGetValue(id, out var existing) && existing != player)
                Debug.LogWarning($"[EffectsModule] Duplicate EffectPlayerComponent id '{id}' on {Actor?.name} -- overwriting.");

            _effectPlayersById[id] = player;
        }

        public void UnregisterEffectPlayer(EffectPlayerComponent player)
        {
            if (player == null || player.EffectPlayer == null) return;

            string id = player.EffectPlayer.GetEffectId();
            if (!string.IsNullOrEmpty(id) &&
                _effectPlayersById.TryGetValue(id, out var currentById) && currentById == player)
                _effectPlayersById.Remove(id);
        }
        #endregion

        #region Fx Players
        public Effect GetExistingEffect(string effectId)
        {
            if (effectId.IsNullOrEmpty()) return null;
            if (_effectsById.ContainsKey(effectId)) return _effectsById[effectId];
            return null;
        }

        public EffectPlayerComponent GetEffectPlayerById(string id)
        {
            if(_effectPlayersById.TryGetValue(id, out var value)) return value;
            return null;
        }

        /// <summary>
        /// The generalized entry point for "play whatever's registered under this id" -- doesn't care whether
        /// it's a standalone Effect (its own Animator/VFX/SFX bundle, e.g. PlayerHitFx) or an EffectPlayerComponent
        /// socket (e.g. a weapon's muzzle point). Effect takes priority since it's the more specific instance;
        /// EffectPlayerComponent is the generic fallback.
        /// </summary>
        public Effect PlayExistingEffectById(string id, EffectPlaySettings? settings = null)
        {
            if (string.IsNullOrEmpty(id)) return null;
            EffectPlaySettings resolvedSettings = settings ?? EffectPlaySettings.GetDefaultSettings();

            Effect existing = GetExistingEffect(id);
            if (existing != null)
            {
                resolvedSettings.DespawnAfterPlay = false; // bound to the actor/item -- don't despawn
                existing.Play(resolvedSettings);
                return existing;
            }

            return GetEffectPlayerById(id)?.PlayEffect(resolvedSettings);
        }
        #endregion

        #region Shader Effects

        public ShaderEffect GetShaderEffect(string effectId)
        {
            if (_shaderEffectsById.ContainsKey(effectId)) return _shaderEffectsById[effectId];
            return null;
        }

        public void PlayShaderEffect(string shaderEffect)
        {
            ShaderEffect effect = GetShaderEffect(shaderEffect);
            if (effect == null) return;
            effect.PlayShaderEffect();
        }

        public void StopShaderEffect()
        {

        }

        /// <summary>
        /// Adds a shader effect
        /// </summary>
        /// <param name="shaderEffect"></param>
        public void AddShaderEffect(ShaderEffect shaderEffect)
        {
            if (shaderEffect == null) return;
            // Indexer, not .Add -- OnModulesInitialized can register the same pre-existing visual's shader
            // effects once itself and then again via the OnActorVisualSet subscription in edge cases; .Add
            // would throw on the second registration of the same id.
            if (!string.IsNullOrEmpty(shaderEffect.EffectId))
            {
                _shaderEffectsById[shaderEffect.EffectId] = shaderEffect;
            }
            // No reparenting -- a ShaderEffect can live on the actor itself (ExistingShaderEffects) or on the
            // swappable ActorVisual (e.g. FlashSpriteShaderEffect), and needs to stay wherever it already is
            // for the latter case (pooled/destroyed with the visual, not orphaned onto the actor root).
            ShaderEffects.Add(shaderEffect);
        }

        public void RemoveShaderEffect(ShaderEffect shaderEffect)
        {
            if (shaderEffect == null) return;
            if (!string.IsNullOrEmpty(shaderEffect.EffectId) && _shaderEffectsById.ContainsKey(shaderEffect.EffectId))
            {
                _shaderEffectsById.Remove(shaderEffect.EffectId);
            }

            ShaderEffects.Remove(shaderEffect);
        }

        public void UpdateShaderEffectRenderers(GameObject renderersParent)
        {
            foreach (var shaderEffect in ShaderEffects)
            {
                shaderEffect.DetectAllRenderers(renderersParent);
            }
        }

        #endregion

        #region Runtime Attached Effects

        /// <summary>
        /// Plays an effect on the actor
        /// </summary>
        /// <param name="effectPlayer"></param>
        public Effect PlayEffectOnActor(EffectPlayer effectPlayer, Vector3 localPos, Quaternion effectRotation)
        {
            EffectPlaySettings playSettings = EffectPlaySettings.GetPlayAtObjectSettings(Actor.transform, localPos, effectRotation);

            //Does the effect is already on the actor?
            Effect existingEffect = GetExistingEffect(effectPlayer.GetEffectId());
            if (existingEffect != null)
            {
                return PlayExistignEffect(existingEffect.EffectId);
            }

            //Effect isnt in the existing effects
            Effect effect = effectPlayer.PlayEffect(playSettings);
            if (effect == null) return null;
            AddActiveEffect(effect);
            return effect;
        }

        /// <summary>
        /// Plays an existing effect
        /// </summary>
        /// <param name="effectId"></param>
        /// <returns></returns>
        public Effect PlayExistignEffect(string effectId)
        {
            Effect existingEffect = GetExistingEffect(effectId);
            if (existingEffect == null) return null;
            existingEffect.Play();
            return existingEffect;
        }

        /// <summary>
        /// Adds an active effect
        /// </summary>
        /// <param name="effect"></param>
        public void AddActiveEffect(Effect effect)
        {
            if (ActiveEffects == null) ActiveEffects = new List<Effect>();
            effect.OwnerEffectModule = this;
            ActiveEffects.Add(effect);
        }

        /// <summary>
        /// Stops an active effect
        /// </summary>
        /// <param name="effect"></param>
        public void StopActiveEffect(Effect effect)
        {
            RemoveActiveEffect(effect);

            effect.Stop();
        }

        public void ClearActiveEffects()
        {
            foreach (var activeFx in ActiveEffects)
            {
                activeFx.Cleanup();
            }
            ActiveEffects.Clear();
        }

        public void RemoveActiveEffect(Effect effect)
        {
            if (!ActiveEffects.IsNullOrEmpty() && ActiveEffects.Contains(effect) && effect.OwnerEffectModule == this)
            {
                ActiveEffects.Remove(effect);
                effect.OwnerEffectModule = null;
            }
        }
        #endregion

        /// <summary>
        /// Applies default values to all configured properties.
        /// </summary>
        public void ApplyDefaults(ActorVisual visual)
        {
            var renderers = visual != null ? visual.GetComponentsInChildren<Renderer>() : GetComponentsInChildren<Renderer>();


            if (Defaults == null || Defaults.Count == 0 || renderers.IsNullOrEmpty())
                return;
            foreach (var renderer in renderers)
            {
                // Directly modify materials (this can instantiate them if you access .materials)
                for(int i=0;i<renderer.materials.Length;++i)
                {
                    ApplyParamsToMaterial(renderer.materials[i]);
                }

            }
        }

        private void ApplyParamsToMaterial(Material mat)
        {
            if (mat == null) return;

            foreach (var p in Defaults)
            {
                if (string.IsNullOrEmpty(p.Property)) continue;
                if (!mat.HasProperty(p.Property)) continue;
                switch (p.Type)
                {

                    case Param.ParamType.Float:
                        mat.SetFloat(p.Property, p.FloatValue);
                        break;
                    case Param.ParamType.Int:
                        // Material.SetInt exists widely; fallback to SetFloat if needed
                        mat.SetInt(p.Property, p.IntValue);
                        break;
                    case Param.ParamType.Color:
                        mat.SetColor(p.Property, p.ColorValue);
                        break;
                }
            }
        }
    }
}
