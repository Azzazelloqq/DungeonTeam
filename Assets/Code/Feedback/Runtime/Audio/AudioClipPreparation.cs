using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DungeonTeam.Feedback.Runtime.Audio
{
    internal static class AudioClipPreparation
    {
        public static async UniTask PrepareAsync(
            IReadOnlyList<AudioClip> clips,
            CancellationToken token)
        {
            if (clips == null)
            {
                throw new ArgumentNullException(nameof(clips));
            }

            var uniqueClips = new HashSet<AudioClip>();
            for (var index = 0; index < clips.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                var clip = clips[index] ?? throw new InvalidOperationException(
                    $"Audio clip at index {index} is missing.");
                if (!uniqueClips.Add(clip) || clip.loadState == AudioDataLoadState.Loaded)
                {
                    continue;
                }

                if (clip.loadState == AudioDataLoadState.Unloaded && !clip.LoadAudioData())
                {
                    throw new InvalidOperationException(
                        $"Audio clip '{clip.name}' refused to start loading its audio data.");
                }

                await UniTask.WaitUntil(
                    () => clip.loadState != AudioDataLoadState.Loading,
                    cancellationToken: token);
                if (clip.loadState != AudioDataLoadState.Loaded)
                {
                    throw new InvalidOperationException(
                        $"Audio clip '{clip.name}' failed to load its audio data.");
                }
            }
        }
    }
}
