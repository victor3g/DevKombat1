using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevKombat.Fighters
{
    /// <summary>
    /// Monta automaticamente a arena do primeiro teste, sem exigir configuração
    /// manual de câmera, chão, jogadores ou interface.
    /// </summary>
    public class PrototypeArenaBootstrap : MonoBehaviour
    {
        [SerializeField] private GameObject fighterPrefab;
        [SerializeField] private Color backgroundColor = new Color(0.055f, 0.065f, 0.09f);
        [SerializeField] private Color groundColor = new Color(0.18f, 0.2f, 0.25f);

        private FighterController playerOne;
        private FighterController playerTwo;
        private bool roundFinished;
        private string winnerMessage = string.Empty;

        private void Awake()
        {
            if (fighterPrefab == null)
            {
                Debug.LogError("Arena sem prefab de lutador configurado.", this);
                enabled = false;
                return;
            }

            CreateCamera();
            CreateGround();
            CreatePlayers();
        }

        private void Update()
        {
            if (!roundFinished && playerOne != null && playerTwo != null)
            {
                if (playerOne.IsDefeated || playerTwo.IsDefeated)
                {
                    roundFinished = true;
                    winnerMessage = playerOne.IsDefeated ? "JOGADOR 2 VENCEU!" : "JOGADOR 1 VENCEU!";
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void CreateCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            camera.backgroundColor = backgroundColor;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(0f, 1.5f, -10f);
        }

        private void CreateGround()
        {
            GameObject ground = new GameObject("Ground");
            ground.transform.position = new Vector3(0f, -2.1f, 0f);

            BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(18f, 0.8f);

            SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSquareSprite();
            renderer.color = groundColor;
            renderer.size = new Vector2(18f, 0.8f);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.sortingOrder = -10;
        }

        private void CreatePlayers()
        {
            GameObject first = Instantiate(fighterPrefab, new Vector3(-2.2f, -0.2f, 0f), Quaternion.identity);
            first.name = "Mariana_P1";
            playerOne = first.GetComponent<FighterController>();
            PrototypeFighter2D firstInput = first.GetComponent<PrototypeFighter2D>();
            firstInput.ConfigureControls(KeyCode.A, KeyCode.D, KeyCode.W, KeyCode.F, KeyCode.G);
            firstInput.FaceRight(true);

            GameObject second = Instantiate(fighterPrefab, new Vector3(2.2f, -0.2f, 0f), Quaternion.identity);
            second.name = "Mariana_P2";
            playerTwo = second.GetComponent<FighterController>();
            PrototypeFighter2D secondInput = second.GetComponent<PrototypeFighter2D>();
            secondInput.ConfigureControls(KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow, KeyCode.K, KeyCode.L);
            secondInput.FaceRight(false);
        }

        private void OnGUI()
        {
            GUIStyle label = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            DrawHealthBar(new Rect(30f, 30f, Screen.width * 0.38f, 28f), playerOne, false);
            DrawHealthBar(new Rect(Screen.width * 0.62f - 30f, 30f, Screen.width * 0.38f, 28f), playerTwo, true);

            GUI.Label(new Rect(20f, Screen.height - 62f, Screen.width - 40f, 28f),
                "P1: A/D mover, W pular, F soco, G chute     |     P2: Setas mover/pular, K soco, L chute     |     R reiniciar",
                label);

            if (roundFinished)
            {
                GUIStyle winner = new GUIStyle(label) { fontSize = 38, fontStyle = FontStyle.Bold };
                GUI.Label(new Rect(0f, 85f, Screen.width, 60f), winnerMessage, winner);
            }
        }

        private static void DrawHealthBar(Rect rect, FighterController fighter, bool reverse)
        {
            GUI.Box(rect, GUIContent.none);
            float ratio = fighter != null && fighter.MaxHealth > 0f
                ? Mathf.Clamp01(fighter.CurrentHealth / fighter.MaxHealth)
                : 0f;
            Rect fill = new Rect(rect.x + 3f, rect.y + 3f, (rect.width - 6f) * ratio, rect.height - 6f);
            if (reverse)
            {
                fill.x = rect.xMax - 3f - fill.width;
            }

            Color previous = GUI.color;
            GUI.color = Color.Lerp(Color.red, new Color(0.2f, 0.9f, 0.3f), ratio);
            GUI.DrawTexture(fill, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static Sprite CreateSquareSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.name = "RuntimeSquare";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f,
                0,
                SpriteMeshType.FullRect);
        }
    }
}
