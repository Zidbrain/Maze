﻿using Microsoft.Xna.Framework.Graphics;

namespace Maze.Graphics.Shaders
{
    public interface IShaderState
    {
        EffectTechnique GetTechnique(EffectTechniqueCollection techniques);

        void Apply(EffectParameterCollection parameters);
    }

    public class Shader
    {
        private readonly Effect _effect;

        public Shader()
        {
            _effect = Maze.Instance.Content.Load<Effect>("Shaders/Shader");

            StandartState = new StandartShaderState();
        }

        public IShaderState State { get; set; }
        
        public StandartShaderState StandartState { get; }

        public void Apply()
        {
            _effect.CurrentTechnique = State.GetTechnique(_effect.Techniques);
            State.Apply(_effect.Parameters);
            _effect.CurrentTechnique.Passes[0].Apply();
        }
    }
}
