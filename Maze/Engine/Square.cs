﻿using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using static System.Math;
using System;

namespace Maze.Engine
{
    public class Square : IDrawable, ICollidable, IDisposable
    {
        private static readonly Vector3[] s_pos =
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, 0.5f),
            new Vector3(0.5f, 0f, 0.5f),
            new Vector3(0.5f, 0f, 0.5f),
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, -0.5f)
        };

        private readonly VertexPositionNormalTexture[] _vertexes;
        private readonly VertexBuffer _buffer;
        private readonly Matrix _transform;

        public Vector3 Position { get; set; }
        public Vector2 Size { get; set; } = Vector2.One;
        public Color Color { get; set; } = Color.White;

        public ShaderState ShaderState { get; set; }

        public Texture2D Texture { get; set; }
        public IEnumerable<Polygon> Polygones
        {
            get
            {
                var ret = new Polygon[2];
                for (int i = 0; i < 2; i++)
                {
                    var buf = new Vector3[3];
                    for (int j = 0; j < 3; j++)
                        buf[j] = Vector3.Transform(_vertexes[i * 3 + j].Position, Matrix.CreateScale(Size.X, 1f, Size.Y) * _transform * Matrix.CreateTranslation(Position));

                    ret[i] = new Polygon(buf);
                }

                return ret;
            }
        }

        public Square(Matrix basis, Texture2D texture)
        {
            _vertexes = new VertexPositionNormalTexture[6];
            for (var i = 0; i < _vertexes.Length; i++)
                _vertexes[i] = new VertexPositionNormalTexture(s_pos[i], basis.Up, new Vector2(s_pos[i].X + 0.5f, s_pos[i].Z + 0.5f));

            _buffer = new DynamicVertexBuffer(Maze.Game.GraphicsDevice, typeof(VertexPositionNormalTexture), 6, BufferUsage.WriteOnly);
            _buffer.SetData(_vertexes);

            _transform = basis;
            Texture = texture;
        }

        public void Draw()
        {
            if (ShaderState is null)
                ShaderState = Maze.Game.Shader.StandartState;
            
            if (ShaderState is StandartShaderState state)
            {
                state.Texture = Texture;
                state.Color = Color;
            }

            Maze.Game.DrawVertexes(_buffer, Matrix.CreateScale(Size.X, 1f, Size.Y) * _transform * Matrix.CreateTranslation(Position), shaderState: ShaderState);
        }

        public void Dispose() =>
            _buffer.Dispose();
    }
}
