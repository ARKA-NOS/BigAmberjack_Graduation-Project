using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lrw.Script.Test
{
    public class StopTest : MonoBehaviour
    {
        [SerializeField] private Key stopKey = Key.E;
        [SerializeField] private Key playKey = Key.P;
        private void Update()
        {
            if (Keyboard.current[stopKey].wasPressedThisFrame)
            {
                Time.timeScale = 0;
            }
            if (Keyboard.current[playKey].wasPressedThisFrame)
            {
                Time.timeScale = 1;
            }
        }
        
    }
}