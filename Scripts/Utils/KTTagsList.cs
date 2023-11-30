using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "KTTags", menuName = "Kuantech/Settings/Tags")]
public class KTTagsList : ScriptableObject
{
    [Serializable]
    public struct TagGroup
    {
        public string TagGroupName;
        public List<string> tags;
    }

    public List<TagGroup> tagGroups = new List<TagGroup>();
}