using System;
using Microsoft.Xna.Framework.Graphics;

namespace Maze
{
    public class RenderTargets
    {
        public RenderTarget2D Color { get; }
        public RenderTarget2D Depth { get; }
        public RenderTarget2D Normal { get; }
        public RenderTarget2D Position { get; }

        public RenderTarget2D United { get; }

        public RenderTargetBinding[] Bindings { get; }

        public RenderTargets()
        {
            Color = new RenderTarget2D(Maze.Game.GraphicsDevice, 1920, 1080, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
            Depth = new RenderTarget2D(Maze.Game.GraphicsDevice, 1920, 1080, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
            Normal = new RenderTarget2D(Maze.Game.GraphicsDevice, 1920, 1080, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
            Position = new RenderTarget2D(Maze.Game.GraphicsDevice, 1920, 1080, false, SurfaceFormat.Vector4, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);

            United = new RenderTarget2D(Maze.Game.GraphicsDevice, 1920, 1080, false, SurfaceFormat.Color, DepthFormat.None, 2, RenderTargetUsage.PreserveContents);

            Bindings = new RenderTargetBinding[]
            {
                new RenderTargetBinding(Color),
                new RenderTargetBinding(Depth),
                new RenderTargetBinding(Normal),
                new RenderTargetBinding(Position)
            };
        }
    }
}
