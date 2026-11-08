using System;
using _Scripts.DataStructure;
using UnityEngine;

namespace _Scripts.Loading
{
    public struct LoadingData
    {
        public string Text;
        public Gauge LoadingGauge;
        public bool IsComplete;

        public LoadingData(string loadingText, float current, float max)
        {
            Text = loadingText;
            LoadingGauge = new Gauge(current, max);
            IsComplete = Mathf.Approximately(current, max);
        }
        
        
        
    }
}