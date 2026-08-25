using System;
using System.Collections;
using DevLib.ObjectPool.Runtime;
using UnityEngine;

namespace CoreSystem.Effect
{
	public class PoolableVfx : PoolableMono
	{
		[SerializeField] private GameObject effectObject;
		private IPlayableVFX _playableVFX;
	
		public event Action<PoolableVfx> OnVfxEnd;
	
		private void Awake()
		{
			_playableVFX = effectObject.GetComponent<IPlayableVFX>();
		}
	
		private void OnValidate()
		{
			if (effectObject == null) return;
			_playableVFX = effectObject.GetComponent<IPlayableVFX>();
			if (_playableVFX == null)
				effectObject = null;
		}
	
		private void Reset()
		{
			_playableVFX.StopVFX();
		}
	
		public void PlayVfx(Vector3 position, Quaternion rotation)
		{
			transform.SetPositionAndRotation(position, rotation);
			StartCoroutine(PlayVfxCoroutine());
		}
	
		private IEnumerator PlayVfxCoroutine()
		{
			_playableVFX.PlayVFX();
			yield return new WaitForSeconds(_playableVFX.VfxDuration);
			OnVfxEnd?.Invoke(this);
		}
		public void PlayVfx() => _playableVFX.PlayVFX();
	}
}