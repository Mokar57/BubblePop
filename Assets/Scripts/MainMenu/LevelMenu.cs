using UnityEngine;
using UnityEngine.SceneManagement;
public class LevelMenu : MonoBehaviour
{
    public void Openlevel(int levelId)
    {
      string levelName = "Ep" + levelId;
        SceneManager.LoadScene(levelName);  
    }
}