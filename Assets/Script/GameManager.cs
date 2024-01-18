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

    [Header("スワイプで繋がる干支の範囲")]
    public float ballDistans = 1.0f;

    [SerializeField]
    private UIManager uiManager;

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

    private float timer;       // 残り時間計測用

    IEnumerator Start()   // 戻り値を void から IEnumerator型に変更してコルーチンメソッドにする
    {
        // ボールの画像を読み込む。この処理が終了するまで、次の処理へは行かないようにする
        yield return StartCoroutine(LoadBallSprites());

        // 残り時間の表示
        uiManager.UpdateDisprayGameTime(GameData.instance.gameTime);

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
        // ボールをつなげる処理
        if (Input.GetMouseButtonDown(0) && firstSelectBall == null)
        {
            // ボールを最初にドラッグした際の処理
            OnStartDrag();
        }
        else if (Input.GetMouseButtonUp(0)) // (順番が重要)
        {
            // 干支のドラッグをやめた（指を離した）際の処理
            OnEndDrag();
        }
        else if (firstSelectBall != null)
        {
            // ボールのドラッグ（スワイプ）中の処理
            OnDragging();
        }

        // ゲームの残り時間のカウント処理
        timer += Time.deltaTime;

        // timerが 1 以上になったら
        if (timer >= 1)
        {
            // リセットして再度加算できるように
            timer = 0;

            // 残り時間をマイナス
            GameData.instance.gameTime--;

            // 残り時間がマイナスになったら
            if (GameData.instance.gameTime <= 0)
            {
                //0で止める
                GameData.instance.gameTime = 0;

                // TODO ゲーム終了を追加する
                Debug.Log("ゲーム終了");
            }

            // 残り時間の表示更新
            uiManager.UpdateDisprayGameTime(GameData.instance.gameTime);
        }
    }

    /// <summary>
    /// ボールを最初にドラッグした際の処理
    /// </summary>
    private void OnStartDrag()
    {
        // 画面をタップした際の位置情報を、CameraクラスのScreenToWorldPointメソッドを利用してCanvas上の座標に変換
        RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

        // ボールが繋がっている数を初期化
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
    /// ボールのドラッグ（スワイプ）中の処理
    /// </summary>
    private void OnDragging()
    {
        // OnStartDragメソッドと同じ処理で、指の位置をワールド座標に変換しRayを発射し、その位置にあるコライダーを持つオブジェクトを取得してhit変数へ代入
        RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

        // Rayの戻り値があり(hit変数がnullではない)、hit変数のゲームオブジェクトballクラスを持っていたら
        if (hit.collider != null && hit.collider.gameObject.TryGetComponent(out Ball dragball))
        {
            // 現在選択中のボールの種類がnullなら処理は行わない
            if (currentBallType == null)
            {
                return;
            }

            // dragBall変数のボールの種類が最初に選択したボールの種類と同じであり、最後にタップしているボールと現在のボールが違うオブジェクトであり、かつ、現在のボールがすでに「選択中」でなければ
            if (dragball.ballType == currentBallType && lastSelectBall != dragball && !dragball.isSelected)
            {
                // 現在タップしているボールの位置情報と最後にタップした干支の位置情報と比べて、差分の値（ボール同士の距離）を取る
                float distance = Vector2.Distance(dragball.transform.position, lastSelectBall.transform.position);

                // ボール同士の距離が設定値よりも小さければ(２つのボールが離れていなければ)、ボールをつなげる
                if (distance < ballDistans)
                {
                    // 現在のボールを選択中にする
                    dragball.isSelected = true;

                    // 最後に選択しているボールを現在のボールに更新
                    lastSelectBall = dragball;

                    //ボールの繋がった数のカウントを１つ増やす
                    linkCount++;

                    // ボールに通し番号を設定
                    dragball.num = linkCount;

                    // 削除リストに現在のボールを追加
                    AddEraseBallList(dragball);
                }
            }

            // 現在のボールの種類を確認(現在のボール(dragBallの情報であれば、他の情報でも良い。ちゃんと選択されているかの確認用))
            Debug.Log(dragball.ballType);

            // 削除リストに２つ以上のボールが追加されている場合
            if (eraseBallList.Count > 1)
            {
                // 現在の干支の通し番号を確認
                Debug.Log(dragball.num);

                // 条件に合致する場合、削除リストからボールを除外する(ドラッグしたまま１つ前のボールに戻る場合、現在のボールを削除リストから除外する)
                if (eraseBallList[linkCount - 1] != lastSelectBall && eraseBallList[linkCount - 1].num == dragball.num && dragball.isSelected)
                {
                    // 選択中のボールを取り除く
                    RemoveEraseBallList(lastSelectBall);

                    lastSelectBall.GetComponent<Ball>().isSelected = false;

                    // 最後のボールの情報を、前のボールに戻す
                    lastSelectBall = dragball;

                    // 繋がっているボールの数を減らす
                    linkCount--;
                }
            }
        }
    }

    /// <summary>
    /// 干支のドラッグをやめた（指を画面から離した）際の処理
    /// </summary>
    private void OnEndDrag()
    {
        // 繋がっているボールが３つ以上あったら削除する処理に移る
        if (eraseBallList.Count >= 3)
        {
            // 選択されているボールを消す
            for (int i = 0; i < eraseBallList.Count; i++)
            {
                // ボールリストから取り除く
                ballList.Remove(eraseBallList[i]);

                // ボールを削除
                Destroy(eraseBallList[i].gameObject);
            }

            // スコアと消したボールの数の加算
            AddScores(currentBallType, (eraseBallList.Count));

            // 消したボールの数だけ新しいボールをランダムに生成
            StartCoroutine(CreateBalls(eraseBallList.Count));

            // 削除リストをクリアする
            eraseBallList.Clear();
        }
        else
        {
            // 繋がっているボールが２つ以下なら、削除はしない

            // 削除リストから、削除候補であったボールを取り除く
            for (int i = 0; i < eraseBallList.Count; i++)
            {
                // 各ボールの選択中の状態を解除する
                eraseBallList[i].isSelected = false;

                // ボールの色の透明度を元の透明度に戻す
                ChangeBallAlpha(eraseBallList[i], 1.0f);
            }
        }

        // 次回のボールを消す処理のために、各変数の値をnullにする
        firstSelectBall = null;
        lastSelectBall = null;
        currentBallType = null;
    }

    /// <summary>
    /// 選択されたボールを削除リストに追加
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
    /// 前のボールに戻った際に削除リストから削除
    /// </summary>
    /// <param name="dragBall"></param>
    private void RemoveEraseBallList(Ball dragball)
    {
        // 削除リストから削除
        eraseBallList.Remove(dragball);

        // 干支の透明度を元の値(1.0f)に戻す
        ChangeBallAlpha(dragball, 1.0f);

        // ボールの「選択中」の情報がtrueの場合
        if (dragball.isSelected)
        {
            // falseにして選択中ではない状態に戻す
            dragball.isSelected = false;
        }
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

    /// <summary>
    /// スコアと消した干支の数を追加
    /// </summary>
    /// <param name="etoType">消したボールの種類</param>
    /// <param name="count">消したボールの数</param>
    private void AddScores(BallType? ballType, int count)
    {
        // スコアを加算(BallPoint * 消した数)
        GameData.instance.score += GameData.instance.ballPoint * count;

        // 消したボールの数を加算
        GameData.instance.eraseBallCount += count;

        // スコア加算と画面の更新処理
        uiManager.UpdateDisplayScore();
    }
}
