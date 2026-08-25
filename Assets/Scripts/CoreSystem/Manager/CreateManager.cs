using CoreSystem.Effect;
using CoreSystem.Events;
using DevLib.EventChannelSystem;
using DevLib.ObjectPool.Runtime;
using UnityEngine;

namespace CoreSystem.Manager
{
	public class CreateManager : MonoBehaviour
	{
		[SerializeField] private EventChannelSO createChannel;
		[SerializeField] private PoolManagerSO poolManagerAsset;

		private void Awake()
		{
			createChannel.AddListener<ShowPoolingVfx>(HandleShowPoolingVfx);
		}

		private void OnDestroy()
		{
			createChannel.RemoveListener<ShowPoolingVfx>(HandleShowPoolingVfx);
		}

		private void HandleShowPoolingVfx(ShowPoolingVfx evt)
		{
			PoolableVfx vfx = poolManagerAsset.Pop<PoolableVfx>(evt.ItemData);
			vfx.OnVfxEnd += HandleVfxEnd;
			vfx.PlayVfx(evt.Position, evt.Rotation);
		}

		private void HandleVfxEnd(PoolableVfx targetVfx)
		{
			targetVfx.OnVfxEnd -= HandleVfxEnd;
			poolManagerAsset.Push(targetVfx);
		}
	}
}