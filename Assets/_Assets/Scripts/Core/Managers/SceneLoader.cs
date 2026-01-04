using UnityEngine;
using UnityEngine.SceneManagement;

namespace NatureVoice.Core
{
    /// <summary>
    /// Handles scene loading. Single Responsibility: only loads scenes.
    /// </summary>
    public class SceneLoader : MonoBehaviour, ISceneLoader
    {
        public static ISceneLoader Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);   // stays across scenes
        }

        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
