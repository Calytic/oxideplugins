using Oxide.Core.Libraries;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Draw", "playrust.io / dcode", "1.0.1", ResourceId = 968)]
    public class Draw : RustPlugin
    {
        [LibraryFunction("Line")]
        public void Line(BasePlayer player, Vector3 from, Vector3 to, Color color, float duration) {
            player.SendConsoleCommand("ddraw.line", duration, color, from, to);
        }

        [LibraryFunction("Arrow")]
        public void Arrow(BasePlayer player, Vector3 from, Vector3 to, float headSize, Color color, float duration) {
            player.SendConsoleCommand("ddraw.arrow", duration, color, from, to, headSize);
        }

        [LibraryFunction("Sphere")]
        public void Sphere(BasePlayer player, Vector3 pos, float radius, Color color, float duration) {
            player.SendConsoleCommand("ddraw.sphere", duration, color, pos, radius);
        }

        [LibraryFunction("Text")]
        public void Text(BasePlayer player, Vector3 pos, string text, Color color, float duration) {
            player.SendConsoleCommand("ddraw.text", duration, color, pos, text);
        }

        [LibraryFunction("Box")]
        public void Box(BasePlayer player, Vector3 pos, float size, Color color, float duration) {
            player.SendConsoleCommand("ddraw.box", duration, color, pos, size);
        }
    }
}
