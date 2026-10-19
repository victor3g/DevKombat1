using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DevKombat.Fighters
{
    /// <summary>
    /// Controla a exibição simples de balões de diálogo dos lutadores.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Text dialogueText;
        [Min(0.1f)]
        [SerializeField] private float defaultDuration = 3f;

        private Coroutine activeDialogue;

        private void Awake()
        {
            HideDialogue();
        }

        public void ShowDialogue(string line)
        {
            ShowDialogue(line, defaultDuration);
        }

        public void ShowDialogue(string line, float duration)
        {
            if (dialoguePanel == null || dialogueText == null)
            {
                Debug.LogWarning("DialogueManager ainda não possui painel e texto configurados.", this);
                return;
            }

            if (activeDialogue != null)
            {
                StopCoroutine(activeDialogue);
            }

            activeDialogue = StartCoroutine(DisplayForDuration(line, duration));
        }

        public void ShowRandomDialogue(FighterData fighter)
        {
            if (fighter == null || fighter.DialogueLines.Count == 0)
            {
                return;
            }

            int index = Random.Range(0, fighter.DialogueLines.Count);
            ShowDialogue(fighter.DialogueLines[index]);
        }

        public void HideDialogue()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            activeDialogue = null;
        }

        private IEnumerator DisplayForDuration(string line, float duration)
        {
            dialogueText.text = line;
            dialoguePanel.SetActive(true);
            yield return new WaitForSeconds(Mathf.Max(0.1f, duration));
            dialoguePanel.SetActive(false);
            activeDialogue = null;
        }
    }
}
