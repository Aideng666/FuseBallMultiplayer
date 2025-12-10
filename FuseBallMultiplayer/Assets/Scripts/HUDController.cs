using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private Image player1Fuse;
    [SerializeField] private Image player2Fuse;
    [SerializeField] private Image player1Spark;
    [SerializeField] private Image player2Spark;
    [SerializeField] private Image centerText;
    [SerializeField] private TMP_Text readyText;
    [SerializeField] private float startSparkHeight;
    [SerializeField] private float endSparkHeight;
    [SerializeField] private GameObject gameOverSection;
    [SerializeField] private Image winnerImage;
    [SerializeField] private Sprite player1WinSprite;
    [SerializeField] private Sprite player2WinSprite;

    private Animator _anim;
    private bool _gameStarted;

    public event Action OnGameStarted;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    public void GameStartAnimEvent()
    {
        OnGameStarted?.Invoke();
        centerText.enabled = false;
    }

    public void PlayGameStartSequence()
    {
        if (!_gameStarted)
        {
            readyText.enabled = false;
            _anim.SetTrigger("Start");
            _gameStarted = true;
        }
    }

    public void ShowGameOver(int winner)
    {
        gameOverSection.SetActive(true);

        if (winner == 1)
        {
            winnerImage.sprite = player1WinSprite;
        } 
        else if (winner == 2)
        {
            winnerImage.sprite = player2WinSprite;
        }
    }

    public void UpdateReadyText(int numPlayersReady)
    {
        readyText.text = $"{numPlayersReady} / 2 Ready";
    }

    public void UpdateFuses(Player_HostMode player1, Player_HostMode player2)
    {
        var maxFuse = 60f;

        player1Fuse.fillAmount = player1.CurrentFuseDuration / maxFuse;
        player2Fuse.fillAmount = player2.CurrentFuseDuration / maxFuse;

        var player1FillPercentage = Mathf.InverseLerp(60, 0, player1.CurrentFuseDuration);
        var player2FillPercentage = Mathf.InverseLerp(60, 0, player2.CurrentFuseDuration);
        
        player1Spark.rectTransform.localPosition =
            new Vector3(player1Spark.rectTransform.localPosition.x,
                Mathf.Lerp(startSparkHeight, endSparkHeight, player1FillPercentage), player1Spark.rectTransform.localPosition.z);
        
        player2Spark.rectTransform.localPosition =
            new Vector3(player2Spark.rectTransform.localPosition.x,
                Mathf.Lerp(startSparkHeight, endSparkHeight, player2FillPercentage), player2Spark.rectTransform.localPosition.z);
    }
}
