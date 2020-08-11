using Microsoft.Xna.Framework.Graphics;

namespace Maze.Graphics.Shaders
{
    public abstract class ShaderState
    {
        public abstract EffectTechnique GetTechnique(EffectTechniqueCollection techniques);

        public abstract void Apply(EffectParameterCollection parameters);
    }

    public class Shader
    {
        private readonly Effect _effect;

        public Shader()
        {
            _effect = Maze.Instance.Content.Load<Effect>("Shaders/Shader");

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
