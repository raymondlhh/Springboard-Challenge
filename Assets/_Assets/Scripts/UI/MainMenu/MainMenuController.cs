using UnityEngine;
using NatureVoice.Core;

public class MainMenuController : MonoBehaviour
{
    // Called by the "Play with AR" button
    public void OnPlayWithAR()
    {
        SceneLoader.Instance.LoadScene(SceneNames.ARForestScene);
    }

    // Called by the "Play with Mobile" button
    public void OnPlayWithMobile()
    {
        SceneLoader.Instance.LoadScene(SceneNames.MobileForestScene);
    }
}

