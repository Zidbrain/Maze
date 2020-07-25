using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maze.Graphics.Shaders
{
    public class FogShaderState : DefferedShaderState
    {
        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["Fog"];

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);

            parameters["_cameraPosition"].SetValue(CameraPosition);
            parameters["_fogStart"].SetValue(FogStart);
            parameters["_fogEnd"].SetValue(FogEnd);
            parameters["_fogColor"].SetValue(FogColor.ToVector4());
        }

        public FogShaderState(Texture2D colors, Texture2D positions) : base(colors) =>
            Position = positions;

        public Vector3 CameraPosition { get; set; }

        public float FogStart { get; set; }

        public float FogEnd { get; set; }

        public Color FogColor { get; set; }
    }
}
