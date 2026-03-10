using PhalanxChronicle.Presentation;
using UnityEngine;

namespace PhalanxChronicle.Battle.Effects
{
    public sealed class FloatingText : MonoBehaviour
    {
        private TextMesh textMesh;
        private MeshRenderer meshRenderer;
        private Color startColor;
        private float elapsed;

        public static void Spawn(string content, Vector3 worldPosition, Color color)
        {
            GameObject gameObject = new GameObject("FloatingText");
            gameObject.transform.position = worldPosition;

            FloatingText floatingText = gameObject.AddComponent<FloatingText>();
            floatingText.Initialize(content, color);
        }

        private void Initialize(string content, Color color)
        {
            textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.text = content;
            textMesh.font = RuntimeSpriteLibrary.DefaultFont;
            textMesh.fontSize = 72;
            textMesh.characterSize = 0.09f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;

            meshRenderer = textMesh.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = RuntimeSpriteLibrary.DefaultFont.material;
            meshRenderer.sortingOrder = 60;

            startColor = color;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / 0.75f);

            transform.position += Vector3.up * (0.8f * Time.deltaTime);
            textMesh.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), progress);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.16f, progress);

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
