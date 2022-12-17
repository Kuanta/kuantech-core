using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Scripts.Utils
{
    public class RegexTest : MonoBehaviour
    {
        public string testString = "This skill deal ${damage} damage";
        public string newString = "";
        public string regexPattern = "${[.]+}";
        public Dictionary<string, float> variables;

        private void Start()
        {
            variables = new Dictionary<string, float>();
            variables["damage"] = 10f;
        }
        [Button("Test")]
        public void GetSmartText()
        {
            Regex reg = new Regex(regexPattern);
            MatchCollection matches = reg.Matches(testString);
            newString = testString;
            foreach (Match m in matches)
            {
                string variableName = m.Value.Substring(2, m.Value.Length - 3);
                Debug.LogError(variableName);
                newString = newString.Replace(m.Value, "bok");

            }
        }
    }
}