using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maze.Graphics.Shaders
{
    public class StandartShaderState : ShaderState
    {
        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["Standart"];

        public Matrix Matrix { get; set; }

        public Matrix Transform { get; set; }

        public Color Color { get; set; } = Color.White;

        public Texture2D Texture { get; set; }

        public Texture2D NormalTexture { get; set; }

        public bool OnlyColor { get; set; }

        public override void Apply(EffectParameterCollection parameters)
        {
            parameters["_matrix"].SetValue(Matrix);
            parameters["_transform"].SetValue(Transform);
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
