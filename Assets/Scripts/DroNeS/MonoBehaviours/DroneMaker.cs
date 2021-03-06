﻿using System.Collections;
using DroNeS.Systems.EventSystem;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace DroNeS.MonoBehaviours
{
    public class DroneMaker : MonoBehaviour
    {
        // Start is called before the first frame update
        private Button _button;
        private Button BuildDrone {
            get
            {
                if (_button == null) _button = GetComponent<Button>();
                return _button;
            }
        }
        
        private IEnumerator Start()
        {
            while (World.Active.GetExistingSystem<DroneBuilderSystem>() == null) yield return null;
            
            BuildDrone.onClick.AddListener(World.Active.GetOrCreateSystem<DroneBuilderSystem>().AddDrone);
        }


    }
}
