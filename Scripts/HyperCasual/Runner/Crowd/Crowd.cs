using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public class Crowd : Runner
    {
        
        [SerializeField] private bool FailLevelOnEmptyCrowd;
        [Header("Element Prefab")]
        [SerializeField] private CrowdElement CrowdElementPrefab;

        [Header("Crowd Formation")]
        protected int StartingCrowdElementCount = 1;
        [SerializeField] protected Transform CrowdParent; //Crowd will be gathered here
        [SerializeField] private float Radius = 0.5f;
        [SerializeField] private float AgentRadius = 0.3f;
        [SerializeField] private float NoiseMagnitude = 0f;

        //States
        private int _currentCrowdCount = 1;
        private bool _crowdNeedsUpdate;

        protected override void Update()
        {
            base.Update();
            //if(HCGameManager.GetCurrentLevelState() != LevelState.Playing) return;
            if(_crowdNeedsUpdate) UpdateCrowd();
        }

        #region Crowd Control
        /// <summary>
        /// Returns the size of the
        /// </summary>
        /// <returns></returns>
        public int GetCrowdSize()
        {
            return CrowdParent.childCount;
        }

        [Button("Create Crowd Elements")]
        public void CreateCrowdElements(int count)
        {
            for(int i=0;i<count;++i)
            {
                CreateCrowdElement();
            }
        }

        /// <summary>
        /// Adds an agent/element to the crowd
        /// </summary>
        protected virtual CrowdElement CreateCrowdElement()
        {
        
            CrowdElement crowdElement = GameManager.Instance.Pool.GetObject(CrowdElementPrefab.gameObject).GetComponent<CrowdElement>();

            crowdElement.transform.SetParent(CrowdParent);
            crowdElement.transform.localPosition = Vector3.zero;
            crowdElement.transform.localRotation = Quaternion.identity;

            crowdElement.Spawn(this);
            _currentCrowdCount++;
            _crowdNeedsUpdate = true;

            return crowdElement;
        }

        /// <summary>
        /// Updates the crowd related informations, like positioning.
        /// </summary>
        protected virtual void UpdateCrowd()
        {
            //Fail level if conditions are met on empty crowd
            if(FailLevelOnEmptyCrowd && GetCrowdSize() == 0 && CurrentLevel != null && CurrentLevel.CurrentState == LevelState.Playing)
            {
                LevelManager.GetContext<LevelManager>().FailLevel();
                return;
            }
            PositionCrowdElements();
            _crowdNeedsUpdate = false;
        }

        //Called when there is a change in crowd elements
        [Button("Recalculate Positions")]
        private void PositionCrowdElements()
        {
            int count = CrowdParent.childCount;
            int currentRingIndex = 0;
            int currentMaxAgentForRing = 0;
            int currentAgentCountForRing = 0;
            float currentDeltaAngle = 0f;

            float minCrowdX = 0f;
            float maxCrowdX = 0f;

            for (int i=0;i<count;++i)
            {
                float angle = currentAgentCountForRing * currentDeltaAngle;
                Vector3 newLocalPos = GetCartesianPositionFromPolar(currentRingIndex * Radius, angle);
                newLocalPos.x += Random.Range(0f, 1f) * NoiseMagnitude;
                newLocalPos.z += Random.Range(0f, 1f) * NoiseMagnitude;
                
                //Set min max
                if(newLocalPos.x > maxCrowdX)
                {
                    maxCrowdX = newLocalPos.x;
                }
                if(newLocalPos.x<minCrowdX)
                {
                    minCrowdX = newLocalPos.x;
                }

                CrowdParent.transform.GetChild(i).DOLocalMove(newLocalPos, 1f).SetEase(Ease.OutBack);
                currentAgentCountForRing++;
                if(currentAgentCountForRing >= currentMaxAgentForRing)
                {
                    //We are in the next ring
                    int remainingWorkers = count - i - 1;
                    if(remainingWorkers == 0) break;
                    currentRingIndex++;
                    currentMaxAgentForRing = Mathf.Min(GetMaxAgentCountForRing(currentRingIndex), remainingWorkers);
                    currentDeltaAngle = GetDeltaAngleForRing(currentMaxAgentForRing);
                    currentAgentCountForRing = 0;
                }
            }

            //Set crowd edges
            RunnerWidth = maxCrowdX - minCrowdX;
            RunnerWidthOffset = (maxCrowdX + minCrowdX) * 0.5f;
        }

        private float GetRingPerimeter(int ringIndex)
        {
            return 2 * (ringIndex * Radius * 2) * Mathf.PI; // R = ringIndex*RadiusPerRing*2
        }

        private int GetMaxAgentCountForRing(int ringIndex)
        {
            return Mathf.FloorToInt(GetRingPerimeter(ringIndex) /AgentRadius);
        }
        
        private Vector3 GetCartesianPositionFromPolar(float radius, float angle)
        {
            return new Vector3(Mathf.Cos(Mathf.Deg2Rad * angle)*radius, 0, Mathf.Sin(Mathf.Deg2Rad * angle) * radius);
        }
        /// <summary>
        /// Amount of angle change to position agents in that ring
        /// </summary>
        /// <returns></returns>
        private float GetDeltaAngleForRing(int amountOfWorkers)
        {
            return 360.0f / Mathf.Max(amountOfWorkers, 1);
        }

        public void SetCrowdNeedsUpdate()
        {
            _crowdNeedsUpdate = true;
        }
        #endregion

        #region Crowd Multiplication
        // Define the delegate for sorting
        public delegate List<CrowdElement> SortHandler(List<CrowdElement> crowd);
        public override void OnMultiplicationGate(GateOperation operationType, float value)
        {
            int currentChildCount = CrowdParent.childCount;

            if (operationType == GateOperation.None) return;

            if (operationType == GateOperation.Addition)
            {
                if (value < 0)
                {
                    RemoveCrowdElements((int)Mathf.Abs(value));
                }
                else
                {
                    CreateCrowdElements((int)value);
                }
            }
            else if (operationType == GateOperation.Multiplication)
            {
                int newCrowdSize = (int)Mathf.RoundToInt(currentChildCount * value);
                int diff = newCrowdSize - currentChildCount;
                if (diff == 0) return;
                if (diff > 0)
                {
                    CreateCrowdElements(diff);
                }
                else
                {
                    RemoveCrowdElements(Mathf.Abs(diff));
                }
            }
        }

 

        [Button("Remove Crowd Elements")]
        public void RemoveCrowdElements(int x, SortHandler sorter = null)
        {
            // If no sorter is provided, use the default RandomizeCrowd
            if (sorter == null)
            {
                sorter = RandomizeCrowd;
            }

            // Extract all the CrowdElement children from CrowdParent into a list
            List<CrowdElement> crowdList = new List<CrowdElement>();
            foreach (Transform child in CrowdParent)
            {
                CrowdElement element = child.GetComponent<CrowdElement>();
                if (element != null)
                {
                    crowdList.Add(element);
                }
            }

            // Sort the list using the provided sort handler
            crowdList = sorter(crowdList);
            x = Mathf.Max(Mathf.Min(x, CrowdParent.childCount - 1), 0);
            // Remove x elements
            for (int i = 0; i < x && i < crowdList.Count; i++)
            {
                GameManager.Instance.Pool.PoolObject(crowdList[i].gameObject);
            }
            _crowdNeedsUpdate = true;
        }

        // Example SortHandler to randomize the crowd
        public List<CrowdElement> RandomizeCrowd(List<CrowdElement> crowd)
        {
            // Using System.Linq's OrderBy with a random key to shuffle the list
            return crowd.OrderBy(a => UnityEngine.Random.value).ToList();
        }
        protected void ClearCrowd()
        {
            List<CrowdElement> crowdAgents = CrowdParent.GetComponentsInChildren<CrowdElement>().ToList();
            _currentCrowdCount = 0;
            foreach (CrowdElement agent in crowdAgents)
            {
                if (agent == CrowdParent.transform) continue;
                GameManager.Instance.Pool.PoolObject(agent.gameObject);
            }
        }
        #endregion

        #region Crowd Element Manipulation
        public delegate void CrowdElementOperation(CrowdElement crowdElement);
        public void ApplyOperationToCrowd(CrowdElementOperation operation)
        {
            List<CrowdElement> crowdAgents = CrowdParent.GetComponentsInChildren<CrowdElement>().ToList();
            foreach (CrowdElement agent in crowdAgents)
            {
                if (agent == CrowdParent.transform) continue;
                operation(agent);
            }
        }

        public List<CrowdElement> GetCrowdElements()
        {
            return CrowdParent.GetComponentsInChildren<CrowdElement>().ToList();
        }
        #endregion

        public override void Reset()
        {
            base.Reset();
            ClearCrowd();
            CreateCrowdElements(StartingCrowdElementCount);
        }
    }
}
