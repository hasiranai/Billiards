using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [SerializeField, Header("ボールのプレファブ")]
    private Ball ballPrefab;      // 宣言する型を GameObject型 から Ball型に変更。BallPrefabは前と同じようにProjectないからアサインできる

    [SerializeField, Header("ボールの生成位置")]
    private Transform ballSetTran;

    [SerializeField, Header("ボール生成時の最大回転角度")]
    private float maxRotateAngle = 35.0f;

    [SerializeField, Header("ボール生成時の左右のランダム幅")]
    private float maxRange = 400.0f;

    [SerializeField, Header("ボール生成時の落下位置")]
    private float fallPos = 2000.0f;

    [SerializeField, Header("生成されたボールのリスト")]
    private List<Ball> ballList = new List<Ball>();

    [SerializeField, Header("ボールの画像データ")]
    private Sprite[] ballSprites;

    // 最初にドラッグしたボールの情報
    private Ball firstSelectBall;

    // 最後にドラッグしたボールの情報
    private Ball lastSelectBall;

    // 最初にドラッグしたボールの種類
    private BallType? currentBallType;

    [SerializeField, Header("削除対象となるボールを登録するリスト")]
    private List<Ball> eraseBallList = new List<Ball>();

    [SerializeField, Header("つながっているボールの数")]
    private int linkCount = 0;

    IEnumerator Start()   // 戻り値を void から IEnumerator型に変更してコルーチンメソッドにする
    {
        // ボールの画像を読み込む。この処理が終了するまで、次の処理へは行かないようにする
        yield return StartCoroutine(LoadBallSprites());

        // 引数で指定した数のボールを生成する
        StartCoroutine(CreateBalls(GameData.instance.createBallCount));
    }

    /// <summary>
    /// ボールの画像を読み込んで配列から使用できるようにする
    /// </summary>
    private IEnumerator LoadBallSprites()
    {
        // 配列の初期化(16個の画像が入るようにSprite型の配列を16個用意する)
        ballSprites = new Sprite[(int)BallType.Count];

        // Resources.LoadAllを行い、分割されている干支の画像を順番に全て読み込んで配列に代入
        //ballSprites = Resources.LoadAll<Sprite>("Sprites/ball");

        // 1つのファイルを分割しているわけではない場合の処理
        for(int i = 0; i < ballSprites.Length; i++)
        {
            ballSprites[i] = Resources.Load<Sprite>("Sprites/ball_" + i);
        }

        yield break;
    }

    /// <summary>
    /// ボールを生成
    /// </summary>
    /// <param name="count">生成する数</param>
    /// <returns></returns>
    private IEnumerator CreateBalls(int generateCount)
    {
        for (int i = 0; i < generateCount; i++)
        {
            // ボールプレファブのクローンをボールの生成位置に生成
            Ball ball = Instantiate(ballPrefab, ballSetTran, false);  // 生成されたボールを代入する型をGameObject型からBall型に変更

            // 生成されたボールの回転情報を設定(色々な角度になるように)
            ball.transform.rotation = Quaternion.AngleAxis(Random.Range(-maxRotateAngle, maxRotateAngle), Vector3.forward);

            // 生成位置をランダムにして落下位置を変化させる
            ball.transform.localPosition = new Vector2(Random.Range(-maxRange, maxRange), fallPos);

            // ランダムなボールを16種類の中から１つ選択
            int randomValue = Random.Range(0, (int)BallType.Count);

            // 生成された干支の初期設定(干支の種類と干支の画像を引数を使ってBallへ渡す)
            ball.SetUpBall((BallType)randomValue, ballSprites[randomValue]);

            // ballListに追加
            ballList.Add(ball);

            // 0.03秒待って次のボールを生成
            yield return new WaitForSeconds(0.03f);
        }
    }

    private void Update()
    {
        // 干支をつなげる処理
        if (Input.GetMouseButtonDown(0) && firstSelectBall == null)
        {
            // 干支を最初にドラッグした際の処理
            OnStartDrag();
        }
    }

    /// <summary>
    /// ボールを最初にドラッグした際の処理
    /// </summary>
    private void OnStartDrag()
    {
        // 画面をタップした際の位置情報を、CameraクラスのScreenToWorldPointメソッドを利用してCanvas上の座標に変換
        RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

        // 干支が繋がっている数を初期化
        linkCount = 0;

        // 変換した座標のコライダーを持つゲームオブジェクトがあるか確認
        if (hit.collider != null)
        {
            // ゲームオブジェクトがあった場合、そのゲームオブジェクトがBallクラスを持っているかどうか確認
            if (hit.collider.gameObject.TryGetComponent(out Ball dragBall))
            {
                // Ballクラスを持っていた場合には以下の処理を行う

                // 最初にドラッグしたボールの情報を変数に代入
                firstSelectBall = dragBall;

                // 最後にドラッグしたボールの情報を変数に代入(最初のドラッグなので、最後のドラッグも同じボール)
                lastSelectBall = dragBall;

                // 最初にドラッグしているボールの種類を代入 = 後程、この情報を使って繋がるボールかどうかを判別する
                currentBallType = dragBall.ballType;

                // ボールの状態が「選択中」であると更新
                dragBall.isSelected = true;

                // ボールに何番目に選択されているのか、通し番号を登録
                dragBall.num = linkCount;

                // 削除する対象のボールを登録するリストを初期化
                eraseBallList = new List<Ball>();

                // ドラッグ中の干支を削除の対象としてリストに登録
                AddEraseBallList(dragBall);
            }
        }
    }

    /// <summary>
    /// 選択された干支を削除リストに追加
    /// </summary>
    /// <param name="dragBall"></param>
    private void AddEraseBallList(Ball dragBall)
    {
        // 削除リストにドラッグ中のボールを追加
        eraseBallList.Add(dragBall);

        // ドラッグ中のボールのアルファ値を0.5fにする(半透明にすることで、選択中であることをユーザーに伝える)
        ChangeBallAlpha(dragBall, 0.5f);
    }

    /// <summary>
    /// ボールのアルファ値を変更
    /// </summary>
    /// <param name="dragBall"></param>
    /// <param name="alphaValue"></param>
    private void ChangeBallAlpha(Ball dragBall, float alphaValue)
    {
        // 現在ドラッグしているボールのアルファ値を変更
        dragBall.imgBall.color = new Color(dragBall.imgBall.color.r, dragBall.imgBall.color.g, dragBall.imgBall.color.b, alphaValue);
    }
}
