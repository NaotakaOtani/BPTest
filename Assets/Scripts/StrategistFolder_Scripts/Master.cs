using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Master : MonoBehaviour
{
    // 盤
    List<List<bool>> board = new List<List<bool>>();
    // 盤Size（8x8）
    const int boardSize = 8;

    // Ray
    Ray ray;
    // Rayのアタリ判定
    private bool hitRay = false;
    // Rayによって取得した情報を保存する構造体
    private RaycastHit hitBlock, hitBoard;
    private RaycastHit[] hitObjects;
    // ブロックの初期位置の保存用
    private Vector3 initialPosition;

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
    }

    // Update is called once per frame
    void Update()
    {
        
        if (Input.GetMouseButtonDown(0))
        {
            check();
        }
        if (Input.GetMouseButtonUp(0) && hitRay)
        {
            fixation();

            hitRay = false;

            delete();
        }
        if (hitRay)
        {
            movement();
        }
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
            hitBlock.collider.gameObject.transform.localScale = new Vector3(2, 1, 2);
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
        Transform[] childTransforms = hitBlock.collider.gameObject.GetComponentsInChildren<Transform>();
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
            int boardLayerIndex = LayerMask.NameToLayer("Board");

            

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
            }

            foreach (Transform ct in childTransforms)
            {
                // 親Objectから抜け出す
                ct.parent = null;
                // タグ（Parts）を付ける
                ct.tag = "Parts";
            }

            // 親Objectを削除する（Destroyは即時反映されない）
            DestroyImmediate(this.gameObject);
        }
        else
        {
            // 縮小
            hitBlock.collider.gameObject.transform.localScale = new Vector3(1, 1, 1);
            // 初期位置に戻す
            hitBlock.collider.gameObject.transform.position = initialPosition;
        }
    }

    /// <summary>
    /// ブロックの削除する
    /// </summary>
    private void delete()
    {
        // Z軸方向
        for (int x = 0; x < 8; x++)
        {
            ray = new Ray(new Vector3(0.5f + x, 0.5f, -1), new Vector3(0, 0, 1));
            // 可視光線（始点：盤の下列、色：緑）
            Debug.DrawRay(ray.origin, ray.direction * 10, Color.green, 5);
            
            hitObjects = Physics.RaycastAll(ray.origin, ray.direction, 10.0f);
            // 1列揃っているなら
            if (hitObjects.Length == 8)
            {
                foreach (RaycastHit hit in hitObjects)
                {
                    Destroy(hit.collider.gameObject);
                }
            }
        }
        // X軸方向
        for (int z = 0; z < 8; z++)
        {
            ray = new Ray(new Vector3(-1, 0.5f, 0.5f + z), new Vector3(1, 0));
            // 可視光線（始点：盤の左列、色：緑）
            Debug.DrawRay(ray.origin, ray.direction * 10, Color.green, 5);
            
            hitObjects = Physics.RaycastAll(ray.origin, ray.direction, 10.0f);
            // 1列揃っているなら
            if (hitObjects.Length == 8)
            {
                foreach (RaycastHit hit in hitObjects)
                {
                    Destroy(hit.collider.gameObject);
                }
            }
        }
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


}
