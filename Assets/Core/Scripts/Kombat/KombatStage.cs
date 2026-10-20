using UnityEngine;

namespace DevKombat.Kombat
{
    /// <summary>
    /// Cenário de fundo. A imagem é ajustada para que a linha do chão (stages.json)
    /// fique em y = 0, onde os lutadores pisam.
    /// </summary>
    public class KombatStage : MonoBehaviour
    {
        public const float ViewHeight = 7.6f;
        public const float FloorFromBottom = 0.8f;

        public float HalfWidth { get; private set; } = 9f;
        public KombatStageInfo Info { get; private set; }

        public static KombatStage Create(KombatStageInfo info, float aspect)
        {
            GameObject go = new GameObject("Stage_" + (info != null ? info.id : "treino"));
            KombatStage stage = go.AddComponent<KombatStage>();
            stage.Build(info, aspect);
            return stage;
        }

        private void Build(KombatStageInfo info, float aspect)
        {
            Info = info;
            float viewWidth = ViewHeight * aspect;
            Sprite sprite = info != null && !string.IsNullOrEmpty(info.id) ? KombatResources.StageSprite(info.id) : null;

            if (sprite == null)
            {
                BuildFallback(viewWidth);
                return;
            }

            SpriteRenderer background = new GameObject("Background").AddComponent<SpriteRenderer>();
            background.transform.SetParent(transform, false);
            background.sprite = sprite;
            background.sortingOrder = -100;

            Vector2 size = sprite.bounds.size;
            float floor = Mathf.Clamp(info.floor, 0.5f, 1f);
            float aboveFloor = ViewHeight - FloorFromBottom;

            // Escala para que o trecho do topo até o chão preencha a tela acima do chão.
            float scale = aboveFloor / (size.y * floor);
            float minWidth = viewWidth * 1.02f;
            if (size.x * scale < minWidth)
            {
                scale = minWidth / size.x;
            }

            background.transform.localScale = new Vector3(scale, scale, 1f);
            float height = size.y * scale;
            float floorY = height * (1f - floor);
            Vector2 pivot = sprite.pivot / sprite.pixelsPerUnit;
            float pivotY = pivot.y * scale;
            background.transform.localPosition = new Vector3(0f, -floorY + pivotY, 0f);

            HalfWidth = Mathf.Max(viewWidth * 0.5f, size.x * scale * 0.5f) - 0.6f;

            // Faixa escura abaixo do chão, caso a imagem não cubra a tela inteira.
            if (floorY < FloorFromBottom)
            {
                SpriteRenderer filler = new GameObject("Filler").AddComponent<SpriteRenderer>();
                filler.transform.SetParent(transform, false);
                filler.sprite = KombatResources.WhiteSprite;
                filler.color = new Color(0.06f, 0.05f, 0.05f);
                filler.sortingOrder = -101;
                filler.transform.localScale = new Vector3(size.x * scale + 4f, FloorFromBottom * 2f, 1f);
                filler.transform.localPosition = new Vector3(0f, -FloorFromBottom, 0f);
            }
        }

        private void BuildFallback(float viewWidth)
        {
            HalfWidth = viewWidth * 0.75f;
            SpriteRenderer sky = new GameObject("Sky").AddComponent<SpriteRenderer>();
            sky.transform.SetParent(transform, false);
            sky.sprite = KombatResources.WhiteSprite;
            sky.color = new Color(0.16f, 0.08f, 0.1f);
            sky.sortingOrder = -101;
            sky.transform.localScale = new Vector3(HalfWidth * 2f + 8f, ViewHeight * 2f, 1f);
            sky.transform.localPosition = new Vector3(0f, ViewHeight * 0.5f, 0f);

            SpriteRenderer floor = new GameObject("Floor").AddComponent<SpriteRenderer>();
            floor.transform.SetParent(transform, false);
            floor.sprite = KombatResources.WhiteSprite;
            floor.color = new Color(0.32f, 0.3f, 0.3f);
            floor.sortingOrder = -100;
            floor.transform.localScale = new Vector3(HalfWidth * 2f + 8f, FloorFromBottom * 2f, 1f);
            floor.transform.localPosition = new Vector3(0f, -FloorFromBottom, 0f);
        }
    }

}
