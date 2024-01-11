using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData instance;

    [Header("ゲームに登場する球の最大種類数")]
    public int ballTypeCount = 5;

    [Header("ゲーム開始時に生成する球の数")]
    public int createBallCount = 50;

    [Header("現在のスコア")]
    public int score = 0;

    [Header("ボールを消した際に加算されるスコア")]
    public int ballPoint = 100;

    [Header("消したボールの数")]
    public int eraseBallCount = 0;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // ゲームの初期化
        InitGame();
    }

    /// <summary>
    /// ゲーム初期化
    /// </summary>
    private void InitGame()
    {
        score = 0;
        eraseBallCount = 0;
        Debug.Log("Init Game");
    }
}
