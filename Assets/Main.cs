using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    public Transform listContent;
    public GameObject itemPrefab;

    string[] simples = new string[]
    {
        "2.分离轴碰撞检测", "02-sat",
        "3.GJK碰撞检测基础", "03-gjk",
        "4.GJK计算多边形最近距离", "04-gjk-closest-point",
        "5.GJK&EPA计算穿透向量", "05-gjk-epa",
        "6.碰撞分离", "06-seperation",
        "7.2D小游戏", "07-2d-demo",
        "8.AABB树", "08-aabb-tree",
    };

    private void Start()
    {
        itemPrefab.SetActive(true);
        for(int i = 0; i < simples.Length; i += 2)
        {
            CreateListItem(simples[i], simples[i + 1]);
        }
        itemPrefab.SetActive(false);
    }

    GameObject CreateListItem(string title, string sceneName)
    {
        GameObject item = Instantiate(itemPrefab, listContent, false);

        Button button = item.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            OpenScene(sceneName);
        });

        Text text = item.transform.Find("Text").GetComponent<Text>();
        text.text = title;
        return item;
    }

    public void OpenScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    public void OnBtnCSDN()
    {
        Application.OpenURL("https://blog.csdn.net/you_lan_hai");
    }

    public void OnBtnGithub()
    {
        Application.OpenURL("https://github.com/youlanhai");
    }
}
