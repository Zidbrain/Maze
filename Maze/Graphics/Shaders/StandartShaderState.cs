using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maze.Graphics.Shaders
{
    public abstract class TransformShaderState : IShaderState
    {
        public Matrix WorldViewProjection { get; set; } = Matrix.Identity;

        public const int MaxBones = 50;

        public Matrix[] Bones { get; set; }

        public Matrix Transform { get; set; } = Matrix.Identity;

        public virtual void Apply(EffectParameterCollection parameters)
        {
            parameters["_matrix"].SetValue(WorldViewProjection);

            if (Bones != null)
                parameters["_bones"].SetValue(Bones);
            parameters["_transform"].SetValue(Transform);
        }

        public EffectTechnique GetTechnique(EffectTechniqueCollection techniques)
        {
            var technique = GetTechniqueForShadingModel(techniques);
            if (Bones != null)
                return techniques[$"Model{technique.Name}"];
            return technique;
        }

        protected abstract EffectTechnique GetTechniqueForShadingModel(EffectTechniqueCollection techniques);
    }

    public class StandartShaderState : TransformShaderState
    {
        protected override EffectTechnique GetTechniqueForShadingModel(EffectTechniqueCollection techniques) =>
            techniques["Standart"];

        public Color Color { get; set; } = Color.White;

        public Texture2D Texture { get; set; }

        public Texture2D NormalTexture { get; set; }

        public bool OnlyColor { get; set; }

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);
            parameters["_color"].SetValue(Color.ToVector4());
            parameters["_texture"].SetValue(Texture);
            parameters["_onlyColor"].SetValue(OnlyColor);

            if (NormalTexture is null)
                parameters["_normalEnabled"].SetValue(false);
            else
            {
                parameters["_normalEnabled"].SetValue(true);
                parameters["_normalTexture"].SetValue(NormalTexture);
            }
        }
    }
}
