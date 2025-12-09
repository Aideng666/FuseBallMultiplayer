using System;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject joinPanel;

    [Header("Main Panel")]
    [SerializeField] private Button hostGameButton;
    [SerializeField] private Button joinGameButton;
    
    [Header("Host Panel")]
    [SerializeField] private Button confirmHostButton;
    [SerializeField] private Button hostBackButton;
    [SerializeField] private TMP_InputField hostInputField;
    [SerializeField] private TMP_Text hostWarningText;
    
    [Header("Join Panel")]
    [SerializeField] private Button confirmJoinButton;
    [SerializeField] private Button joinBackButton;
    [SerializeField] private TMP_InputField joinInputField;
    [SerializeField] private TMP_Text joinWarningText;

    public event Action<GameMode, string> OnPlayerJoinedSession;

    private void Awake()
    {
        hostGameButton.onClick.AddListener(_onHostPressed);
        joinGameButton.onClick.AddListener(_onJoinPressed);
        confirmHostButton.onClick.AddListener(_onHostConfirmed);
        confirmJoinButton.onClick.AddListener(_onJoinConfirmed);
        hostBackButton.onClick.AddListener(_onBackPressed);
        joinBackButton.onClick.AddListener(_onBackPressed);
    }

    private void OnDestroy()
    {
        hostGameButton.onClick.RemoveListener(_onHostPressed);
        joinGameButton.onClick.RemoveListener(_onJoinPressed);
        confirmHostButton.onClick.RemoveListener(_onHostConfirmed);
        confirmJoinButton.onClick.RemoveListener(_onJoinConfirmed);
        hostBackButton.onClick.RemoveListener(_onBackPressed);
        joinBackButton.onClick.RemoveListener(_onBackPressed);
    }

    private void _onHostPressed()
    {
        hostPanel.SetActive(true);
        joinPanel.SetActive(false);
        hostWarningText.gameObject.SetActive(false);
        joinWarningText.gameObject.SetActive(false);
    }

    private void _onJoinPressed()
    {
        hostPanel.SetActive(false);
        joinPanel.SetActive(true);
        hostWarningText.gameObject.SetActive(false);
        joinWarningText.gameObject.SetActive(false);
    }
    
    private void _onHostConfirmed()
    {
        if (hostInputField.text.Length == 0)
        {
            hostWarningText.gameObject.SetActive(true);
        }
        else
        {
            OnPlayerJoinedSession?.Invoke(GameMode.Host, hostInputField.text);
        }
    }

    private void _onJoinConfirmed()
    {
        if (joinInputField.text.Length == 0)
        {
            joinWarningText.gameObject.SetActive(true);
        }
        else
        {
            OnPlayerJoinedSession?.Invoke(GameMode.Client, joinInputField.text);
        }
    }
    
    private void _onBackPressed()
    {
        hostPanel.SetActive(false);
        joinPanel.SetActive(false);
        hostWarningText.gameObject.SetActive(false);
        joinWarningText.gameObject.SetActive(false);
    }
}
