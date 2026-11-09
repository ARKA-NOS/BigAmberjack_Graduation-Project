using _Scripts._Core._Debug;
using _Scripts.UI.BindSystem;
using UnityEngine;

namespace _Scripts.Loading
{
    [CreateAssetMenu(fileName = "Loading View Model", menuName = "LoadingSystem/Loading View Model", order = 0)]
    public class LoadingViewModel : AbstractUIViewModel<LoadingData>
    {
        public override void Bind(LoadingData data)
        {
            base.Bind(data);
            FDebug.Log(data, this);
        }
        
    }
}