using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maze.Graphics.Shaders
{
    public class InstancedShaderState : StandartShaderState
    {
        public Matrix[] Matrices { get; set; }

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);
            parameters["_matrices"].SetValue(Matrices);
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["Instanced"];
    }
}
