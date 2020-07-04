using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maze
{
    public abstract class ShaderState
    {
        public abstract EffectTechnique GetTechnique(EffectTechniqueCollection techniques);

        public abstract void Apply(EffectParameterCollection parameters);
    }

    public class StandartShaderState : ShaderState
    {
        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["Standart"];

        public Matrix Matrix { get; set; }

        public Matrix Transform { get; set; }

        public Color Color { get; set; } = Color.White;

        public Texture2D Texture { get; set; }

        public bool OnlyColor { get; set; }

        public override void Apply(EffectParameterCollection parameters)
        {
            parameters["_matrix"].SetValue(Matrix);
            parameters["_transform"].SetValue(Transform);
            parameters["_color"].SetValue(Color.ToVector4());
            parameters["_texture"].SetValue(Texture);
            parameters["_onlyColor"].SetValue(OnlyColor);
        }
    }

    public class FogShaderState : StandartShaderState
    {
        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["Fog"];

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);

            parameters["_cameraPlane"].SetValue(CameraPlane.ToVector4());
            parameters["_fogStart"].SetValue(FogStart);
            parameters["_fogEnd"].SetValue(FogEnd);
            parameters["_fogColor"].SetValue(FogColor.ToVector4());
        }

        public Plane CameraPlane { get; set; }

        public float FogStart { get; set; }

        public float FogEnd { get; set; }

        public Color FogColor { get; set; }
    }

    public class RasterizeShaderState : ShaderState
    {
        public override void Apply(EffectParameterCollection parameters)
        {
            parameters["_texture"].SetValue(Color);
            //parameters["_depthTexture"].SetValue(Depth);
            //parameters["_normal"].SetValue(Normal);
            //parameters["_position"].SetValue(Position);
        }

        public Texture2D Color { get; set; }
        public Texture2D Depth { get; set; }
        public Texture2D Normal { get; set; }
        public Texture2D Position { get; set; }

        public RasterizeShaderState(RenderTargets renderTargets)
        {
            Color = renderTargets.Color;
            Depth = renderTargets.Depth;
            Normal = renderTargets.Normal;
            Position = renderTargets.Position;
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["Rasterize"];
    }

    public class Shader
    {
        private readonly Effect _effect;

        public Shader()
        {
            _effect = Maze.Game.Content.Load<Effect>("Shaders/Shader");

            StandartState = new StandartShaderState();
        }

        public ShaderState State { get; set; }

        public StandartShaderState StandartState { get; }

        public void Apply()
        {
            _effect.CurrentTechnique = State.GetTechnique(_effect.Techniques);
            State.Apply(_effect.Parameters);
            _effect.CurrentTechnique.Passes[0].Apply();
        }
    }
}
