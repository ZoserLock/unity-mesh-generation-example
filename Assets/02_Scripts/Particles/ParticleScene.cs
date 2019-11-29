using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleScene : MonoBehaviour
{
    float _particleTime    = 0;

    [SerializeField]
    float _particleTimeMax = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        _particleTime += Time.deltaTime;

        if(_particleTime>_particleTimeMax)
        {
            _particleTime -= _particleTimeMax;

            SparkController.Instance.SpawnSpark(new Vector3(0, 0, 0), Color.white, 0);
            SparkController.Instance.SpawnSpark(new Vector3(0, 0, 0), Color.white, 1);
            SparkController.Instance.SpawnSpark(new Vector3(0, 0, 0), Color.white, 2);
            SparkController.Instance.SpawnSpark(new Vector3(0, 0, 0), Color.white, 0);
            SparkController.Instance.SpawnSpark(new Vector3(0, 0, 0), Color.white, 1);
            SparkController.Instance.SpawnSpark(new Vector3(0, 0, 0), Color.white, 2);
        }
    }
}
