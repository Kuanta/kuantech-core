using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Kuantech.DemolutionRunner;
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
        public CrowdFormationData FormationData;
        [NonSerialized] public CrowdFormation CrowdFormation;
        protected int StartingCrowdElementCount = 1;
        public Transform CrowdParent; //Crowd will be gathered here

        //States
        private int _currentCrowdCount = 1;
        private bool _crowdNeedsUpdate;

        public override void Initialize()
        {
            base.Initialize();
            SetupFormation();
        }

        private void SetupFormation()
        {
            switch (FormationData.FormationType)
            {
                case FormationType.Triangular:
                    CrowdFormation = new TriangularFormation(FormationData);
                    break;
                case FormationType.Rectangular:
                    CrowdFormation = new RectangularFormation(FormationData);
                    break;
                case FormationType.Circular:
                default:
                    CrowdFormation = new CircularFormation(FormationData);
                    break;
            }
        }
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
            if(CrowdFormation == null) SetupFormation();
            CrowdFormation.SetCrowdFormation(CrowdParent);
            RunnerWidth = CrowdFormation.GetCrowdWidth();
            RunnerWidthOffset = CrowdFormation.GetCrowdWidthOffset();
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
