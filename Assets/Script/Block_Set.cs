using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block_Set : MonoBehaviour {
    public GameObject[] Blocies;

    void Start (){
        int cnt = 0;
        while(cnt < 3){
            int number = Random.Range (0, Blocies.Length);
            Instantiate(Blocies[number],new Vector3(-2f + 3f*cnt, 0f, 0f),transform.rotation);

            cnt++;
        }

    }

}
