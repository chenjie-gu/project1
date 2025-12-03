using UnityEngine;
using UnityEngine.SceneManagement; // 必须引用这个才能管理场景

public class GameManagement: MonoBehaviour
{
    void Update()
    {
        // 监听 R 键
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReloadScene();
        }
    }

    public void ReloadScene()
    {

        // 2. 获取当前场景的名字
        string currentSceneName = SceneManager.GetActiveScene().name;

        // 3. 重新加载当前场景
        SceneManager.LoadScene(currentSceneName);
    }
}