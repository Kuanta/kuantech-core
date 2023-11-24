using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "KTTags", menuName = "Kuantech/Settings/Tags")]
public class KTTagsList : ScriptableObject
{
    public List<string> tags = new List<string>();
}