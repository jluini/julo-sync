/*using System.Collections;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Julo.Util
{
    public delegate void OnFinishLoadingScene();

    public class Scene
    {
        // Scene loading


        public static void LoadSceneAsync(string sceneName, OnFinishLoadingScene onFinishDelegate)
        {

            StartCoroutine(LoadSceneCoroutine(sceneName, onFinishDelegate));


        }

        public static IEnumerator LoadSceneCoroutine(string sceneName, OnFinishLoadingScene onFinishDelegate)
        {

            var operation = SceneManager.LoadSceneAsync(sceneName);

            while(!operation.isDone)
            {
                yield return null; // wait for next frame
            }

            onFinishDelegate();
        }


    } // class Scene
} // namespace Julo.Util
*/