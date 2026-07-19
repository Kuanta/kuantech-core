using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core
{
    [CreateAssetMenu(fileName ="ActorBlueprintCollection", menuName = "Kuantech/Data/ActorBlueprintCollection")]
    public class ActorBlueprintCollection : ScriptableObject
    {
        public List<ActorBlueprint> ActorBlueprints;
        private Dictionary<string, ActorBlueprint> _blueprintsByKey;

        /// <summary>
        /// Returns actor blueprint
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActorBlueprint GetActorBlueprint(string id)
        {
            ActorBlueprint toReturn = null;
            if(_blueprintsByKey == null)
            {
                _blueprintsByKey = new Dictionary<string, ActorBlueprint>();
                foreach(var blueprint in ActorBlueprints)
                {
                    string bpId = blueprint.GetId();
                    _blueprintsByKey.Add(blueprint.GetId(), blueprint);
                    if(bpId == id)
                    {
                        toReturn = blueprint;
                    }
                }
            }
            else
            {
                if(!_blueprintsByKey.ContainsKey(id)) return null;
                toReturn = _blueprintsByKey[id];
            }
            return toReturn;
        }
    }
}