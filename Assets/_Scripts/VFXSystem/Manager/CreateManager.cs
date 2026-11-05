using _Scripts.DevLib.EventChannelSystem;
using _Scripts.DevLib.ObjectPool.Runtime;
using _Scripts.VFXSystem.Effect;
using _Scripts.VFXSystem.Events;
using UnityEngine;

namespace _Scripts.VFXSystem.Manager
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