using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Ball : MonoBehaviour
{
    [Header("ボールの種類")]
    public BallType ballType;

    [Header("ボールのイメージ変更用")]
    public Image imgBall;

    [Header("スワイプされたボールである判定。trueの場合、このボールは削除対象となる")]
    public bool isSelected;

    [Header("スワイプされた通し番号。スワイプされた順番が代入される")]
    public int num;

    /// <summary>
    /// ボールの初期設定
    /// </summary>
    public void SetUpBall(BallType ballType, Sprite sprite)
    {
        // ボールの種類を設定
        this.ballType = ballType;

        // ボールの名前を、設定したボールの種類の名前に変更
        name = this.ballType.ToString();

        // 引数で届いたボールのイメージに合わせてイメージを変更
        ChangeBallImage(sprite);

    }

    /// <summary>
    /// ボールのイメージを変更
    /// </summary>
    /// <param name="changeSprite">ボールのイメージ</param>
    public void ChangeBallImage(Sprite changeSprite)
    {
        imgBall.sprite = changeSprite;
    }
}
