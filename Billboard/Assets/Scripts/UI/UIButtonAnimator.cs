using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BillboardTool.UI
{
    /// <summary>
    /// Smooth hover and press feedback for menu buttons.
    /// </summary>
    public class UIButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float pressScale = 0.95f;
        [SerializeField] private float transitionSpeed = 15f;

        private Vector3 originalScale;
        private Vector3 targetScale;
        private Coroutine scaleCoroutine;

        private void Awake()
        {
            originalScale = transform.localScale;
            targetScale = originalScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            targetScale = originalScale * hoverScale;
            StartScaleTransition();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = originalScale;
            StartScaleTransition();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetScale = originalScale * pressScale;
            StartScaleTransition();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            targetScale = originalScale * hoverScale;
            StartScaleTransition();
        }

        private void StartScaleTransition()
        {
            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
            }
            scaleCoroutine = StartCoroutine(AnimateScale());
        }

        private IEnumerator AnimateScale()
        {
            while (Vector3.Distance(transform.localScale, targetScale) > 0.001f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * transitionSpeed);
                yield return null;
            }
            transform.localScale = targetScale;
        }

        private void OnDisable()
        {
            transform.localScale = originalScale;
        }
    }
}
