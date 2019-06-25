using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Master : MonoBehaviour
{
    bool gameFlag = true;

    // 盤
    List<List<bool>> board = new List<List<bool>>();
    // 盤Size（8x8）
    const int boardSize = 8;

    // ブロックズ
    //List<List<bool>> blocks = new List<List<bool>>();
    List<bool[,]> blocks = new List<bool[,]>();
    // ブロックの部品数
    List<int> NumberOfPartsOfBlock = new List<int>();

    // ブロック生成位置
    public Transform[] Frame;
    // ブロック用配列
    public Transform[] prefabBlocks;
    // 子オブジェクト用 (2019/06/20/11:47)
    private Transform[] childTransforms;
    // 乱数
    int ran;
    // 使用済みブロック数
    int alreadyUsedCount = 0;

    // 盤の局面取得フラグ
    int emptyCheck = 0;
    // ブロックを置いたか
    int putCheck = 0;

    // Ray
    Ray ray;
    // Rayのアタリ判定
    private bool hitRay = false;
    // Rayによって取得した情報を保存する構造体
    private RaycastHit hitBlock, hitBoard, hitObject;
    private RaycastHit[] hitObjects, hitObjectsX, hitObjectsZ;
    // ブロックの初期位置の保存用
    private Vector3 initialPosition;

    // ブロックの初期位置の状況
    private bool[] StatusOfGenerationPlace = new bool[3];
    // 生成されたブロック番号を保持
    private int[] blockNumber = new int[3];

    // Layer(Board)
    private int boardLayerIndex;

    // Start is called before the first frame update
    void Start()
    {
        // 盤 初期化　
        for (int y = 0; y < boardSize; y++)
        {
            board.Add(new List<bool>());
            for (int x = 0; x < boardSize; x++)
            {
                board[y].Add(false);
            }
        }


        blockInit();

        generate();
    }

    // Update is called once per frame
    void Update()
    {
        if (emptyCheck == 1)
        {
            boardSituation();

            // Debug(Board)
            boardShow();

            emptyCheck = 0;

            // 初期位置情報取得
            GetUnusedBlocks();

            // ブロックごとに置き場所が存在するかチェック
            for (int i = 0; i < 3; i++)
            {

                // ブロックの生成位置にブロックを存在するとき
                if (StatusOfGenerationPlace[i] == true)
                {
                    Debug.Log(blockNumber[i]);
                    Debug.Log("return:" + canPutBlock(blockNumber[i]));

                    gameFlag = canPutBlock(blockNumber[i]);

                    // ゲーム終了
                    if (gameFlag == false)
                    {
                        Debug.Log("---------- Game Over ----------");
                        break;
                    }
                }
            }
            
            
        }

        if (Input.GetMouseButtonDown(0))
        {
            check();
        }
        if (Input.GetMouseButtonUp(0) && hitRay)
        {
            fixation();

            hitRay = false;

            if (putCheck == 1)
            {
                delete();
            }

        }
        if (hitRay)
        {
            movement();
        }
        if (alreadyUsedCount >= 3)
        {
            generate();
            alreadyUsedCount = 0;
        }

        
    }

    /// <summary>
    /// ブロックを３個生成する
    /// </summary>
    private void generate()
    {
        // ブロックの生成
        for (int i = 0; i < 3; i++)
        {
            // 乱数生成
            ran = UnityEngine.Random.Range(0, prefabBlocks.Length);
            Debug.Log("ran:" + ran);
            // ブロック番号保存
            blockNumber[i] = ran;

            // Center座標を求める
            Vector3 BlockCenterPos = GetCenterPosition(prefabBlocks[ran]);

            // humanをどれだけ動かすと、Pivotの位置にCenterを持ってこられるか求める
            Vector3 centerDis = prefabBlocks[ran].position - BlockCenterPos;
            // ブロックのインスタンスを生成
            Transform pb = Instantiate(prefabBlocks[ran].gameObject.transform);

            // frameの座標と、humanのPivotとCenterの位置の差を足せば完了
            pb.position = Frame[i].position + centerDis;

            // 求めた座標をそれぞれ代入
            float x = prefabBlocks[ran].position.x;
            float y = 0.5f;
            float z = prefabBlocks[ran].position.z;

            pb.position += new Vector3(x, y, z);

            //b.transform.position += new Vector3(x, y, z); 

            //Instantiate(prefabBlocks[ran].gameObject, new Vector3(x, y, z), transform.rotation);
        }
    }

    /// <summary>
    /// ブロックの中心を求める
    /// </summary>  
    public Vector3 GetCenterPosition(Transform target)
    {
        //非アクティブも含めて、targetとtargetの子全てのレンダラーとコライダーを取得
        // ある方を使う。
        Collider[] cols = target.GetComponentsInChildren<Collider>(true);
        Renderer[] rens = target.GetComponentsInChildren<Renderer>(true);

        //コライダーとレンダラーが１つもなければ、target.positionがcenterになる　↓の計算式
        if (cols.Length == 0 && rens.Length == 0)
        {
            return target.position;
        }

        bool isInit = false;

        // min(始点) max(終点) 対角線
        Vector3 minPos = Vector3.zero;
        Vector3 maxPos = Vector3.zero;

        for (int i = 0; i < cols.Length; i++)
        {
            var bounds = cols[i].bounds;
            var center = bounds.center;
            var size = bounds.size / 2;

            //最初の１度だけ通って、minPosとmaxPosを初期化する
            if (!isInit)
            {
                minPos.x = center.x - size.x;
                minPos.y = center.y - size.y;
                minPos.z = center.z - size.z;
                maxPos.x = center.x + size.x;
                maxPos.y = center.y + size.y;
                maxPos.z = center.z + size.z;

                isInit = true;
                continue;
            }

            if (minPos.x > center.x - size.x) minPos.x = center.x - size.x;
            if (minPos.y > center.y - size.y) minPos.y = center.y - size.y;
            if (minPos.z > center.z - size.z) minPos.z = center.z - size.z;
            if (maxPos.x < center.x + size.x) maxPos.x = center.x + size.x;
            if (maxPos.y < center.y + size.y) maxPos.y = center.y + size.y;
            if (maxPos.z < center.z + size.z) maxPos.z = center.z + size.z;
        }

        for (int i = 0; i < rens.Length; i++)
        {
            var bounds = rens[i].bounds;
            var center = bounds.center;
            var size = bounds.size / 2;

            //コライダーが１つもなければ１度だけ通って、minPosとmaxPosを初期化する
            if (!isInit)
            {
                minPos.x = center.x - size.x;
                minPos.y = center.y - size.y;
                minPos.z = center.z - size.z;
                maxPos.x = center.x + size.x;
                maxPos.y = center.y + size.y;
                maxPos.z = center.z + size.z;

                isInit = true;
                continue;
            }

            if (minPos.x > center.x - size.x) minPos.x = center.x - size.x;
            if (minPos.y > center.y - size.y) minPos.y = center.y - size.y;
            if (minPos.z > center.z - size.z) minPos.z = center.z - size.z;
            if (maxPos.x < center.x + size.x) maxPos.x = center.x + size.x;
            if (maxPos.y < center.y + size.y) maxPos.y = center.y + size.y;
            if (maxPos.z < center.z + size.z) maxPos.z = center.z + size.z;
        }

        // 対角線の中心
        return (minPos + maxPos) / 2;
    }

    /// <summary>
    /// クリック（タップ）した位置にブロックがあるか
    /// </summary>
    private void check()
    {
        // メインカメラからクリックした位置に向かって光線を撃つ
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // 光線が当たったオブジェクトがブロックのとき
        // Rayの開始地点、Rayの方向、最大距離、レイヤーマスク
        if (Physics.Raycast(ray.origin, ray.direction, out hitBlock, 20.0f, LayerMask.GetMask("Block")))
        {
            hitRay = true;

            initialPosition = hitBlock.collider.gameObject.transform.position;

            // ブロックの拡大
            hitBlock.collider.gameObject.transform.localScale = new Vector3(1, 1, 1);
        }
        // 可視光線（始点：Main Camera、色：赤）
        Debug.DrawRay(ray.origin, ray.direction * 20, Color.red, 5);
    }

    /// <summary>
    /// ブロックを固定する
    /// </summary>
    private void fixation()
    {
        // 子オブジェクトのTransformを取得する。（親含む）
        childTransforms = hitBlock.collider.gameObject.GetComponentsInChildren<Transform>();
        // カウント：光線による盤Objectに衝突した子オブジェクト数
        int count = 0;

        foreach (Transform ct in childTransforms.Skip(1))
        {
            // 子オブジェクトから光線を撃つ。（子オブジェクトの少し手前から）
            ray = new Ray(ct.position + new Vector3(0, 1.1f, 0), ct.TransformDirection(new Vector3(0, -1, 0)));
            // 可視光線（始点：子ブロックの中心少し手前、色：青）
            Debug.DrawRay(ct.position + new Vector3(0, 1.1f, 0), ray.direction * 5, Color.blue, 5);

            // 衝突したObjectを全取得する
            hitObjects = Physics.RaycastAll(ray.origin, ray.direction, 5.0f);
            // Layer：10
            boardLayerIndex = LayerMask.NameToLayer("Board");



            foreach (RaycastHit hitObject in hitObjects)
            {

                // 盤上であるとき
                if (hitObject.collider.gameObject.layer == boardLayerIndex)
                {
                    count++;
                }
                // すでにブロックが置かれているとき
                else if (hitObject.collider.gameObject.tag == "Parts")
                {
                    count--;
                }
            }
        }

        // ブロックが盤上で空きがあるなら
        if (count == childTransforms.Length - 1)
        {

            //ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            ray = new Ray(childTransforms[0].gameObject.transform.position, childTransforms[0].TransformDirection(new Vector3(0, -1, 0)));
            // 可視光線（始点：Main Camera、色：黄）
            Debug.DrawRay(ray.origin, ray.direction * 5, Color.yellow, 5);

            if (Physics.Raycast(ray.origin, ray.direction, out hitBoard, 5.0f, LayerMask.GetMask("Board")))
            {
                // 盤ObjectのX,Z座標をブロックパーツに代入。Y座標を後付け。
                hitBlock.collider.gameObject.transform.position = hitBoard.collider.gameObject.transform.position + new Vector3(0, 1, 0);
                alreadyUsedCount++;
                emptyCheck = 1;
            }

            foreach (Transform ct in childTransforms)
            {
                // 親Objectから抜け出す
                ct.parent = null;
                // タグ（Parts）を付ける
                ct.tag = "Parts";
            }

            // 親Objectを削除する（Destroyは即時反映されない）
            DestroyImmediate(hitBlock.collider.gameObject);

            putCheck = 1;

        }
        else
        {
            // 縮小
            hitBlock.collider.gameObject.transform.localScale = new Vector3(0.5f, 1, 0.5f);
            // 初期位置に戻す
            hitBlock.collider.gameObject.transform.position = initialPosition;
        }
    }

    /// <summary>
    /// ブロックの削除する
    /// </summary>
    private void delete()
    {
        // ↑方向
        for (int x = 0; x < 8; x++)
        {
            ray = new Ray(new Vector3(0.5f + x, 0.5f, -1), new Vector3(0, 0, 1));
            // 可視光線（始点：盤の下列、色：緑）
            Debug.DrawRay(ray.origin, ray.direction * 10, Color.green, 5);

            hitObjectsZ = Physics.RaycastAll(ray.origin, ray.direction, 10.0f);
            //Debug.Log("↑" + x + ":" + hitObjectsZ.Length);
            // 1列揃っているなら
            if (hitObjectsZ.Length == 8)
            {
                foreach (RaycastHit hit in hitObjectsZ)
                {
                    Destroy(hit.collider.gameObject);
                }
            }
        }
        // →方向
        for (int z = 0; z < 8; z++)
        {
            ray = new Ray(new Vector3(-1, 0.5f, 0.5f + z), new Vector3(1, 0));
            // 可視光線（始点：盤の左列、色：緑）
            Debug.DrawRay(ray.origin, ray.direction * 10, Color.green, 5);

            hitObjectsX = Physics.RaycastAll(ray.origin, ray.direction, 10.0f);
            //Debug.Log("→" + z + ":" + hitObjectsX.Length);
            // 1列揃っているなら
            if (hitObjectsX.Length == 8)
            {
                foreach (RaycastHit hit in hitObjectsX)
                {
                    Destroy(hit.collider.gameObject);
                }
            }
        }

        putCheck = 0;
    }

    /// <summary>
    /// ブロックを移動する
    /// </summary>
    private void movement()
    {
        // 現在のマウスの座標を読み取る
        Vector3 mousePos = Input.mousePosition;
        // Z座標を付け足す。メインカメラから+10した位置
        mousePos.z = 14.5f;
        // マウスの座標をUnity上の座標に変換する
        Vector3 screenPos = Camera.main.ScreenToWorldPoint(mousePos);

        // 取得したオブジェクトの座標をscreenPos(Unity上)の座標に変更する。
        hitBlock.collider.gameObject.transform.position = screenPos;
    }

    /// <summary>
    /// 盤面の状況を取得する
    /// </summary>
    private void boardSituation()
    {
        int x = 0;
        int z = 0;
        ray = new Ray(new Vector3(0.5f, 1.1f, 0.5f), Vector3.forward);

        for (float f = 0.5f; f < 8; f++)
        {
            for (float g = 0.5f; g < 8; g++)
            {
                ray = new Ray(new Vector3(g, 1.1f, f), new Vector3(0, -1, 0));
                Physics.Raycast(ray.origin, ray.direction, out hitObject, 5.0f);
                Debug.DrawRay(ray.origin, ray.direction * 2, Color.gray, 5);

                if (hitObject.collider.gameObject.tag == "Parts")
                {
                    board[z][x] = true;
                    //Debug.Log(z + ":" + x + " " + board[z][x]);
                }
                else if (hitObject.collider.gameObject.layer == boardLayerIndex)
                {
                    board[z][x] = false;
                }

                x++;
            }
            x = 0;
            z++;

        }
    }

    /// <summary>
    /// 各ブロック情報 初期値
    /// </summary>
    private void blockInit()
    {
        Debug.Log("PrefabCount:" + prefabBlocks.Length);
        for (int i = 0; i < prefabBlocks.Length; i++)
        {
            childTransforms = prefabBlocks[i].gameObject.GetComponentsInChildren<Transform>();
            //Debug.Log(childTransforms[0].GetComponent<BoxCollider>().size.x + ":" + childTransforms[0].GetComponent<BoxCollider>().size.z);
            blocks.Add(new bool[(int)childTransforms[0].GetComponent<BoxCollider>().size.x, (int)childTransforms[0].GetComponent<BoxCollider>().size.z]);

            //Debug.Log("C.Length:" + (childTransforms.Length - 1));          
            
            for (int x = 0; x < blocks[i].GetLength(0); x++)
            {
                for (int z = 0; z < blocks[i].GetLength(1); z++)
                {
                    blocks[i][x, z] = false;
                }
            }


            // ブロックごとの部品数を保持する
            NumberOfPartsOfBlock.Add(childTransforms.Length - 1);
            Debug.Log(i +":"+ NumberOfPartsOfBlock[i]);
            // 親Objectを除いて子オブジェクト
            foreach (Transform ct in childTransforms.Skip(1))
            {
                int x = 0, z = 0;


                //Debug.Log(i + "-" + ct.position.x + ":" + ct.position.z);               

                x = (int)(ct.position.x * 2);
                z = (int)(ct.position.z * 2);


                //Debug.Log(i + "-" + x + ":" + z);
                // ブロックがある
                blocks[i][x, z] = true;
                //Debug.Log(i + "-" + x + ":" + z + ":" + blocks[i][x,z]);

            }

           

        }


        Debug.Log("--------------------");

        // 確認
        for (int i = 0; i < blocks.Count; i++)
        {
           //Debug.Log(i + ":" + blockShow(i));
        }

        Debug.Log("---------- FIN ----------");
    }

    /// <summary>
    /// 盤に現在のブロックを置ける空きがあるか
    /// </summary>
    private bool canPutBlock(int bNum)
    {


        int count = 0;

        // 盤のZ軸方向
        for (int i = 0; i < boardSize - blocks[bNum].GetLength(1) + 1; i++)
        {   // 盤のX軸方向
            for (int j = 0; j < boardSize - blocks[bNum].GetLength(0) + 1; j++)
            {   // ブロックのZ軸方向
                for (int z = 0; z < blocks[bNum].GetLength(1); z++)
                {   // ブロックのX軸方向
                    for (int x = 0; x < blocks[bNum].GetLength(0); x++)
                    {
                        // ブロックの x * z の範囲のうちブロックのない場所なら処理を飛ばす
                        if (blocks[bNum][x, z] == false)
                        {
                            //Debug.Log("飛ばす");
                            continue;
                        }  
                        // 盤の指定した１マスにブロックが存在しないなら
                        else if (board[i + z][j + x] == false)
                        {
                            count++;
                            //Debug.Log("count:" + count);
                        }
                        // 指定したブロックのパーツ数とカウントした数が一緒なら
                        if (count == NumberOfPartsOfBlock[bNum])
                        {
                            //Debug.Log("TRUE");
                            return true;
                        }
                    }
                    count = 0;
                    
                }
                
            }
           
        }
        //Debug.Log("FALSE");
        return false;

    }

    /// <summary>
    /// ブロックの使用状況を調べる
    /// </summary>
    private void GetUnusedBlocks()
    {
        int plus = 0;

        for (int i = 0; i < 3; i++)
        {
            // ３つの生成場所に光線を撃ち、未使用ならtrue 使用済みならfalse
            ray = new Ray(new Vector3(1 + plus, 2f, -3), new Vector3(0, -1, 0));
            Debug.DrawRay(ray.origin, ray.direction * 2, Color.white, 5);
            plus += 3;
            if (Physics.Raycast(ray.origin, ray.direction, out hitBlock, 5.0f, LayerMask.GetMask("Block")))
            {
                StatusOfGenerationPlace[i] = true;
            }
            else
            {
                StatusOfGenerationPlace[i] = false;
            }
            Debug.Log("Frame"  + i + ":" + StatusOfGenerationPlace[i]);
        }
        
    }

    private void boardShow()
    {
        String s = "";

        for (int z = boardSize -1; z >= 0; z--)
        {
            for (int x = 0; x < boardSize; x++)
            {
                s += board[z][x] + ",";
            }
            s += "\r\n";
        }
        Debug.Log(s);
    }

    private String blockShow(int i)
    {
        // 2019 06 24 ブロック初期化後　全情報を表示してみる

        String s = "";

        
        for (int x = 0; x < blocks[i].GetLength(0); x++)
        {
            for (int z = 0; z < blocks[i].GetLength(1); z++)
            {
                s += blocks[i][x,  z];
            }
            s += "\r\n";
        }


        return s;
    }

}
