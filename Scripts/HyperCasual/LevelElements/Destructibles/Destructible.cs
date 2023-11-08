using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core.FX;
using Sirenix.OdinInspector;
using UnityEngine;
namespace Kuantech.Core.HyperCasual
{
    public class Destructible : MonoBehaviour {
        [SerializeField] private GameObject WholeObject;
        [SerializeField] private GameObject DestroyedObject;
        [SerializeField] private List<DestructibleComponent> DestructibleComponents;
        [SerializeField] private Effect DestructionEffect;
        [SerializeField] private float DestructionMagnitude;
        [SerializeField] private float HideDelay;
        private List<DestructiblePiece> _pieces;
        [NonSerialized] public bool Destroyed = false;
        private IEnumerator _hideCoroutine;

        public void Initialize()
        {
            Destroyed = false;
            if(WholeObject != null) WholeObject.SetActive(true);
            if (DestroyedObject == null) return;
            _pieces = DestroyedObject.transform.GetComponentsInChildren<DestructiblePiece>().ToList();
            foreach (var piece in _pieces)
            {
                piece.Initialize();
            }
            foreach (var comp in DestructibleComponents)
            {
                comp.Initialize();
            }
        }

        public void Destruct(Vector3 hitPoint)
        {
            Destroyed = true;
            if(WholeObject != null) WholeObject.SetActive(false);

            Destroyed = true;
            if (DestroyedObject != null) DestroyedObject.SetActive(true);
            if (_pieces != null)
            {
                foreach (var piece in _pieces)
                {
                    Vector3 distance = (piece.transform.position - hitPoint).normalized;
                    piece.Rigidbody.AddForce(distance * DestructionMagnitude, ForceMode.Impulse);
                }
            }
            if (DestructionEffect != null) DestructionEffect.Play();
            if (DestroyedObject != null)
            {
                _hideCoroutine = HideDestroyedObject();
                StartCoroutine(_hideCoroutine);
            }

            foreach (var comp in DestructibleComponents)
            {
                comp.OnDestroyed();
            }
        }

        private IEnumerator HideDestroyedObject()
        {
            yield return new WaitForSeconds(HideDelay);
            DestroyedObject.SetActive(false);
        }
        
        public void Reset()
        {
            if (_pieces == null) return;
            Destroyed = false;

            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }
            if (WholeObject != null) WholeObject.SetActive(true);
            if (DestroyedObject != null) DestroyedObject.SetActive(false);
            foreach (var piece in _pieces)
            {
                piece.Reset();
            }
            foreach (var comp in DestructibleComponents)
            {
                comp.Reset();
            }
        }
        
        [Button("Add Destructible Pieces")]
        private void AddDestructiblePieces()
        {
            if(DestroyedObject == null) return;
            for(int i=0;i<DestroyedObject.transform.childCount;++i)
            {
                GameObject child = DestroyedObject.transform.GetChild(i).gameObject;
                if(child.GetComponent<DestructiblePiece>() == null)
                {
                    child.AddComponent<DestructiblePiece>();
                }
                if (child.GetComponent<Rigidbody>() == null)
                {
                    child.AddComponent<Rigidbody>();
                }
            }  
        }
    }
}