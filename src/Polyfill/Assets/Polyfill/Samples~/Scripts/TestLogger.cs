using UnityEngine;

// Unity does not find MonoBehaviour scripts with file-scoped namespace (namespace X;). Use block namespace.
namespace xpTURN.Polyfill.Samples
{
    public class TestLogger : MonoBehaviour
    {
        void FixedUpdate()
        {
            XLogger.Log($"Interpolated at time={Time.time:F2}, frame={Time.frameCount}");
        }
    }
}