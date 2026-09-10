using UnityEngine;

namespace ScalePunch.Core
{
    /// <summary>
    /// Publishes the prototype config before anything reads it.
    ///
    /// Execution order is explicit and very negative because half the systems in
    /// the scene check a toggle in their own Awake, and Unity does not define
    /// the order Awake runs in between GameObjects.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] PrototypeConfig config;

        void Awake() => PrototypeConfig.SetActive(config);
    }
}
