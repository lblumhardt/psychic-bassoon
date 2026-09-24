using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuPage : MonoBehaviour
{
    [SerializeField] private string shopSceneName = "ShopScene";

    private void Start()
    {
        VisualElement root = ToolkitUi.Attach(this, new Color(0.06f, 0.09f, 0.14f));
        root.style.justifyContent = Justify.Center;
        root.style.alignItems = Align.Center;

        Label title = ToolkitUi.Label("Psychic Bassoon", 54, Color.white, true);
        title.style.marginBottom = 14;
        root.Add(title);

        Label subtitle = ToolkitUi.Label("Reach 10 wins. Three losses ends your run.", 20,
            new Color(0.74f, 0.8f, 0.88f));
        subtitle.style.marginBottom = 36;
        root.Add(subtitle);

        Button start = ToolkitUi.Button("Start Run", StartRun);
        start.style.width = 240;
        start.style.height = 54;
        root.Add(start);
    }

    private void StartRun()
    {
        RunProgress.StartNewRun();
        SceneManager.LoadScene(shopSceneName);
    }
}
