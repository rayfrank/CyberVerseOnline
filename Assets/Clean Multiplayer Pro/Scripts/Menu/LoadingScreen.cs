using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AvocadoShark
{
    public class LoadingScreen : MonoBehaviour
    {
        public CanvasGroup canvasGroup;
        public float lerpSpeed = 1.0f; // Speed for lerp. Adjust as needed.
        public bool FadeOnStart = false;

        private void Awake()
        {
            if (!FadeOnStart)
            {
                // Initialize the canvas group alpha to 0
                canvasGroup.alpha = 0;
            }
            else
            {
                canvasGroup.alpha = 1;
                FadeOutAndDisable();
            }
        }

        // Called automatically when the GameObject is enabled
        private void OnEnable()
        {
            if (!FadeOnStart)
                StartCoroutine(LerpCanvasAlpha(0, 1));
        }

        // Public function to call when you want to fade out and disable
        public void FadeOutAndDisable()
        {
            StartCoroutine(LerpCanvasAlpha(1, 0));
        }

        private IEnumerator LerpCanvasAlpha(float startValue, float endValue)
        {
            float timeStartedLerping = Time.time;
            bool reachedEnd = false;

            while (!reachedEnd)
            {
                float timeSinceStarted = Time.time - timeStartedLerping;
                float percentageComplete = timeSinceStarted / lerpSpeed;

                canvasGroup.alpha = Mathf.Lerp(startValue, endValue, percentageComplete);

                if (percentageComplete >= 1.0f)
                {
                    reachedEnd = true;
                }

                yield return new WaitForEndOfFrame();
            }

            if (endValue == 0)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
