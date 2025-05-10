using System;
using UnityEngine;

namespace Kuantech.Utils
{
    [Serializable]
    public class IdReference
    {
        [SerializeField] private string Id;
        [SerializeField] private IdReferenceAsset IdReferenceAsset;

        public void SetId(string id)
        {
            Id = id;
        }
        public string GetId()
        {
            if (IdReferenceAsset != null) return IdReferenceAsset.Id;
            return Id;
        }
    }
}