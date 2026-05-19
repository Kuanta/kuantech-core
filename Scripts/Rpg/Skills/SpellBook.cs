using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
using Kuantech.Core.Combat;
using Kuantech.Core.Utils;
using Kuantech.Rpg.Managers;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

#if NETWORKING_FISHNET
    using FishNet.Object;
#endif

namespace Kuantech.Rpg.Skills
{
    /// <summary>
    /// Carries the skill ID list for state sync (late-join) and spawn data.
    /// Skills are looked up via RpgManager on load — no ScriptableObject references needed.
    /// Set ModuleId to match the SpellBook component's ModuleId field.
    /// </summary>
    [System.Serializable]
    public class SpellBookSerializableData : ActorModuleSerializableData
    {
        public List<string> SkillIds = new();
    }

    #if NETWORKING_FISHNET
    /// <summary>
    /// RPC-safe version of ActionCastData. Actor.Target replaced with NetworkObject
    /// because FishNet cannot serialize MonoBehaviour/UnityEngine.Object references.
    /// </summary>
    [System.Serializable]
    public struct SkillCastRpcData
    {
        public string SkillId;
        public Vector3 StartPosition;
        public Vector3 Direction;
        public Vector3 TargetPosition;
        public NetworkObject Target; //todo: Fix this, wont work in single player games

        public static SkillCastRpcData From(string skillId, ActionCastData cast)
        {
            return new SkillCastRpcData
            {
                SkillId         = skillId,
                StartPosition   = cast.StartPosition,
                Direction       = cast.Direction,
                TargetPosition  = cast.TargetPosition,
                Target          = cast.Target != null ? cast.Target.GetComponent<FishNet.Object.NetworkObject>() : null,
            };
        }

        public ActionCastData ToActionCastData()
        {
            return new ActionCastData
            {
                StartPosition  = StartPosition,
                Direction      = Direction,
                TargetPosition = TargetPosition,
                Target         = Target != null ? Target.GetComponent<Actor>() : null,
            };
        }
    }
    #endif

    public class SpellBook : ActorModule
    {
        [Header("Positionings")]
        public string DefaultCastSlotName;

        [Header("Default Skills")]
        [Tooltip("Skills added here are given to the actor at Initialize — useful for player prefabs.")]
        public List<SkillDataAsset> DefaultSkills;

        [Header("Default Passive Skills")]
        public List<PassiveSkillDataAsset> DefaultPassiveSkills;

        [Header("Lock")]
        public LockKey SkillLockKey;

        // Fired on the local client when a skill cast ends (for rotation hold, UI, etc.)
        public UnityAction<Skill> SkillCastEndedEvent;

        private Dictionary<string, Skill> _skills = new Dictionary<string, Skill>();
        private List<Skill> _activeSkills = new();
        private Dictionary<string, List<SkillDataAsset>> _skillSourcesByFamily = new();

        private Dictionary<string, PassiveSkill> _passiveSkills = new();
        private Dictionary<string, List<PassiveSkillDataAsset>> _passiveSkillSourcesByFamily = new();

        //Runtime
        private HealthcareModule _healthcareModule;
        private LockModule _lockModule;
        private bool _modulesInitialized;


        public override void Initialize()
        {
            base.Initialize();
            if (DefaultSkills != null)
                foreach (var asset in DefaultSkills)
                    if (asset != null) AddSkill(asset);

            if (DefaultPassiveSkills != null)
                foreach (var asset in DefaultPassiveSkills)
                    if (asset != null) AddPassiveSkill(asset);
        }

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _healthcareModule = Actor.GetModule<HealthcareModule>();
            _lockModule = Actor.GetModule<LockModule>();
            if(_lockModule != null)
            {
                _lockModule.OnLocked += OnLockHandler;
            }

            _modulesInitialized = true;
            foreach (var passive in _passiveSkills.Values)
                passive.Activate();
        }

        public override void ModuleUpdate()
        {
            base.ModuleUpdate();
            if (!Actor.IsAlive()) return;

            for (int i = _activeSkills.Count - 1; i >= 0; i--)
            {
                Skill skill = _activeSkills[i];
                if (skill.IsCasting())
                    skill.UpdateSkill(Time.deltaTime);
                else
                    _activeSkills.RemoveAt(i);
            }

            foreach (var passive in _passiveSkills.Values)
                passive.Update(Time.deltaTime);
        }

        #region Slot

        public Vector3 GetDefaultCastPosition()
        {
            ActorSlotsHandler slotsHander = Actor.GetModule<ActorSlotsHandler>();
            if(slotsHander != null)
            { 
                Transform slot = slotsHander.GetSlot(DefaultCastSlotName);
                if (slot != null) return slot.position;
            }
            return Actor.transform.position;
        }

        #endregion
        
        #region Skill Management
        
        public bool IsCastingSkill()
        {
            return _activeSkills.Any(skill => skill.IsCasting());
        }
        
        public Skill[] GetSkills()
        {
            return _skills.Values.ToArray();
        }
        
        public Skill AddSkill(SkillDataAsset skillAsset)
        {
            if (HasSkill(skillAsset)) return null;
            Skill skill = new Skill();
            skill.Initialize(this, skillAsset);
            _skills[skillAsset.SkillId] = skill;
            return skill;
        }

        public void RemoveSkill(SkillDataAsset skillAsset)
        {
            if (skillAsset != null) _skills.Remove(skillAsset.SkillId);
        }

        public bool HasSkill(SkillDataAsset skilLDataAsset)
        {
            Skill skill = GetSkillByDataAsset(skilLDataAsset);
            if (skill == null) return false;
            return true;
        }

        // Rank-aware registration — multiple sources per family; active skill is always the highest rank.
        public void RegisterSkill(SkillDataAsset asset)
        {
            if (asset == null) return;
            string family = GetFamilyId(asset);
            if (!_skillSourcesByFamily.TryGetValue(family, out var sources))
            {
                sources = new List<SkillDataAsset>();
                _skillSourcesByFamily[family] = sources;
            }
            if (!sources.Contains(asset)) sources.Add(asset);
            RefreshBestSkillForFamily(family);
        }

        public void UnregisterSkill(SkillDataAsset asset)
        {
            if (asset == null) return;
            string family = GetFamilyId(asset);
            if (!_skillSourcesByFamily.TryGetValue(family, out var sources)) return;
            sources.Remove(asset);
            RefreshBestSkillForFamily(family);
        }

        private static string GetFamilyId(SkillDataAsset asset)
            => !string.IsNullOrEmpty(asset.BaseSkillId) ? asset.BaseSkillId : asset.SkillId;

        private void RefreshBestSkillForFamily(string family)
        {
            if (!_skillSourcesByFamily.TryGetValue(family, out var sources)) return;

            // Find which skill (if any) is currently active for this family
            string currentActiveId = null;
            foreach (var pair in _skills)
            {
                if (GetFamilyId(pair.Value.SkillDataAsset) == family)
                {
                    currentActiveId = pair.Key;
                    break;
                }
            }

            if (sources.Count == 0)
            {
                if (currentActiveId != null) _skills.Remove(currentActiveId);
                return;
            }

            // Pick highest rank
            SkillDataAsset best = null;
            foreach (var s in sources)
                if (best == null || s.Rank > best.Rank) best = s;

            if (currentActiveId == best.SkillId) return; // already correct

            if (currentActiveId != null) _skills.Remove(currentActiveId);
            if (!_skills.ContainsKey(best.SkillId)) AddSkill(best);
        }
        #endregion


        #region Passive Skill Management

        public PassiveSkill AddPassiveSkill(PassiveSkillDataAsset dataAsset)
        {
            if (dataAsset == null || _passiveSkills.ContainsKey(dataAsset.SkillId)) return null;
            var passive = new PassiveSkill();
            passive.Initialize(this, dataAsset);
            _passiveSkills[dataAsset.SkillId] = passive;
            return passive;
        }

        public void RemovePassiveSkill(string skillId)
        {
            if (!_passiveSkills.TryGetValue(skillId, out var passive)) return;
            passive.Deactivate();
            _passiveSkills.Remove(skillId);
        }

        public void RemovePassiveSkill(PassiveSkillDataAsset dataAsset)
            => RemovePassiveSkill(dataAsset.SkillId);

        public PassiveSkill GetPassiveSkill(string skillId)
        {
            _passiveSkills.TryGetValue(skillId, out var passive);
            return passive;
        }

        public bool HasPassiveSkill(string skillId) => _passiveSkills.ContainsKey(skillId);
        public bool HasPassiveSkill(PassiveSkillDataAsset dataAsset) => HasPassiveSkill(dataAsset.SkillId);

        // Rank-aware registration — mirrors the active skill RegisterSkill pattern.
        public void RegisterPassiveSkill(PassiveSkillDataAsset asset)
        {
            if (asset == null) return;
            string family = GetPassiveFamilyId(asset);
            if (!_passiveSkillSourcesByFamily.TryGetValue(family, out var sources))
            {
                sources = new List<PassiveSkillDataAsset>();
                _passiveSkillSourcesByFamily[family] = sources;
            }
            if (!sources.Contains(asset)) sources.Add(asset);
            RefreshBestPassiveSkillForFamily(family);
        }

        public void UnregisterPassiveSkill(PassiveSkillDataAsset asset)
        {
            if (asset == null) return;
            string family = GetPassiveFamilyId(asset);
            if (!_passiveSkillSourcesByFamily.TryGetValue(family, out var sources)) return;
            sources.Remove(asset);
            RefreshBestPassiveSkillForFamily(family);
        }

        private static string GetPassiveFamilyId(PassiveSkillDataAsset asset)
            => !string.IsNullOrEmpty(asset.BaseSkillId) ? asset.BaseSkillId : asset.SkillId;

        private void RefreshBestPassiveSkillForFamily(string family)
        {
            if (!_passiveSkillSourcesByFamily.TryGetValue(family, out var sources)) return;

            // Find currently active passive for this family
            string currentActiveId = null;
            foreach (var pair in _passiveSkills)
            {
                if (GetPassiveFamilyId(pair.Value.DataAsset) == family)
                {
                    currentActiveId = pair.Key;
                    break;
                }
            }

            if (sources.Count == 0)
            {
                if (currentActiveId != null)
                {
                    _passiveSkills[currentActiveId].Deactivate();
                    _passiveSkills.Remove(currentActiveId);
                }
                return;
            }

            // Pick highest rank
            PassiveSkillDataAsset best = null;
            foreach (var s in sources)
                if (best == null || s.Rank > best.Rank) best = s;

            if (currentActiveId == best.SkillId) return; // already correct

            // Deactivate and remove current
            if (currentActiveId != null)
            {
                _passiveSkills[currentActiveId].Deactivate();
                _passiveSkills.Remove(currentActiveId);
            }

            // Add new best and activate if modules are ready
            PassiveSkill newPassive = AddPassiveSkill(best);
            if (newPassive != null && _modulesInitialized)
                newPassive.Activate();
        }

        #endregion

        #region Queries

        public bool HasSkill(string skillId)
        {
            Skill skill = GetSkillById(skillId);
            return skill != null;
        }

        public Skill GetSkillByDataAsset(SkillDataAsset skillDataAsset)
        {
            return GetSkillById(skillDataAsset.SkillId);
        }

        public Skill GetSkillById(string skillId)
        {
            if (_skills.IsNullOrEmpty() || !_skills.ContainsKey(skillId)) return null;
            return _skills[skillId];
        }
        public bool CanCastSkill(SkillDataAsset skillDataAsset, ActionCastData skillCastData)
        {
            if (!IsSkillReady(skillDataAsset)) return false;
            Skill skill = GetSkillByDataAsset(skillDataAsset);
            return skill.CanBeCast(skillCastData);
        }

        /// <summary>
        /// Checks cooldown, skill availability and resource availability
        /// </summary>
        /// <param name="skillDataAsset"></param>
        /// <returns></returns>
        public bool IsSkillReady(SkillDataAsset skillDataAsset)
        {
            if (!HasSkill(skillDataAsset)) return false;
            
            Skill skill = GetSkillByDataAsset(skillDataAsset);
            if (!skill.IsSkillReady()) return false;

            //Check resource
            if (_healthcareModule != null && skillDataAsset.RequiredResource != null)
            {
                if (_healthcareModule.GetCurrentResource(skillDataAsset.RequiredResource) < skillDataAsset.RequiredResourceAmount) return false;
            }

            return true;
        }

        #endregion

        #region Lock
        public bool IsLocked()
        {
            if(_lockModule == null) return false;
            return _lockModule.IsLocked(SkillLockKey);
        }

        public void Lock(object locker)
        {
            if(_lockModule == null) return;
            _lockModule.Lock(SkillLockKey, locker);
        }

        public void Unlock(object locker)
        {
            if(_lockModule == null) return;
            _lockModule.Unlock(SkillLockKey, locker);
        }   

        private void OnLockHandler(string lockKey)
        {
            if(lockKey != SkillLockKey.LockId) return;
            foreach(var skill in _activeSkills)
            {
                skill.EndCast();
            }
            _activeSkills.Clear();
        }
        #endregion

        #region Commands
        public bool CastSkill(string skillId, ActionCastData skillCastData)
        {
            Skill skillToCast = GetSkillById(skillId);
            return CastSkill(skillToCast, skillCastData);
        }

        public bool CastSkill(Skill skillToCast, ActionCastData skillCastData)
        {
            if (skillToCast == null || skillToCast.SkillDataAsset == null) return false;
            return CastSkill(skillToCast.SkillDataAsset, skillCastData);
        }

        public bool CastSkill(SkillDataAsset skillDataAsset, ActionCastData skillCastData)
        {
            if (!IsServerInitialized && IsSpawned)
            {
#if NETWORKING_FISHNET
                // Client: send to server, server drives the full lifecycle via RPCs
                SkillCastRpcData rpcData = SkillCastRpcData.From(skillDataAsset.SkillId, skillCastData);
                ServerCastSkill_Rpc(rpcData);
#endif
                return true;
            }
            return ExecuteCastSkill(skillDataAsset, skillCastData);
        }
        #endregion

        #region Execution

        public bool ExecuteCastSkill(SkillDataAsset skillDataAsset, ActionCastData skillCastData)
        {
            if(!IsServerInitialized) return false; //For now execute only at server
            if (IsLocked()) return false;
            if (!CanCastSkill(skillDataAsset, skillCastData)) return false;
            Skill skillToCast = GetSkillByDataAsset(skillDataAsset);
            if (skillToCast == null) return false;
            if (!_activeSkills.Contains(skillToCast))
            {
                _activeSkills.Add(skillToCast);
            }

            //Spend resource
            if (_healthcareModule != null && skillDataAsset.RequiredResource != null)
            {
                _healthcareModule.RemoveResource(skillDataAsset.RequiredResource, skillDataAsset.RequiredResourceAmount);
            }

            //Turn towards skill direction?
            if (skillCastData.OverrideRotation)
            {
                if (skillCastData.Target != null)
                    Actor.MotionVectorsHandler.SetTargetObject(skillCastData.Target.transform);
                else
                    Actor.MotionVectorsHandler.SetTargetVector(skillCastData.Direction);
            }
            return skillToCast.Cast(skillCastData);
        }
        #endregion

        public override void Cleanup()
        {
            base.Cleanup();
            _skills.Clear();
            _skillSourcesByFamily.Clear();
            foreach (var passive in _passiveSkills.Values)
                passive.Deactivate();
            _passiveSkills.Clear();
            _passiveSkillSourcesByFamily.Clear();
        }

        protected override ActorModuleSerializableData InstantiateState()
        {
            return new SpellBookSerializableData
            {
                SkillIds = _skills.Keys.ToList(),
            };
        }

        public override void LoadState(ActorModuleSerializableData serializableData)
        {
            base.LoadState(serializableData);
            if (serializableData is not SpellBookSerializableData data) return;
            foreach (string skillId in data.SkillIds)
            {
                SkillDataAsset asset = RpgManager.GetSkillDataAssetById(skillId);
                if (asset != null) AddSkill(asset);
                else Debug.LogWarning($"[SpellBook] LoadState: skill '{skillId}' not found in RpgManager.");
            }
        }
        

        #region Events

        public virtual void OnSkillCastStarted(Skill skill)
        {
            #if NETWORKING_FISHNET
            //CurrentlyCastedSkill = skill;
            if(IsServerInitialized)
            {
                ObserversSkillCasted_Rpc(skill.GetId());
            }
            #endif
        }

        public void OnSkillBehaviourStarted(SkillBehaviour skillBehaviour)
        {
             #if NETWORKING_FISHNET
            if (!IsServerInitialized || !IsSpawned) return;
            Skill skill = skillBehaviour.ParentSkill;
            SkillCastRpcData castRpcData = SkillCastRpcData.From(skill.GetId(), skill.CurrentSkillCastData);
            ObserversOnSkillBehaviourStarted_Rpc(skill.GetId(), skill.CurrentSkilLBehaviourIndex, castRpcData);
            #endif
        }

        public virtual void OnSkillCastEnded(Skill skill)
        {
            SkillCastEndedEvent?.Invoke(skill);
#if NETWORKING_FISHNET
            if(IsServerInitialized)
            {
                ObserversOnSkillCastEnded_Rpc(skill.GetId());
            }
#endif
        }
#endregion

        #region Networking

#if NETWORKING_FISHNET
        [ServerRpc]
        private void ServerCastSkill_Rpc(SkillCastRpcData rpcData)
        {
            SkillDataAsset asset = RpgManager.GetSkillDataAssetById(rpcData.SkillId);
            if (asset == null)
            {
                Debug.LogWarning($"[SpellBook] ServerCastSkill_Rpc: skill asset not found for id '{rpcData.SkillId}'");
                return;
            }
            ActionCastData castData = rpcData.ToActionCastData();
            castData.Caster = Actor;
            ExecuteCastSkill(asset, castData);
        }

        [ObserversRpc(ExcludeOwner = true)]
        private void ObserversSkillCasted_Rpc(string skillId)
        {
            if (IsServerInitialized) return;
            Skill skill = GetSkillById(skillId);
            if (skill == null || _activeSkills.Contains(skill)) return;
            _activeSkills.Add(skill);
        }

        [ObserversRpc]
        private void ObserversOnSkillBehaviourStarted_Rpc(string skillId, int behaviourIndex, SkillCastRpcData castData)
        {
            if (IsServerInitialized) return;
            Skill skill = GetSkillById(skillId);
            if (skill == null) return;
            if (IsOwner && skill.IsCasting() && skill.CurrentSkilLBehaviourIndex == behaviourIndex) return;
            skill.CurrentSkillCastData = castData.ToActionCastData();
            skill.BeginObserverCast();
            if (!_activeSkills.Contains(skill)) _activeSkills.Add(skill);
            skill.CurrentSkilLBehaviourIndex = behaviourIndex;
            skill.StartSkillBehaviour(behaviourIndex);
        }

        [ObserversRpc]
        private void ObserversOnSkillCastEnded_Rpc(string skillId)
        {
            if (IsServerInitialized) return;
            Skill skill = GetSkillById(skillId);
            skill?.EndCast();
        }
#endif

        #endregion
    }
}