using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maze
{
    public class Shader
    {
        private readonly Effect _effect;

        public bool OnlyColor { get; set; }

        public Shader()
        {
            _effect = Maze.Game.Content.Load<Effect>("Shader");

            Color = Color.White;
        }

        public Matrix Matrix
        {
            get => _effect.Parameters["_matrix"].GetValueMatrix();
            set => _effect.Parameters["_matrix"].SetValue(value);
        }

        public Matrix Transform
        {
            get => _effect.Parameters["_transform"].GetValueMatrix();
            set => _effect.Parameters["_transform"].SetValue(value);
        }

        public Color Color
        {
            get => new Color(_effect.Parameters["_color"].GetValueVector4());
            set => _effect.Parameters["_color"].SetValue(value.ToVector4());
        }

        public Texture2D Texture
        {
            get => _effect.Parameters["_texture"].GetValueTexture2D();
            set => _effect.Parameters["_texture"].SetValue(value);
        }

        public void Apply()
        {
            _effect.CurrentTechnique = _effect.Techniques[OnlyColor ? "Color" : "Standart"];
            _effect.CurrentTechnique.Passes[0].Apply();
        }
    }
}
