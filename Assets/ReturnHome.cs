using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReturnHome : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnBtnReturn);
    }

    void OnBtnReturn()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("main");
    }
}
