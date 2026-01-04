using UnityEngine.SceneManagement;

namespace NatureVoice.Core
{
    public interface ISceneLoader
    {
        void LoadScene(string sceneName);
    }
}
