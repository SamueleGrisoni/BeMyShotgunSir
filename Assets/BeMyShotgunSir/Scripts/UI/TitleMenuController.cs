using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class TitleMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument titleMenuDocument;

    private VisualElement _root;
    private Button _startButton;
    private Button _optionsButton;
    private Button _feedbackButton;


    private void OnEnable()
    {
        _root = titleMenuDocument.rootVisualElement;
        _startButton = _root.Q<Button>("StartButton");
        _optionsButton = _root.Q<Button>("OptionsButton");
        _feedbackButton = _root.Q<Button>("FeedbackButton");

        StartCoroutine(InitNextFrame());

    }

    IEnumerator InitNextFrame()
    {
        yield return null;
        _startButton.clicked += OnStartButtonClicked;
        _optionsButton.clicked += OnOptionsButtonClicked;
        _feedbackButton.clicked += OnFeedbackButtonClicked;
    }


    private void OnStartButtonClicked()
    {
        // Load the next scene or start the game
        Debug.Log("Start Button Clicked");
    }

    private void OnOptionsButtonClicked()
    {
        // Open the options menu
        Debug.Log("Options Button Clicked");
    }

    private void OnFeedbackButtonClicked()
    {
        // Open the feedback form or page
        Debug.Log("Feedback Button Clicked");
    }

}
