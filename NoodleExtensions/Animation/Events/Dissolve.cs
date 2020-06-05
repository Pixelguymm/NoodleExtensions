﻿using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using IPA.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static NoodleExtensions.Animation.AnimationController;
using static NoodleExtensions.Animation.AnimationHelper;
using static NoodleExtensions.Plugin;

namespace NoodleExtensions.Animation
{
    internal static class Dissolve
    {
        private static Dictionary<Track, Coroutine> _activeCoroutines = new Dictionary<Track, Coroutine>();
        private static readonly FieldAccessor<BaseNoteVisuals, CutoutAnimateEffect>.Accessor _noteCutoutAnimateEffectAccessor = FieldAccessor<BaseNoteVisuals, CutoutAnimateEffect>.GetAccessor("_cutoutAnimateEffect");
        private static readonly FieldAccessor<ObstacleDissolve, CutoutAnimateEffect>.Accessor _obstacleCutoutAnimateEffectAccessor = FieldAccessor<ObstacleDissolve, CutoutAnimateEffect>.GetAccessor("_cutoutAnimateEffect");

        internal static void Callback(CustomEventData customEventData)
        {
            if (customEventData.type == "Dissolve")
            {
                Track track = GetTrack(customEventData);
                if (track != null)
                {
                    float start = (float?)Trees.at(customEventData.data, START) ?? 1f;
                    float end = (float?)Trees.at(customEventData.data, END) ?? 0f;
                    float duration = (float?)Trees.at(customEventData.data, DURATION) ?? 1.4f;
                    string easingString = Trees.at(customEventData.data, EASING);
                    Easings.Functions easing = Easings.InterprateString(easingString);

                    List<CutoutAnimateEffect> cutoutAnimateEffects = new List<CutoutAnimateEffect>();
                    foreach (NoteController noteController in GetActiveNotes(track))
                    {
                        BaseNoteVisuals baseNoteVisuals = noteController.gameObject.GetComponent<BaseNoteVisuals>();
                        cutoutAnimateEffects.Add(_noteCutoutAnimateEffectAccessor(ref baseNoteVisuals));
                    }
                    foreach (ObstacleController obstacleController in GetActiveObstacles(track))
                    {
                        ObstacleDissolve obstacleDissolve = obstacleController.gameObject.GetComponent<ObstacleDissolve>();
                        cutoutAnimateEffects.Add(_obstacleCutoutAnimateEffectAccessor(ref obstacleDissolve));
                    }

                    if (_activeCoroutines.TryGetValue(track, out Coroutine coroutine) && coroutine != null) _instance.StopCoroutine(coroutine);
                    _activeCoroutines[track] = _instance.StartCoroutine(DissolveCoroutine(start, end, duration, customEventData.time, cutoutAnimateEffects, easing, track));
                }
            }
        }

        private static IEnumerator DissolveCoroutine(float cutoutStart, float cutoutEnd, float duration, float startTime,
            List<CutoutAnimateEffect> cutoutAnimateEffects, Easings.Functions easing, Track track)
        {
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime = _customEventCallbackController._audioTimeSource.songTime - startTime;
                float time = elapsedTime / duration;
                float cutout = 1 - Mathf.Lerp(cutoutStart, cutoutEnd, Easings.Interpolate(time, easing));
                cutoutAnimateEffects.ForEach(n => n.SetCutout(cutout));
                yield return null;
            }
            cutoutAnimateEffects.ForEach(n => n.SetCutout(1 - cutoutEnd));
            _activeCoroutines.Remove(track);
            yield break;
        }
    }
}